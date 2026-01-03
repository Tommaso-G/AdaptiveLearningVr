using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class VirtualGrabConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Virtual Grab";

    public override ICondition GetNewItem()
    {
        return new VirtualGrabCondition();
    }
}