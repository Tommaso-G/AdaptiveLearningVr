using TMPro;
using UnityEditor.UIElements;
using UnityEngine;

public class Teacher : MonoBehaviour
{
    public bool done = false;
    private int id;
    private (int, int, int) correctInputs;
    private (int, int, int) userInputs;
   
    public void setId(int id)
    {
        this.id = id;
    }

    public int getId() { return id; }

    public void setCorrectInputs((int, int, int) correctInputs)
    {
        this.correctInputs = correctInputs;
    }

    public (int, int, int) getCorrectInputs()
    {
        return correctInputs;
    }

    public void setPresent(TMP_InputField tmpField)
    {
        string present = tmpField.text;

        if (int.TryParse(present, out int number))
        {
            userInputs.Item1 = number;
        }
        else
        {
            Debug.LogWarning("Input non valido, non è un numero!");
        }
    }


    public void setEvacuated(TMP_InputField tmpField)
    {
        string evacuated = tmpField.text;

        if (int.TryParse(evacuated, out int number))
        {
            userInputs.Item2 = number;
        }
        else
        {
            Debug.LogWarning("Input non valido, non è un numero!");
        }
    }


    public void setMissing(TMP_InputField tmpField)
    {
        string missing = tmpField.text;

        if (int.TryParse(missing, out int number))
        {
            userInputs.Item3 = number;
        }
        else
        {
            Debug.LogWarning("Input non valido, non è un numero!");
        }
    }

    public (int, int, int) getUserInputs()
    {
        return userInputs;
    }

    public void setDone()
    {
        done = true;
    }
}
