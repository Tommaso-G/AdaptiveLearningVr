using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class ReportManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ReportDialogue ReportDialogue;
    private Teacher[] teachers;
    private int results = 0;

    [Header("Riferimenti")]
    public IterationDataGenerator dataGenerator;

    [Header("Dialoghi per insegnanti (ordine fisso 1-2-3)")]
    public List<DialogueLine> teacherDialogues = new List<DialogueLine>(); // size 3

    public ErrorReporter ErrorReporter;

    void Start()
    {
        teachers = GetComponentsInChildren<Teacher>();
        //string lastClassId = "";
        //for (int i = 0; i < teachers.Length; i++)
        //{
        //    string classId = lastClassId;
        //    while (classId == lastClassId)
        //    {
        //        classId = (UnityEngine.Random.Range(1, 5)).ToString() + (char)UnityEngine.Random.Range('A', 'C');
        //    }
        //    lastClassId = classId;
        //    teachers[i].setClassId(classId);

        //}
        //assignDialogue(teachers);
        if (dataGenerator.isReady)
        {
            GenerateTeacherDialogues();
        }
        else
        {
            dataGenerator.OnDocumentsGenerated += GenerateTeacherDialogues;
        }
    }

    void GenerateTeacherDialogues()
    {
        print("[ReportManager] chiamato generate Teacher Dialogue");
        for (int i = 0; i < teachers.Length; i++)
        {
            CreateDialogueForTeacher(i);
        }

        AssignDialog();
    }

    void CreateDialogueForTeacher(int index)
    {
        print("[ReportManager] chiamatocreate dialogue");
        // Numero originale della classe
        int totalStudents = dataGenerator.generatedFirstNumbers[index];

        int absentStudents = UnityEngine.Random.Range(0, 2);

        // Missing tra 0 e 2
        int missing = UnityEngine.Random.Range(0, 3);

        // Evacuated = presenti dopo malattie
        int evacuated = totalStudents - absentStudents - missing;

        // Costruiamo il testo
        string lineText = absentStudents == 0
            ? $"Di solito siamo {totalStudents} in classe. "
            : absentStudents > 1
            ? $"Di solito siamo {totalStudents} in classe, ma oggi {absentStudents} studenti non erano a scuola. "
            : $"Di solito siamo {totalStudents} in classe, ma oggi {absentStudents} studente non era a scuola. ";

        lineText += missing == 0
            ? $" Sono stati evacuati tutti."
            : missing > 1
            ? $" {missing} persone non risultano evacuate."
            : $" {missing} persona non è stata evacuata.";

        DialogueLine newLine = new DialogueLine
        {
            text = lineText,
            present = totalStudents - absentStudents,
            evacuated = evacuated,
            missing = missing
        };

        teacherDialogues.Add(newLine);
    }

    private void AssignDialog()
    {
        for (int i = 0; i < teachers.Length; i++)
        {
            Transform informationUI = teachers[i].transform.Find("Information");
            if (informationUI != null)
            {
                Transform dialogueUI = informationUI.Find("Dialogue");
                if (dialogueUI != null)
                {
                    TextMeshProUGUI textBox = dialogueUI.GetComponent<TextMeshProUGUI>();
                    teachers[i].setCorrectInputs((teacherDialogues[i].present, teacherDialogues[i].evacuated, teacherDialogues[i].missing));
                    if (textBox != null)
                    {
                        textBox.text = teacherDialogues[i].text;
                    }
                }
                else
                {
                    Debug.Log("DialogueUI not found");
                }
            }
            else
            {
                Debug.Log("information not found");
            }
        }
    }

    private void Update()
    {
        foreach (Teacher t in teachers)
        {
            if (t.done)
            {
                checkInputs(t);
            }
        }
    }
    public void checkInputs(Teacher t)
    {

        if (t.getUserInputs() == t.getCorrectInputs())
        {
            results++;
        }
        else
        {
            if (ErrorReporter != null)
            {
                ErrorReporter.RegisterError(gameObject.name);
            }
            else
            {
                Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
            }
        }

        t.done = false;
        Debug.Log(t.getId() + ", correct inputs: " + t.getCorrectInputs() + " , user inputs:" + t.getUserInputs() + " result: " + results);
    }

}