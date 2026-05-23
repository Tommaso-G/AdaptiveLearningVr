using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ObjectEnabledMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Custom/Object Enabled";

    public override ICondition GetNewItem()
    {
        return new ObjectEnabledCondition();
    }
}