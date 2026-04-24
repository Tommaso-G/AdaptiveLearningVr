using UnityEngine;
using UnityEngine.Events;

public class QuizManager : MonoBehaviour
{
    public GameObject[] questions;
    public GameObject Uipanel;
    int currentquestion;

    public UnityEvent OnEnd;


    public void CorrectAnswer()
    {

        if (currentquestion + 1 < questions.Length)
        {
            questions[currentquestion].SetActive(false);
            currentquestion++;
            questions[currentquestion].SetActive(true);
        }
        else
        {

            OnEnd?.Invoke();
            Uipanel.SetActive(false);
        }
    }

    public void WrongAnswer()
    {
        string chapterName = ErrorEvent.process.Data.Current.Data.Name;
        string stepName = ErrorEvent.process.Data.Current.Data.Current.Data.Name;
        ErrorEvent.OnError.Invoke(chapterName, stepName, transform.name);
        CorrectAnswer();
    }
}
