using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class ReportManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public ReportDialogue ReportDialogue;
    public GameObject teacherPrefab;
    public int teacherNr;
    private Teacher[] teachers;
    private int results = 0;

    void Start()
    {
        teachers = new Teacher[teacherNr];
        for(int i = 0; i < teachers.Length; i++)
        {
            GameObject newTeacher = Instantiate(teacherPrefab);
            newTeacher.transform.parent = transform;
            newTeacher.transform.localPosition = new Vector3(i * 8.0f, 0.0f, 0.0f);
            Teacher teacherInfo = newTeacher.transform.GetComponent<Teacher>();
            teachers[i] = teacherInfo;
        }

        assignDialogue(teachers);

    }

    private void assignDialogue(Teacher[] teacher)
    {
        Debug.Log("Dentro");
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
                        randomIndex = Random.Range(0, ReportDialogue.dialogueOptions.Count);
                    }
                    lastIndex = randomIndex;
                    DialogueLine randomLine = ReportDialogue.dialogueOptions[randomIndex];
                    TextMeshProUGUI textBox = dialogueUI.GetComponent<TextMeshProUGUI>();
                    teacher[i].setCorrectInputs((randomLine.present, randomLine.evacuated, randomLine.missing));
                    Debug.Log(i + ", correct inputs: " + teacher[i].getCorrectInputs());
                    if (textBox != null)
                    {
                        textBox.text = randomLine.text;
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
        t.done = false;
        Debug.Log(t.getId() + ", correct inputs: " + t.getCorrectInputs() + " , user inputs:" + t.getUserInputs() + " result: " + results);
    }

}