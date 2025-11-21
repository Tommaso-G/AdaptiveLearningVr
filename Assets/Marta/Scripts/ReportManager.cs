using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ReportManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ReportDialogue ReportDialogue;
    private Teacher[] teachers;
    private int results = 0;

    void Start()
    {
        teachers = GetComponentsInChildren<Teacher>();
        string lastClassId = "";
        for (int i = 0; i < teachers.Length; i++)
        {
            string classId = lastClassId;
            while (classId == lastClassId)
            {
                classId = (UnityEngine.Random.Range(1, 5)).ToString() + (char)UnityEngine.Random.Range('A', 'C');
            }
            lastClassId = classId;
            teachers[i].setClassId(classId);

        }
        assignDialogue(teachers);
    }

    private void assignDialogue(Teacher[] teacher)
    {
        int lastIndex = -1;
        for (int i = 0; i < teacher.Length; i++)
        {
            Transform informationUI = teacher[i].transform.Find("Information");
            if (informationUI != null)
            {
                Transform dialogueUI = informationUI.Find("Dialogue");
                if (dialogueUI != null)
                {
                    int randomIndex = lastIndex;
                    while (randomIndex == lastIndex)
                    {
                        randomIndex = UnityEngine.Random.Range(0, ReportDialogue.dialogueOptions.Count);
                    }
                    lastIndex = randomIndex;
                    DialogueLine randomLine = ReportDialogue.dialogueOptions[randomIndex];
                    TextMeshProUGUI textBox = dialogueUI.GetComponent<TextMeshProUGUI>();
                    teacher[i].setCorrectInputs((randomLine.present, randomLine.evacuated, randomLine.missing));
                    //Debug.Log(i + ", correct inputs: " + teacher[i].getCorrectInputs());
                    if (textBox != null)
                    {
                        textBox.text = randomLine.text;
                    }

                }
                else
                {
                    Debug.Log("DialogueUI not found");
                }

                Transform classIdUI = informationUI.Find("ClassId");
                if (classIdUI != null)
                {
                    TextMeshProUGUI textBox = classIdUI.GetComponent<TextMeshProUGUI>();
                    if (textBox != null)
                    {
                        textBox.text = "Class " + teacher[i].getClassId();
                    }

                }
                else
                {
                    Debug.Log("classIdUI not found");
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
        t.done = false;
        Debug.Log(t.getId() + ", correct inputs: " + t.getCorrectInputs() + " , user inputs:" + t.getUserInputs() + " result: " + results);
    }

}