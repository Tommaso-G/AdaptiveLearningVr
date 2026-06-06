using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using VRBuilder.Core;

[CreateAssetMenu(menuName = "Learning Styles/Riflessivo Behaviour")]
public class RiflessivoFeatures : LearningStyleFeatures
{
    [Header("Settings")]
    public float audioFadeDuration = 1.5f;

    [Header("State")]
    [SerializeField] private bool isTimeStopFeatureEnabled = true;

    private static int? _uiLayerMask;

    private int _activeFeedbackCount = 0;
    private bool _firstFeedbackEverOpened = false;

    private bool EffectsActive => _activeFeedbackCount > 0;
    private static int UILayerMask
    {
        get
        {
            if (!_uiLayerMask.HasValue)
                _uiLayerMask = 1 << LayerMask.NameToLayer("UI");
            return _uiLayerMask.Value;
        }
    }

    private readonly List<(SphereInteractionCaster caster, LayerMask original)> _sphereOriginalMasks = new();
    private readonly List<(CurveInteractionCaster caster, LayerMask original)> _curveOriginalMasks = new();

    private Volume globalVolume;
    private readonly List<Animator> pausedAnimators = new();
    private class ParticleSnapshot
    {
        public ParticleSystem ps;
        public bool wasPlaying;
    }

    private readonly Dictionary<ParticleSystem, ParticleSnapshot> particleStates = new();
    private readonly List<AgentState> pausedNavMeshAgents = new();
    private ChapterTimer pausedChapterTimer;
    private ChapterTracker pausedChapterTracker;
    public static bool IsPaused { get; private set; } = false;

    // --- Cache ---
    private Animator[] _cachedAnimators;
    private ParticleSystem[] _cachedParticles;
    private NavMeshAgent[] _cachedNavMeshAgents;
    private bool _cacheInitialized = false;

    // --- Coroutine handles per cancellazione ---
    private Coroutine _audioFadeCoroutine;
    private Coroutine _volumeFadeCoroutine;
    private Coroutine _resetCoroutine;

    private FeedbackAutoManager _safeRunner;
    private bool _effectsCurrentlyApplied = false;

    private bool _cacheInitializedS
{
    get => _cacheInitializedBacking;
    set
    {
        Debug.Log($"[RiflessivoFeatures] _cacheInitialized impostato a {value}\n{new System.Diagnostics.StackTrace()}");
        _cacheInitializedBacking = value;
    }
}
private bool _cacheInitializedBacking = false;

    private FeedbackAutoManager SafeRunner
    {
        get
        {
            if (_safeRunner == null)
                _safeRunner = Object.FindFirstObjectByType<FeedbackAutoManager>();
            return _safeRunner;
        }
    }

    private void OnEnable()
    {
        _firstFeedbackEverOpened = false;
        _cacheInitialized = false;
        _activeFeedbackCount = 0;
        Debug.Log("[RiflessivoFeatures] OnEnable — variabili resettate.");
    }


    // --- CACHE ---

    private void EnsureCache()
    {
        if (_cacheInitialized) return;

        _cachedAnimators     = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _cachedParticles     = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _cachedNavMeshAgents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        _cacheInitialized = true;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"[RiflessivoFeatures] Cache inizializzata — {_cachedAnimators.Length} animators, {_cachedParticles.Length} particles, {_cachedNavMeshAgents.Length} navmesh agents.");

        sb.AppendLine("── ANIMATORS ──");
        foreach (var a in _cachedAnimators)
            sb.AppendLine($"  [{(a.gameObject.activeInHierarchy ? "ON " : "off")}] {GetFullPath(a.transform)} (layer: {LayerMask.LayerToName(a.gameObject.layer)})");

        sb.AppendLine("── PARTICLES ──");
        foreach (var p in _cachedParticles)
            sb.AppendLine($"  [{(p.gameObject.activeInHierarchy ? "ON " : "off")}] {GetFullPath(p.transform)}");

        sb.AppendLine("── NAVMESH AGENTS ──");
        foreach (var n in _cachedNavMeshAgents)
            sb.AppendLine($"  [{(n.gameObject.activeInHierarchy ? "ON " : "off")}] {GetFullPath(n.transform)}");

