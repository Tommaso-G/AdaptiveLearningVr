#if UNITY_EDITOR
using UnityEditor;

using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.Menu
{
    /// <summary>
    /// Aggiunge la condizione "Verifica Stato Oggetto" al menu Conditions del Process Editor.
    /// </summary>
    public class IsActiveStateMenuEntry : MenuItem<ICondition>
    {
        public override string DisplayedName => "Custom/Verifica Stato Oggetto";

        public override ICondition GetNewItem()
        {
            var condition = new IsActiveCondition();
            condition.Data.DeveEssereAttivo = true;
            return condition;
        }
    }
}
#endif
