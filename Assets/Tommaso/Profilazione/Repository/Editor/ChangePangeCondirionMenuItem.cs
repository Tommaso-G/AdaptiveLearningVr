#if UNITY_EDITOR
using UnityEditor;

using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.Menu
{
    /// <summary>
    /// Aggiunge la condizione "Wait For Feedback Page" al menu Conditions.
    /// </summary>
    public class FeedbackPageChangedMenuEntry : MenuItem<ICondition>
    {
        public override string DisplayedName => "Custom/Change Page";

        public override ICondition GetNewItem()
        {
            var condition = new FeedbackPageChangedCondition();

            // Valori di default sensati
            condition.Data.TargetPageIndex = 0;

            return condition;
        }
    }
}
#endif