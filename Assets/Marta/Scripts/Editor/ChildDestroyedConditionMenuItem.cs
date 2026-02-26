using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ChildDestroyedConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Custom/Child Destroyed";

    public override ICondition GetNewItem()
    {
        return new ChildDestroyedCondition();
    }
}