using UnityEngine;
using VRBuilder.Core.Configuration;
using System;
using VRBuilder.Core.SceneObjects;
using System.Collections.Generic;
using System.Linq;
using VRBuilder.Core;
using VRBuilder.Core.Conditions;
using System.Collections;
using VRBuilder.Core.Behaviors;
using System.Reflection;
using static UnityEngine.InputSystem.OnScreen.OnScreenStick;
using NUnit.Framework;

[System.Serializable]
public class ParallelStep
{
    public List<GameObject> steps = new List<GameObject>();
}
public class ExecutionOrderController : MonoBehaviour
{
    public Material highlightMat;
    private IProcess process;
    private IStep step;
    private IList<IStep> steps;
    private Camera camera;
    private int ParallelStepIndex;
    private List<IChapter> subch;
    private GameObject prevObj;
    [SerializeField] private Canvas lockedUI;
    [SerializeField] private List<GameObject> optionalSubChapterObjs;
    [SerializeField] private List<ParallelStep> parallelStepObjs;
    [SerializeField] private ChaptersOrderManager co_mgr;
    [SerializeField] private StepErrorTracker errorTracker;
    private Dictionary<string, List<GameObject>> subChapterObjesToMainChapter = new Dictionary<string, List<GameObject>>();
    // per evitare hihlight sovrapposti
    private Dictionary<GameObject, Coroutine> _activeFlashes = new();
    private Dictionary<GameObject, List<Material[]>> _savedMaterials = new();

    [SerializeField] private StepNameAliasMap stepAliasMap;



    private IChapter previousChapter = null;


    private enum StepObjectMode
    {
        CurrentStep,
        Initialization,
        ParallelStep,
        UpdateParallelStep,
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camera = Camera.main;
        ParallelStepIndex = -1;
        lockedUI = transform.GetComponentInChildren<Canvas>(true);
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.StepStarted += OnStepStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
        co_mgr.OnSubChapterAdded += UpdateParallelStepObjs;
        prevObj = null;

        //ProcessRunner.Events.ChapterFinished += OnChapterFinished;

    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        process = ProcessRunner.Current;
        initialization();
        ErrorEvent.SetProcess(process);
        errorTracker.InitializeChapters(process.Data.Chapters); //recupera tutti i capitoli, messo da tommaso
    }
    private void OnStepStarted(object sender, ProcessEventArgs args)
    {
        getCurrenteObjects(process.Data.Current);

        if (subch != null)
        {
            Debug.Log($"[EOC] StepStarted — controllo sottocapitoli attivi:");
            for (int i = 0; i < subch.Count; i++)
            {
                string currentStepName = subch[i].Data.Current?.Data.Name ?? "nessuno";
               // Debug.Log($"[EOC]   Sottocapitolo[{i}] '{subch[i].Data.Name}' | Stage: {subch[i].LifeCycle.Stage} | Step corrente: '{currentStepName}'");
            }
        }
    }

    private void OnChapterStarted(object sender, ProcessEventArgs args)
    {
        //getCurrenteObjects(process.Data.Current);

        if (previousChapter != null && previousChapter.Data.Name != "Starting Point")
            {
                errorTracker.NotifyChapterCompleted(previousChapter.Data.Name);
            }

        previousChapter = process.Data.Current;

    }

    private void initialization()
    {
        IList<IChapter> chapters = process.Data.Chapters;
        foreach (IChapter chapter in chapters)
        {
            if (getStepsInParallelStep(chapter, StepObjectMode.Initialization))
            {
                continue;
            }
            else
            {
                initializeObjsWithListener(chapter);
            }
        }
    }

    private void getCurrenteObjects(IChapter chapter)
    {
        if (getStepsInParallelStep(chapter, StepObjectMode.ParallelStep))
        {
            return;
        }
        else
        {
            GetStepObjects(chapter);
        }
    }

    private void UpdateParallelStepObjs(string main_chapter_name, IChapter newSubChapter)
    {
        Debug.Log($"[EOC] Chiamato UpdateParallelStepObjs per il sottocapitolo {newSubChapter.Data.Name}");
        CollectStepObjects(newSubChapter, StepObjectMode.UpdateParallelStep, main_chapter_name);
    }

