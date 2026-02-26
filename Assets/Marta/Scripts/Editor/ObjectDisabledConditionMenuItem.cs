using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ObjectDisabledConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Custom/Object Disabled";

    public override ICondition GetNewItem()
    {
        return new ObjectDisabledCondition();
    }
}