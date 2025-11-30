using System.Linq;
using UnityEngine;
using VRBuilder.Core;
using System.Collections.Generic;
using VRBuilder.Core.Entities.Factories;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.SceneObjects;
using System;
using VRBuilder.Core.Configuration;
using static UnityEngine.Rendering.GPUSort;
using JetBrains.Annotations;

public enum AddChapter
{
    Disattivo = 0,
    Attivo = 1,
    Completato = 2
}
public class EditProcess : MonoBehaviour
{
    [SerializeField] private bool addCondition;
    [SerializeField] private bool addStep;
    [SerializeField] private AddChapter addChapter;
    [SerializeField] private ScriptableCondition condition;
    [SerializeField] private string chapterToEdit;
    [SerializeField] private string previousStep;
    [SerializeField] private List<GameObject> conditionObjs; // reference che servono alla condizione
    [SerializeField] private ChaptersOrderManager co_mgr; // gestisce la lista di nodi che rappresentano i capitoli

    private IProcess process;
    private IChapter chapter;
    public int lastChapterId {  get; private set; }

    private void Start()
    {
        UnityEngine.Debug.Log("Inizio modifica processo...");
        ProcessRunner.Events.ProcessStarted += OnProcessStarted;
        ProcessRunner.Events.ChapterStarted += OnChapterStarted;
        ProcessRunner.Events.StepStarted += OnStepStarted;
        co_mgr.OnListChanged += CheckNextChapter;
    }

    private void OnProcessStarted(object sender, ProcessEventArgs args)
    {
        UnityEngine.Debug.Log("Processo iniziato!");
        process = ProcessRunner.Current;
        co_mgr.initialize(process);
        lastChapterId = process.Data.Chapters.Count - co_mgr.OptionalChapters.Count - 1;
        //process = GlobalEditorHandler.GetCurrentProcess(); // per vedere nell'editor cosa succede (non cancellare)
    }

    private void OnChapterStarted(object sender, ProcessEventArgs args)
    {
        UnityEngine.Debug.Log("Capitolo iniziato: " + args.Process.Data.Current.Data.Name);

        // se il capitolo è opzionale e non stai aggiungendo capitoli, saltalo
        if (args.Process.Data.Current.Data.Name.Contains("Optional") && addChapter != AddChapter.Attivo)
        {
            UnityEngine.Debug.Log("Capitolo opzionale");
            disableChapters(args.Process.Data.Current.Data.Name);
        }

        if (addCondition)
        {
            chapter = process.Data.Chapters.FirstOrDefault(c => c.Data.Name == chapterToEdit);
        }
    }

    private void CheckNextChapter()
    {
        if(process.Data.Chapters.IndexOf(process.Data.Current) == lastChapterId)
        {
            addChapter = AddChapter.Completato;
        }

        if (addChapter == AddChapter.Attivo)
        {
            setNextChapter(co_mgr.head);
        }
    }
    private void OnStepStarted(object sender, ProcessEventArgs args)
    {
        UnityEngine.Debug.Log("Step iniziato: " + args.Process.Data.Current.Data.Current.Data.Name);

        if (addCondition)
        {
            IStep step = chapter.Data.Steps.Last();
            newCondition(step);
        }

        if (addStep)
        {
            IStep step = EntityFactory.CreateStep("New Step");
            newStep(step);
        }
    }

    private void disableChapters(string chapter)
    {
        ProcessRunner.SkipChapters(co_mgr.OptionalChapters.Count - co_mgr.OptionalChapters.IndexOf(chapter)); // salta i capitoli opzionali rimasti
    }

