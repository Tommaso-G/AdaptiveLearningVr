using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ObjectSelectedConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Custom/Object Selected";

    public override ICondition GetNewItem()
    {
        return new ObjectSelectedCondition();
    }
}