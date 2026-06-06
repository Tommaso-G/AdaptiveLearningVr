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

    // Layer mask contenente solo il layer "UI", calcolato a runtime per evitare
    // chiamate a NameToLayer durante la serializzazione (UnityException)
    private static int? _uiLayerMask;

    private int _activeFeedbackCount = 0;

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

    // Backup dei layer mask originali dei caster
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

    private FeedbackAutoManager SafeRunner
    {
        get
        {
            if (_safeRunner == null)
                _safeRunner = Object.FindFirstObjectByType<FeedbackAutoManager>();
            return _safeRunner;
        }
    }


    // --- CACHE ---

    private void EnsureCache()
    {
        if (_cacheInitialized) return;

        _cachedAnimators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _cachedParticles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _cachedNavMeshAgents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        _cacheInitialized = true;
        Debug.Log("[RiflessivoFeatures] Cache animators/particles/agents inizializzata.");
    }

    public void InitializeCache() => EnsureCache();


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

        _activeFeedbackCount++;

        Debug.Log(
            $"[RiflessivoFeatures] Feedback aperto. Count = {_activeFeedbackCount}"
        );

        if (_activeFeedbackCount == 1)
        {
            Debug.Log(
                "[RiflessivoFeatures] Primo feedback aperto -> applicazione effetti."
            );

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

        Debug.Log(
            $"[RiflessivoFeatures] Feedback chiuso. Count = {_activeFeedbackCount}"
        );

        if (_activeFeedbackCount == 0)
        {
            Debug.Log(
                "[RiflessivoFeatures] Ultimo feedback chiuso -> reset effetti."
            );

            if (_resetCoroutine != null)
                SafeRunner?.StopCoroutineSafe(_resetCoroutine);

            _resetCoroutine = SafeRunner?.RunCoroutineSafe(
                WaitForVolumeAndReset(feedback)
            );
        }
    }

    public override void resetVariables()
    {
        isTimeStopFeatureEnabled = true;
        _cacheInitialized = false;

        _activeFeedbackCount = 0;

        Debug.Log(
            "[RiflessivoFeatures] Variabili resettate."
        );
    }


    // --- LOGICA ESECUZIONE EFFETTI ---

    private void ApplyReflectiveEffects(FeedbackPrefabController feedback)
    {

        if (EffectsActive == false &&
            _activeFeedbackCount != 1)
        {
            return;
        }
        EnsureCache();
        EnsureVolumeReference();

        // Cancella tutte le coroutine attive prima di avviarne di nuove
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
        // Cancella eventuale fade audio in corso prima di avviarne uno nuovo
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
        // Cancella eventuali fade audio attivi: il volume lo gestiamo qui
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

    /// <summary>
    /// Muta/smuta gli AudioSource che ignorano l'AudioListener (bypassano AudioListener.volume).
    /// </summary>
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

    /// <summary>
    /// Salva i layer mask originali di tutti i caster e li sovrascrive con solo "UI",
    /// impedendo ai controller di interagire con qualsiasi oggetto tranne le UI.
    /// </summary>
    private void RestrictCastersToUILayer()
    {
        if (_sphereOriginalMasks.Count > 0 ||
            _curveOriginalMasks.Count > 0)
        {
            return;
        }

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

    /// <summary>
    /// Ripristina i layer mask originali su tutti i caster.
    /// </summary>
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
            if (animator != null && animator.gameObject.layer != LayerMask.NameToLayer("UI"))
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

            // salva SOLO una volta
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

            if (ps == null)
                continue;

            if (state.wasPlaying)
            {
                ps.Play(true);
            }
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