    private void setNextChapter(Node currentNode)
    {
        if (process != null && co_mgr != null)
        {
            if (co_mgr.empty) // empty è vero quando leggo l'ultimo nodo
            {
                addChapter = AddChapter.Completato;
                ProcessRunner.SkipChapters(currentNode.chapterId);
                //UnityEngine.Debug.Log("AddChapter: " + addChapter);
            }
            else
            {
                addChapter = AddChapter.Attivo;

                Node optionalNext = currentNode.OptionalNext;
                if (optionalNext != null)
                {   
                    IChapter chapter = process.Data.Chapters[optionalNext.chapterId];
                    ProcessRunner.SetNextChapter(chapter); // override del capitolo successivo
                    //UnityEngine.Debug.Log("Next chapter is: " + chapter.Data.Name);
                }
            }
        }
        else
        {
            UnityEngine.Debug.Log("Reference null");
        }
    }
    private void newStep(IStep step)
    {
        chapter.Data.Steps.Add(step);
        IStep prev = chapter.Data.Steps.FirstOrDefault(s => s.Data.Name == previousStep);
        ITransition transitionToEnd = prev.Data.Transitions.Data.Transitions[0];
        ITransition transitionFromStep = EntityFactory.CreateTransition();
        transitionFromStep.Data.TargetStep = transitionToEnd.Data.TargetStep;
        transitionToEnd.Data.TargetStep = step;
        step.Data.Transitions.Data.Transitions.Add(transitionFromStep);
        step.Data.Transitions.Data.Transitions.Remove(step.Data.Transitions.Data.Transitions.FirstOrDefault(t => t.Data.TargetStep == null));
    }
    private void newCondition(IStep step)
    {
        if (step != null) // crea una transizione verso la fine del capitolo
        {
            ITransition transition = step.Data.Transitions.Data.Transitions.FirstOrDefault(t => t.Data.TargetStep == null);

            ICondition newCondition = condition.CreateCondition(); // crea la condizione desiderata
            transition.Data.Conditions.Add(newCondition); // aggiungila alla transizione
            IConditionData data = newCondition.Data;
            AssignReferences(data, conditionObjs);
            UnityEngine.Debug.Log("Condizione aggiunta a runtime");
        }
    }

    private void AssignReferences(IConditionData data, List<GameObject> references)
    {
        var properties = data.GetType().GetProperties(System.Reflection.BindingFlags.Public |
                                                      System.Reflection.BindingFlags.Instance); // prendi tutte le proprietà in Data 
        int index = 0; // inizializzazione dell'indice
        foreach (var prop in properties)
        {
            if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))// guardo se tra le proprietà c'è un SSOR
            {

                if (index < references.Count)
                {
                    GameObject go = references[index]; // prendo il primo obj

                    // Convert GameObject -> SceneObjectReference
                    var processSceneObject = go.GetComponent<ProcessSceneObject>();

                    if (processSceneObject == null)
                    {
                        processSceneObject = go.AddComponent<ProcessSceneObject>();
                        var registry = RuntimeConfigurator.Configuration.SceneObjectRegistry;
                        registry.Register(processSceneObject);
                    }

                    var ssor = new SingleSceneObjectReference(processSceneObject.Guid);


                    prop.SetValue(data, ssor); // lo assegno alla proprietà della condition
                    UnityEngine.Debug.Log("Assegnato " + go.name + " alla condizione!");

                    index++;
                }
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(MultipleScenePropertyReference<>))
            {
                if (index < references.Count)
                {
                    GameObject go = references[index];

                    // Convert GameObject -> MultipleSceneObjectReference
                    var processSceneObject = go.GetComponent<ProcessSceneObject>();
                    if (processSceneObject == null)
                    {
                        processSceneObject = go.AddComponent<ProcessSceneObject>();
                        var registry = RuntimeConfigurator.Configuration.SceneObjectRegistry;
                        registry.Register(processSceneObject);
                    }
                    var genericArg = prop.PropertyType.GetGenericArguments()[0];
                    var mspr = Activator.CreateInstance(
                        prop.PropertyType,
                        processSceneObject.Guid
                    );

                    prop.SetValue(data, mspr);
                    UnityEngine.Debug.Log("Assegnato " + go.name + " alla condizione!");

                    index++;
                }
            }
        }
    }

    private void OnDestroy()
    {
        ProcessRunner.Events.ProcessStarted -= OnProcessStarted;
        ProcessRunner.Events.ChapterStarted -= OnChapterStarted;
        ProcessRunner.Events.StepStarted -= OnStepStarted;
    }
}