        Debug.Log(sb.ToString());
    }

    private string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }


    // --- LOGICA DI CONTROLLO STATO ---

    public override void EnableFeature()
    {
        isTimeStopFeatureEnabled = true;
        Debug.Log("[RiflessivoFeatures] Feature abilitata manualmente.");
        ApplyReflectiveEffects(Object.FindFirstObjectByType<FeedbackPrefabController>());
    }

    public override void DisableFeature()
    {
        isTimeStopFeatureEnabled = false;
        ResetReflectiveEffects(Object.FindFirstObjectByType<FeedbackPrefabController>());
    }


    // --- CICLO DI VITA FEEDBACK ---

    public override void OnFeedbackOpened(FeedbackPrefabController feedback)
    {
        if (feedback == null || !isTimeStopFeatureEnabled)
            return;

        if (!_firstFeedbackEverOpened)
        {
            _firstFeedbackEverOpened = true;
            EnsureCache();
        }

        _activeFeedbackCount++;

        Debug.Log($"[RiflessivoFeatures] Feedback aperto. Count = {_activeFeedbackCount}");

        if (_activeFeedbackCount == 1)
        {
            Debug.Log("[RiflessivoFeatures] Primo feedback aperto -> applicazione effetti.");
            ApplyReflectiveEffects(feedback);
        }
    }

    public override void OnFeedbackClosed(FeedbackPrefabController feedback)
    {
        if (feedback == null)
            return;

        _activeFeedbackCount--;

        if (_activeFeedbackCount < 0)
            _activeFeedbackCount = 0;

        Debug.Log($"[RiflessivoFeatures] Feedback chiuso. Count = {_activeFeedbackCount}");

        if (_activeFeedbackCount == 0)
        {
            Debug.Log("[RiflessivoFeatures] Ultimo feedback chiuso -> reset effetti.");

            if (_resetCoroutine != null)
                SafeRunner?.StopCoroutineSafe(_resetCoroutine);

            _resetCoroutine = SafeRunner?.RunCoroutineSafe(WaitForVolumeAndReset(feedback));
        }
    }

    public override void resetVariables()
    {
        isTimeStopFeatureEnabled = true;
        _cacheInitialized = false;
        _firstFeedbackEverOpened = false;
        _activeFeedbackCount = 0;

        Debug.Log("[RiflessivoFeatures] Variabili resettate.");
    }


    // --- LOGICA ESECUZIONE EFFETTI ---

    private void ApplyReflectiveEffects(FeedbackPrefabController feedback)
    {
        if (EffectsActive == false && _activeFeedbackCount != 1)
            return;

        EnsureVolumeReference();

        if (_resetCoroutine != null)
            SafeRunner?.StopCoroutineSafe(_resetCoroutine);
        if (_audioFadeCoroutine != null)
            SafeRunner?.StopCoroutineSafe(_audioFadeCoroutine);
        if (_volumeFadeCoroutine != null)
            SafeRunner?.StopCoroutineSafe(_volumeFadeCoroutine);

        if (globalVolume != null)
            _volumeFadeCoroutine = SafeRunner?.RunCoroutineSafe(FadeVolumeWeight(globalVolume, 1f, 0.5f));

        _audioFadeCoroutine = SafeRunner?.RunCoroutineSafe(FadeAudioVolume(AudioListener.volume, 0f, audioFadeDuration));

        RestrictCastersToUILayer();
        MuteIgnoredAudioSources(true);

        PauseChapterTimers();
        PauseChapterTracker();
        PauseAnimators();
        PauseParticles();
        PauseNavMeshAgent();

        SetPaused(true);
    }

    private void ResetReflectiveEffects(FeedbackPrefabController feedback)
    {
        if (_audioFadeCoroutine != null)
            SafeRunner?.StopCoroutineSafe(_audioFadeCoroutine);

        _audioFadeCoroutine = SafeRunner?.RunCoroutineSafe(FadeAudioVolume(AudioListener.volume, 1f, audioFadeDuration));

        MuteIgnoredAudioSources(false);

        ResumeChpaterTimers();
        ResumeChpaterTracker();
        RestoreCasterLayers();
        ResumeAnimators();
        ResumeParticles();
        ResumeNavMeshAgents();

        SetPaused(false);
    }

    private IEnumerator WaitForVolumeAndReset(FeedbackPrefabController feedback)
    {
        if (_audioFadeCoroutine != null)
            SafeRunner?.StopCoroutineSafe(_audioFadeCoroutine);
        _audioFadeCoroutine = null;

        EnsureVolumeReference();

        if (globalVolume != null)
        {
            float elapsed = 0f;
            float startWeight = globalVolume.weight;
            while (elapsed < audioFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                globalVolume.weight = Mathf.Lerp(startWeight, 0f, elapsed / audioFadeDuration);
                yield return null;
            }
            globalVolume.weight = 0f;
        }

        ResetReflectiveEffects(feedback);
        _resetCoroutine = null;
    }

    private void EnsureVolumeReference()
    {
        if (globalVolume == null)
            globalVolume = Object.FindFirstObjectByType<Volume>();
    }

    private void MuteIgnoredAudioSources(bool mute)
    {
        foreach (var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            if (source != null && source.ignoreListenerVolume)
            {
                source.mute = mute;
                Debug.Log($"[RiflessivoFeatures] AudioSource '{source.name}' ignoreListenerVolume → mute={mute}");
            }
        }
    }


    // --- GESTIONE LAYER MASK DEI CASTER ---

    private void RestrictCastersToUILayer()
    {
        if (_sphereOriginalMasks.Count > 0 || _curveOriginalMasks.Count > 0)
            return;

        foreach (var caster in Object.FindObjectsByType<SphereInteractionCaster>(FindObjectsSortMode.None))
        {
            if (caster == null) continue;
            _sphereOriginalMasks.Add((caster, caster.physicsLayerMask));
            caster.physicsLayerMask = UILayerMask;
            Debug.Log($"[RiflessivoFeatures] SphereInteractionCaster '{caster.name}' layer mask → solo UI.");
        }

        foreach (var caster in Object.FindObjectsByType<CurveInteractionCaster>(FindObjectsSortMode.None))
        {
            if (caster == null) continue;
            _curveOriginalMasks.Add((caster, caster.raycastMask));
            caster.raycastMask = UILayerMask;
            Debug.Log($"[RiflessivoFeatures] CurveInteractionCaster '{caster.name}' layer mask → solo UI.");
        }
    }

    private void RestoreCasterLayers()
    {
        foreach (var (caster, original) in _sphereOriginalMasks)
        {
            if (caster != null)
            {
                caster.physicsLayerMask = original;
                Debug.Log($"[RiflessivoFeatures] SphereInteractionCaster '{caster.name}' layer mask ripristinato.");
            }
        }
        _sphereOriginalMasks.Clear();

        foreach (var (caster, original) in _curveOriginalMasks)
        {
            if (caster != null)
            {
                caster.raycastMask = original;
                Debug.Log($"[RiflessivoFeatures] CurveInteractionCaster '{caster.name}' layer mask ripristinato.");
            }
        }
        _curveOriginalMasks.Clear();
    }


    private IEnumerator FadeVolumeWeight(Volume volume, float targetWeight, float duration)
    {
        float startWeight = volume.weight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            volume.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
            yield return null;
        }
        volume.weight = targetWeight;
    }

    private IEnumerator FadeAudioVolume(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            AudioListener.volume = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        AudioListener.volume = end;
    }

    public void PauseAnimators()
    {
        pausedAnimators.Clear();

        foreach (Animator animator in _cachedAnimators)
        {
            if (animator != null && animator.enabled &&
                animator.gameObject.layer != LayerMask.NameToLayer("UI"))
            {
                animator.enabled = false;
                pausedAnimators.Add(animator);
            }
        }
    }

    private void ResumeAnimators()
    {
        foreach (Animator animator in pausedAnimators)
        {
            if (animator != null)
                animator.enabled = true;
        }
        pausedAnimators.Clear();
    }

    private void PauseParticles()
    {
        foreach (var ps in _cachedParticles)
        {
            if (ps == null || !ps.gameObject.activeInHierarchy)
                continue;

            if (!particleStates.ContainsKey(ps))
            {
                particleStates[ps] = new ParticleSnapshot
                {
                    ps = ps,
                    wasPlaying = ps.isPlaying
                };
            }

            ps.Pause(true);
        }
    }

    private void ResumeParticles()
    {
        foreach (var kv in particleStates)
        {
            var ps = kv.Key;
            var state = kv.Value;

            if (ps == null) continue;

            if (state.wasPlaying)
                ps.Play(true);
        }

        particleStates.Clear();
    }

    private class AgentState
    {
        public NavMeshAgent agent;
        public Vector3 destination;
    }

    private void PauseNavMeshAgent()
    {
        pausedNavMeshAgents.Clear();

        foreach (var nma in _cachedNavMeshAgents)
        {
            if (nma != null && nma.gameObject.activeInHierarchy && !nma.isStopped)
            {
                pausedNavMeshAgents.Add(new AgentState
                {
                    agent       = nma,
                    destination = nma.hasPath ? nma.destination : nma.transform.position
                });
                nma.isStopped = true;
            }
        }
    }

    private void ResumeNavMeshAgents()
    {
        foreach (var state in pausedNavMeshAgents)
        {
            if (state.agent != null)
            {
                state.agent.isStopped = false;
                state.agent.SetDestination(state.destination);
            }
        }
        pausedNavMeshAgents.Clear();
    }

    private void PauseChapterTimers()
    {
        if (pausedChapterTimer == null)
            pausedChapterTimer = FindAnyObjectByType<ChapterTimer>();

        pausedChapterTimer.PauseTimer(all: true);
        Debug.Log($"[RiflessivoFeatures] Chapter timer in pausa per tutti i capitoli.");
    }

    private void ResumeChpaterTimers()
    {
        if (pausedChapterTimer == null)
            pausedChapterTimer = FindAnyObjectByType<ChapterTimer>();

        pausedChapterTimer.ResumeTimer(all: true);
        Debug.Log($"[RiflessivoFeatures] Chapter timer ripreso per tutti i capitoli.");
    }

    private void PauseChapterTracker()
    {
        if (pausedChapterTracker == null)
            pausedChapterTracker = FindAnyObjectByType<ChapterTracker>();

        pausedChapterTracker.PauseTracker();
        Debug.Log($"[RiflessivoFeatures] Chapter tracker in pausa.");
    }

    private void ResumeChpaterTracker()
    {
        if (pausedChapterTracker == null)
            pausedChapterTracker = FindAnyObjectByType<ChapterTracker>();

        pausedChapterTracker.ResumeTracker();
        Debug.Log($"[RiflessivoFeatures] Chapter tracker ripreso.");
    }

    public static void SetPaused(bool value)
    {
        IsPaused = value;
    }

    public override void OnStepActivated(IStep step)
    {
        _activeFeedbackCount = 0;
        _cacheInitialized = false;
        isTimeStopFeatureEnabled = true;
    }

    public override void OnStepCompleted(IStep step) { }
}