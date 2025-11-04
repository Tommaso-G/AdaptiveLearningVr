using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ObjectDeletedConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Object Deleted";

    public override ICondition GetNewItem()
    {
        return new ObjectDeletedCondition();
    }
}