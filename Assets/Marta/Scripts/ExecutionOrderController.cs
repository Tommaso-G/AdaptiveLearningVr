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
    }

    private void OnChapterStarted(object sender, ProcessEventArgs args)
    {
        //getCurrenteObjects(process.Data.Current);

        if (previousChapter != null)
        {
            errorTracker.UpdateErrorPanel();
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
        Debug.Log("[ExecutionChapterController] Chiamato UpdateParallelStepObjs");
        CollectStepObjects(newSubChapter, StepObjectMode.UpdateParallelStep, main_chapter_name);
    }

    public void GetStepObjects(IChapter chapter)
    {
        optionalSubChapterObjs.Clear();
        parallelStepObjs.Clear();
        CollectStepObjects(chapter, StepObjectMode.CurrentStep);
    }
    public void initializeObjsWithListener(IChapter chapter)
    {
        optionalSubChapterObjs.Clear();
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

        if (attachListener && s.GameObject.GetComponent<InteractionListener>() == null)
        {
            InteractionListener interactionListeners = s.GameObject.AddComponent<InteractionListener>();
            interactionListeners.Initialize(process);
            interactionListeners.executionOrderController = this;
        }

        if (isParallelStep)
        {
            if (parallelStepObjs.Count > 0)
                parallelStepObjs[^1].steps.Add(go);
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
            }
            else
            {
                List<GameObject> gos = new List<GameObject>();
                gos.Add(go);
                subChapterObjesToMainChapter.Add(main_chapter_name, gos);
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
                        GetParallelStepObjects(ch);
                    }
                }
                return true;
            }
        }
        return false;
    }

    public void checkForObjInStep(GameObject go, string chapter_name, GameObject proxy = null)
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

            if (ParallelStepIndex != -1 && parallelStepObjs[ParallelStepIndex].steps.Contains(go))
            {
                Debug.Log("Oggetto " + go.gameObject.name + " nello step.");
                return;
            }
        }

        //if (optionalSubChapterObjs.Contains(go))
        //{
        //    Debug.Log("Oggetto " + go.gameObject.name + " nello step(subChAdded).");
        //    return;
        //}
        if (subChapterObjesToMainChapter.TryGetValue(chapter_name, out var chapterOptionalObjecs))
        {
            if (chapterOptionalObjecs.Contains(go))
            {
                Debug.Log("Oggetto " + go.gameObject.name + " nello step(subChAdded).");
                return;
            }
        }

        if (ParallelStepIndex != -1)
        {
            Debug.Log("Last subchapter selected " + subch[ParallelStepIndex].Data.Name + ", last subchapter LifeStage " + subch[ParallelStepIndex].LifeCycle.Stage);
        }
        else
        {
            Debug.Log("Step group index: " + ParallelStepIndex);
        }

        if (proxy != null)
        {
            DifferentStepWarningHighlight(proxy);

        }
        else
        {
            DifferentStepWarningHighlight(go);
            //DifferentStepWarningUI(go);

        }
        string chapterName = process.Data.Current?.Data.Name ?? "Unknown Chapter";
        string stepName = process.Data.Current?.Data.Current?.Data.Name ?? "Unknown Step";

        errorTracker.RegisterError(chapterName, stepName, go.name);
    }



    public void DifferentStepWarningHighlight(GameObject go)
    {
        Debug.Log($"Oggetto interagito: {go.name}");

        if (go == prevObj)
        {
            return;
        }

        prevObj = go;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        //if (renderers == null)
        //{
        //    renderers = GetRendererFromProxy(go);
        //}

        List<Material[]> orgMaterials = new List<Material[]>();
        List<Material[]> redMaterials = new List<Material[]>();

        foreach (var rend in renderers)
        {
            Material[] mats = rend.materials;
            orgMaterials.Add(mats);
            Material[] redArray = new Material[mats.Length];
            Array.Fill(redArray, highlightMat);
            redMaterials.Add(redArray);

            rend.materials = redArray;
        }

        StartCoroutine(FadeColor(renderers, orgMaterials, redMaterials));
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

    private IEnumerator FadeColor(Renderer[] renderers, List<Material[]> orgMaterials, List<Material[]> redMaterials)
    {
        yield return new WaitForSeconds(0.5f);
        int k = 0;
        while (k < 3)
        {
            k++;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].materials = orgMaterials[i];
            }
            yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].materials = redMaterials[i];
            }
            yield return new WaitForSeconds(0.5f);
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = orgMaterials[i];
        }

        prevObj = null;
    }

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
