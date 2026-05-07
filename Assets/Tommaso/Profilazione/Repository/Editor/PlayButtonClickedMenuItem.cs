#if UNITY_EDITOR
using UnityEditor;

using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.Menu
{
    /// <summary>
    /// Aggiunge la condizione "Play Button Clicked" al menu Conditions.
    /// </summary>
    public class PlayButtonClickedMenuEntry : MenuItem<ICondition>
    {
        public override string DisplayedName => "Custom/Play Button Clicked";

        public override ICondition GetNewItem()
        {
            return new PlayButtonClickedCondition();
        }
    }
}
#endif