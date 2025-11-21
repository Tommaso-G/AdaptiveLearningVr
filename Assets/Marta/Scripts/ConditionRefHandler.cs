using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Reference
{
    public List<GameObject> objsToReference = new List<GameObject>();
    public ScriptableCondition condition;
}

public class ConditionRefHandler : MonoBehaviour
{

    public ConditionFromScript conditionFromScript;
    public List<Reference> references = new List<Reference>();
    private int iteration = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        conditionFromScript = GetComponent<ConditionFromScript>();
    }

    private void Update()
    {
        if (conditionFromScript != null)
        {
            if (iteration < references.Count)
            {
                if (conditionFromScript.conditionDone)
                {
                    //Debug.Log("creating condition...");
                    conditionFromScript.conditionDone = false;
                    assignConditionObj(references[iteration].objsToReference);
                    conditionFromScript.transitionCondition = references[iteration].condition;
                    conditionFromScript.createCondition();
                    iteration++;
                    StartCoroutine("waitForCondition");
                }
            }
            else
            {
                conditionFromScript.runProcess();
                conditionFromScript = null;
            }
        }
    }

    private IEnumerator waitForCondition()
    {
        while (!conditionFromScript.conditionDone)
        {
            yield return null;
        }
    }

   private void assignConditionObj(List<GameObject> o)
   {
       conditionFromScript.conditionObjects.Clear();
        for (int i = 0; i < o.Count; i++)
        {
            conditionFromScript.conditionObjects.Add(o[i]);
        }
    }
}
