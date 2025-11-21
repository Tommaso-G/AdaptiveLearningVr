using UnityEngine;
using VRBuilder.Core.Conditions;
using System;

[CreateAssetMenu(fileName = "GenericConditionAsset", menuName = "VRBuilder/Conditions/GenericConditionAsset")]
public class ScriptableCondition : ScriptableObject
{
    [Tooltip("Il tipo completo della condizione (includere namespace). Esempio: VRBuilder.Core.Conditions.ButtonSelectedCondition")]
    public string conditionTypeName;

    public string conditionName = "New Condition";

    /// <summary>
    /// Crea un'istanza della condizione a runtime.
    /// </summary>
    public ICondition CreateCondition()
    {
        if (string.IsNullOrEmpty(conditionTypeName))
        {
            Debug.LogError("GenericConditionAsset: conditionTypeName non impostato!");
            return null;
        }

        Type conditionType = Type.GetType(conditionTypeName);
        if (conditionType == null)
        {
            Debug.LogError($"GenericConditionAsset: tipo '{conditionTypeName}' non trovato.");
            return null;
        }

        // Crea un'istanza usando reflection
        object conditionObj = Activator.CreateInstance(conditionType);
        return conditionObj as ICondition;
    }
}
