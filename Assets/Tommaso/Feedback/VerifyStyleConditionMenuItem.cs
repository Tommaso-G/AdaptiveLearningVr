using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

namespace VRBuilder.Editor.UI.Menu
{

    public class LearningStyleConditionMenuEntry : MenuItem<ICondition>
    {
        public override string DisplayedName => "Custom/Learning Style Condition";

        public override ICondition GetNewItem()
        {
            return new LearningStyleCondition();
        }
    }
}

