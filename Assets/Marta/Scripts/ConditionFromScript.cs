using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VRBuilder.Core;
using VRBuilder.Core.Conditions;
using VRBuilder.Core.Configuration;
using VRBuilder.Core.Editor;
using VRBuilder.Core.Editor.UI.GraphView;
using VRBuilder.Core.Editor.UndoRedo;
using VRBuilder.Core.Entities.Factories;
using VRBuilder.Core.Properties;
using VRBuilder.Core.SceneObjects;
using UnityEditor;

public class ConditionFromScript : MonoBehaviour
{
    public string chapterToEdit;
    public ScriptableCondition transitionCondition;
    public bool conditionDone;
    [Tooltip("Popolare con i GameObject che la condizione deve referenziare")]
    public List<GameObject> conditionObjects;
    private IProcess process;
    private IChapter chapter;

    private IEnumerator Start()
    {
        conditionDone = true;
        // Ottieni il path dal RuntimeConfigurator
        string processPath = RuntimeConfigurator.Instance.GetSelectedProcess();

        // Carica il processo dal file
        Task<IProcess> loadProcessTask = RuntimeConfigurator.Configuration.LoadProcess(processPath);

        while (!loadProcessTask.IsCompleted)
        {
            yield return null;
        }

        // Crea un clone del processo corrente
        IProcess loadedProcess = loadProcessTask.Result;
        VRBuilder.Core.Process orginalProcess = loadedProcess as VRBuilder.Core.Process;
        process = orginalProcess.CloneOtherName("Clone_" + Guid.NewGuid().ToString());//GlobalEditorHandler.GetCurrentProcess();

        // Stampa per debug
        UnityEngine.Debug.Log("Process clone: " + process.Data.Name);
        /*foreach (var c in process.Data.Chapters)
        {
            UnityEngine.Debug.Log("- " + c.Data.Name + " con " + c.Data.Steps.Count + " step");
        }*/

        // Trova il capitolo, avvia il processo clone
        chapter = process.Data.Chapters.FirstOrDefault(c => c.Data.Name == chapterToEdit);
    }

    public void Update()
    {
    }
    public void createCondition()
    {
        if (chapter != null)
        {

            // Ultimo step del capitolo
            IStep lastStep = null;
            if (chapter.Data.Steps.Count > 0)
            {
                lastStep = chapter.Data.Steps.Last();
            }
            if (lastStep != null)
            {
                ITransition transitionToEnd = lastStep.Data.Transitions.Data.Transitions.FirstOrDefault(t => t.Data.TargetStep == null);

                if (transitionToEnd == null)
                {
                    // Se non esiste, creala
                    transitionToEnd = EntityFactory.CreateTransition();
                    transitionToEnd.Data.TargetStep = null;
                    lastStep.Data.Transitions.Data.Transitions.Add(transitionToEnd);
                }

                // Crea la condizione e aggiungila
                ICondition condition = transitionCondition.CreateCondition();

                // oppure new MyCustomCondition();
                transitionToEnd.Data.Conditions.Add(condition);

                // Controlla se la condizione ha un campo dati contenente SingleSceneObjectReference
                var conditionDataProperty = condition.GetType().GetProperty("Data");
                if (conditionDataProperty != null)
                {
                    var data = conditionDataProperty.GetValue(condition);
                    if (data != null)
                    {
                        AssignReferencesToData(data, conditionObjects);
                    }
                }

                conditionDone = true;
                UnityEngine.Debug.Log("Condizione aggiunta a runtime");
            }
        }
        else
        {
            conditionDone = true;
            UnityEngine.Debug.LogWarning("Nessun capitolo corrente trovato!");
        }
    }

    public void runProcess()
    {
        ProcessRunner.Initialize(process);
        ProcessRunner.Run();
        UnityEngine.Debug.Log("ProcessRunner sta eseguendo: " + ProcessRunner.Current.Data.Name);
        process = null;
    }


    private void AssignReferencesToData(object data, List<GameObject> references)
    {
        var properties = data.GetType().GetProperties(System.Reflection.BindingFlags.Public |
                                                      System.Reflection.BindingFlags.Instance);

        int index = 0;

        foreach (var prop in properties)
        {
            if (typeof(SingleSceneObjectReference).IsAssignableFrom(prop.PropertyType))
            {
                //UnityEngine.Debug.Log("Trovata proprietà SingleSceneObjectReference!");

                if (index < references.Count)
                {
                    GameObject go = references[index];

                    // Convert GameObject -> SceneObjectReference
                    var processSceneObject = go.GetComponent<ProcessSceneObject>();

                    var ssor = new SingleSceneObjectReference(processSceneObject.Guid);

                    prop.SetValue(data, ssor);
                    UnityEngine.Debug.Log("Assegnato " + go.name + " alla condizione!");

                    index++;
                }
            }
            else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(MultipleScenePropertyReference<>))
            {
                //UnityEngine.Debug.Log("Trovata proprietà MultipleScenePropertyReference!");

                if (index < references.Count)
                {
                    GameObject go = references[index];

                    var processSceneObject = go.GetComponent<ProcessSceneObject>();

                    // Qui devi creare correttamente l'istanza generica
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
}