    public void GetStepObjects(IChapter chapter)
    {
        //optionalSubChapterObjs.Clear();
        parallelStepObjs.Clear();
        CollectStepObjects(chapter, StepObjectMode.CurrentStep);
    }
    public void initializeObjsWithListener(IChapter chapter)
    {
        //optionalSubChapterObjs.Clear();
        parallelStepObjs.Clear();
        CollectStepObjects(chapter, StepObjectMode.Initialization);
    }

    public void GetParallelStepObjects(IChapter chapter)
    {
        CollectStepObjects(chapter, StepObjectMode.ParallelStep);
    }

    private void CollectStepObjects(IChapter chapter, StepObjectMode mode, string main_chapter_name = "NESSUN_MAIN_CHAPTER_NAME")
    {
        bool isParallelStep = mode == StepObjectMode.ParallelStep;
        bool attachListener = mode == StepObjectMode.Initialization;
        bool updatesg = mode == StepObjectMode.UpdateParallelStep;

        Debug.Log($"[EOC] CHAPTER: {chapter.Data.Name} -> CollectStepObjects mode: isParallelStep {isParallelStep} | Initialization {attachListener} | Update suchapter Objs {updatesg}");
        if (isParallelStep || attachListener || updatesg) // se non � uno step group e non sto inizilizzando, allora guardo uno step per volta
        {
            steps = chapter.Data.Steps;
        }
        else
        {
            steps.Clear();
            steps.Add(chapter.Data.Current);
        }

        foreach (IStep step in steps)
        {
            IList<ITransition> transitions = step.Data.Transitions.Data.Transitions;

            foreach (ITransition transition in transitions)
            {
                Transition t = transition as Transition;
                IList<ICondition> cconditions = t.Data.Conditions;
                foreach (ICondition condition in cconditions)
                {
                    var properties = condition.Data.GetType().GetProperties(System.Reflection.BindingFlags.Public |
                                                          System.Reflection.BindingFlags.Instance); // prendi tutte le propriet� in Data 

                    foreach (var prop in properties)
                    {
                        if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))
                        {
                            getSSO(prop, condition, isParallelStep, attachListener, updatesg, main_chapter_name);

                        }
                        else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(MultipleScenePropertyReference<>))
                        {
                            getMSPR(prop, condition, isParallelStep, attachListener, updatesg, main_chapter_name);
                        }
                    }
                }
            }
        }
    }

    private void getSSO(PropertyInfo prop, ICondition condition, bool isParallelStep, bool attachListener, bool updatesg, string main_chapter_name)
    {
        SingleSceneObjectReference ssor = prop.GetValue(condition.Data) as SingleSceneObjectReference;
        if (ssor != null)
        {
            IReadOnlyList<Guid> objectGuids = ssor.Guids;
            foreach (var guid in objectGuids)
            {
                IEnumerable<ISceneObject> sceneObjects = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetObjects(guid);

                foreach (ISceneObject sceneObject in sceneObjects)
                {
                    HandleProcessObj(sceneObject, isParallelStep, attachListener, updatesg, main_chapter_name);
                }
            }
        }
    }

    private void getMSPR(PropertyInfo prop, ICondition condition, bool isParallelStep, bool attachListener, bool updatesg, string main_chapter_name)
    {
        object multiReference = prop.GetValue(condition.Data);
        if (multiReference != null)
        {
            var guidsProperty = multiReference.GetType().GetProperty("Guids");
            IEnumerable<Guid> guids = guidsProperty.GetValue(multiReference) as IEnumerable<Guid>;

            if (guids != null)
            {
                foreach (Guid guid in guids)
                {
                    IEnumerable<ISceneObject> sceneObjects = RuntimeConfigurator.Configuration.SceneObjectRegistry.GetObjects(guid);

                    foreach (ISceneObject sceneObject in sceneObjects)
                    {
                        HandleProcessObj(sceneObject, isParallelStep, attachListener, updatesg, main_chapter_name);
                    }
                }
            }
        }
    }

    private void HandleProcessObj(ISceneObject sceneObject, bool isParallelStep, bool attachListener, bool updatesg, string main_chapter_name)
    {
        ProcessSceneObject s = sceneObject as ProcessSceneObject;
        if (s == null) return;

        GameObject go = s.GameObject;
        Debug.Log($"[EOC] Oggetto processato: {go.name}");

        if (attachListener && s.GameObject.GetComponent<InteractionListener>() == null)
        {
            InteractionListener interactionListeners = s.GameObject.AddComponent<InteractionListener>();
            interactionListeners.Initialize(process);
            interactionListeners.executionOrderController = this;
        }

        if (isParallelStep)
        {
            if (parallelStepObjs.Count > 0)
            {
                parallelStepObjs[^1].steps.Add(go);
                Debug.Log($"[EOC] Oggetto {go.name} aggiunto al sottocapitolo parallelo idx {parallelStepObjs.Count-1}");
            }
        }
        else if (!updatesg)
        {
            if (parallelStepObjs.Count == 0)
            {
                parallelStepObjs.Add(new ParallelStep());
            }
            if (parallelStepObjs[0].steps.Contains(go))
            {
                return;
            }
            parallelStepObjs[0].steps.Add(go);
        }

        if (updatesg)
        {
            //if (optionalSubChapterObjs.Contains(go))
            //{
            //    return;
            //}
            //optionalSubChapterObjs.Add(go);
            if (subChapterObjesToMainChapter.ContainsKey(main_chapter_name))
            {
                subChapterObjesToMainChapter[main_chapter_name].Add(go);
                optionalSubChapterObjs.Add(go);
            }
            else
            {
                List<GameObject> gos = new List<GameObject>();
                gos.Add(go);
                subChapterObjesToMainChapter.Add(main_chapter_name, gos);
                optionalSubChapterObjs.Add(go);
            }
        }
    }

    private bool getStepsInParallelStep(IChapter chapter, StepObjectMode mode)
    {
        optionalSubChapterObjs.Clear();
        parallelStepObjs.Clear();
        bool attachListern = mode == StepObjectMode.Initialization;
        foreach (IStep step in chapter.Data.Steps)
        {
            var behavior = step.Data.Behaviors?.Data.Behaviors.FirstOrDefault();

            if (behavior == null)
            {
                continue;
            }

            if (behavior is ExecuteChaptersBehavior executeChaptersBehavior)
            {
                subch = executeChaptersBehavior.Data.GetChildren().ToList();
                foreach (IChapter ch in subch)
                {
                    parallelStepObjs.Add(new ParallelStep());

                    if (attachListern)
                    {
                        initializeObjsWithListener(ch);
                    }
                    else
                    {
                        SubChapter sch = executeChaptersBehavior.Data.AddedSubChapters.FirstOrDefault(sch => sch.Chapter.Data.Name == ch.Data.Name);
                        if (sch == null)
                        {
                            GetParallelStepObjects(ch);
                        }
                    }

                int idx = parallelStepObjs.Count - 1;
                string objNames = string.Join(", ", parallelStepObjs[idx].steps.Select(g => g.name));
                Debug.Log($"[EOC] Sottocapitolo[{idx}] '{ch.Data.Name}' — oggetti validi: [{objNames}]");
                }
                return true;
            }
        }
        return false;
    }

    public void checkForObjInStep(GameObject go, string chapter_name, GameObject proxy = null, string errorString = "")
    {
        if (parallelStepObjs.Count == 1)
        {
            if (parallelStepObjs[0].steps.Contains(go))
            {
                Debug.Log("Oggetto " + go.gameObject.name + " nello step.");
                return;
            }

        }
        else if (parallelStepObjs.Count > 0)
        {
            // LOG: stato attuale dei sottocapitoli
            for (int i = 0; i < subch.Count; i++)
            {
                string objNames = string.Join(", ", parallelStepObjs[i].steps.Select(g => g.name));
                Debug.Log($"[EOC] Sottocapitolo[{i}] '{subch[i].Data.Name}' | Stage: {subch[i].LifeCycle.Stage} | Oggetti validi: [{objNames}]");
            }
           
            // Se non ho altri sottocapitoli iniziati -> il sottocapitolo che contiene l'oggetto diventa il corrente
            if (ParallelStepIndex == -1 || subch[ParallelStepIndex].LifeCycle.Stage == Stage.Active)
            {
                for (int i = 0; i < parallelStepObjs.Count; i++)
                {
                    if (parallelStepObjs[i].steps.Contains(go))
                    {
                        ParallelStepIndex = i;
                        break;
                    }
                    ParallelStepIndex = -1;
                }
            }

            // Se avevo un sottocapitolo iniziato -> controllo se contiene l'oggetto
            if (ParallelStepIndex != -1 && parallelStepObjs[ParallelStepIndex].steps.Contains(go))
            {
                Debug.Log("[EOC] Oggetto " + go.gameObject.name + " nello step.");
                return;
            }

            Debug.Log($"[EOC] ParallelStepIndex corrente: {ParallelStepIndex} | Sottocapitolo: {(ParallelStepIndex != -1 ? subch[ParallelStepIndex].Data.Name : "Nessuno")} | Oggetto interagito: {go.name}");

            // Se non ho trovato l'oggetto nel sottocapitolo corrente -> lo cerco nei sottocapitoli aggiunti
            if (subChapterObjesToMainChapter.TryGetValue(chapter_name, out var chapterOptionalObjecs))
            {
                if (chapterOptionalObjecs.Contains(go))
                {
                    Debug.Log("[EOC] Oggetto " + go.gameObject.name + " nello step(subChAdded).");
                    return;
                }
            }

            // Non ho trovato l'oggetto -> segno errore
            Debug.Log($"[EOC] OGGETTO SBAGLIATO: ParallelStepIndex corrente: {ParallelStepIndex} | Sottocapitolo: {(ParallelStepIndex != -1 ? subch[ParallelStepIndex].Data.Name : "Nessuno")} | Oggetto interagito: {go.name}");
        }

        if (proxy != null)
        {
            DifferentStepWarningHighlight(proxy);
            Debug.Log($"[EOC] L'oggetto sbagliato era un proxy");
        }
        else
        {
            DifferentStepWarningHighlight(go);
            //DifferentStepWarningUI(go);

        }
        string chapterName = process.Data.Current?.Data.Name ?? "Unknown Chapter";
        string stepName = process.Data.Current?.Data.Current?.Data.Name ?? "Unknown Step";

        // ← aggiungi solo questa riga
        string resolvedStepName = stepAliasMap != null ? stepAliasMap.Resolve(chapterName, stepName) : stepName;

        if (string.IsNullOrEmpty(errorString) && parallelStepObjs.Count == 1)
        {
            errorTracker.RegisterError(chapterName, resolvedStepName, go.name);  // stepName → resolvedStepName
        }
        else
        {
            Debug.Log($"[EOC] L'oggetto sbagliato aveva una custom errorString");
            string subName =
                ParallelStepIndex >= 0 && ParallelStepIndex < subch.Count
                    ? subch[ParallelStepIndex]?.Data?.Name ?? ""
                    : "";
            errorTracker.RegisterError(chapterName, resolvedStepName, errorString, subName);  // stepName → resolvedStepName
        }
    }



    public void DifferentStepWarningHighlight(GameObject go)
    {
        Debug.Log($"[EOC] DifferentStepWarningHighlight per {go.name}");
        // Ferma l'eventuale flash già in corso su questo oggetto
        // e ripristina i suoi materiali originali PRIMA di ricominciare
        if (_activeFlashes.TryGetValue(go, out Coroutine running))
        {
            StopCoroutine(running);
            _activeFlashes.Remove(go);
            Debug.Log($"[EOC] Fermato highlight per {go.name}");

            if (_savedMaterials.TryGetValue(go, out List<Material[]> saved))
            {
                Renderer[] existingRenderers = go.GetComponentsInChildren<Renderer>()
                    .Where(r => !HasExcludedTagInHierarchy(r.transform))
                    .ToArray();

                for (int i = 0; i < existingRenderers.Length && i < saved.Count; i++)
                    existingRenderers[i].materials = saved[i];

                Debug.Log($"[EOC] Ripristinati i materiali per {go.name}");
                _savedMaterials.Remove(go);
            }
        }

        // Disabilita Outline
        List<Outline> disabledOutlines = new List<Outline>();

        // Children
        foreach (Outline ol in go.GetComponentsInChildren<Outline>())
        {
            if (ol.enabled)
            {
                ol.enabled = false;
                disabledOutlines.Add(ol);
            }
        }

        // Parents
        foreach (Outline ol in go.GetComponentsInParent<Outline>())
        {
            if (ol.enabled)
            {
                ol.enabled = false;
                disabledOutlines.Add(ol);
            }
        }

        // Cattura i materiali ORIGINALI (sicuramente non highlight ora)
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>()
            .Where(r => !HasExcludedTagInHierarchy(r.transform))
            .ToArray();

        List<Material[]> orgMaterials = new();
        List<Material[]> redMaterials = new();

        foreach (var rend in renderers)
        {
            Material[] mats = rend.materials;
            orgMaterials.Add(mats);

            Material[] redArray = new Material[mats.Length];
            Array.Fill(redArray, highlightMat);
            redMaterials.Add(redArray);
            rend.materials = redArray;
        }

        // Salva gli originali nel dizionario prima di avviare
        _savedMaterials[go] = orgMaterials;

        Coroutine c = StartCoroutine(FadeColor(go, renderers, orgMaterials, redMaterials, disabledOutlines));
        _activeFlashes[go] = c;

        prevObj = go; // se prevObj serve ancora altrove
    }

    private IEnumerator FadeColor(
        GameObject go,
        Renderer[] renderers,
        List<Material[]> orgMaterials,
        List<Material[]> redMaterials,
        List<Outline> outline)
    {
        Debug.Log($"[EOC] FadeColor avviato per {renderers[0].gameObject.name}");

        yield return new WaitForSeconds(0.5f);

        for (int k = 0; k < 3; k++)
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].materials = orgMaterials[i];
            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < renderers.Length; i++)
                renderers[i].materials = redMaterials[i];
            yield return new WaitForSeconds(0.5f);
        }

        // Ripristino finale garantito
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].materials = orgMaterials[i];

        // Riabilita gli outline
        //foreach (Outline ol in outline)
        //    ol.enabled = true;

        _activeFlashes.Remove(go);
        _savedMaterials.Remove(go);
        prevObj = null;
    }
    private bool HasExcludedTagInHierarchy(Transform t)
    {
        while (t != null)
        {
            if (t.CompareTag("IconFeedback")) return true;
            t = t.parent;
        }
        return false;
    }

    //private  Renderer[] GetRendererFromProxy(GameObject go)
    //{
    //    Renderer[] renderers = null;
    //    VisualProxy proxy = go.GetComponent<VisualProxy>();
    //    if(proxy != null)
    //    {
    //       renderers = proxy.GetProxyRenderers();
    //    }

    //    return renderers;
    //}


    private void DifferentStepWarningUI(GameObject go)
    {
        if (lockedUI != null)
        {
            if (!lockedUI.gameObject.activeSelf)
            {
                Debug.Log($"Oggetto interagito: {go.name}");
                lockedUI.transform.position = camera.transform.position;
                lockedUI.transform.position += camera.transform.forward * 0.6f;
                lockedUI.transform.forward = camera.transform.forward;
                //lockedUI.transform.SetParent(camera.transform, true);

                lockedUI.gameObject.SetActive(true);
                StartCoroutine("FadeUI");
            }
        }

    }

    private IEnumerator FadeUI()
    {
        yield return new WaitForSeconds(3);

        float fadeTime = 2.0f;

        CanvasGroup img = lockedUI.GetComponent<CanvasGroup>();
        float startAlpha = img.alpha;

        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            float t = timer / fadeTime;
            float newAlpha = Mathf.Lerp(startAlpha, 0f, t);

            img.alpha = newAlpha;

            yield return null;
        }
        img.alpha = 0.0f;
        lockedUI.gameObject.SetActive(false);
        img.alpha = 1.0f;

    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
        ProcessRunner.Events.StepStarted -= OnStepStarted;
        co_mgr.OnSubChapterAdded -= UpdateParallelStepObjs;
    }
}
