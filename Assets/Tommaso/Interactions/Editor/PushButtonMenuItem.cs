using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class XRButtonPressedMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "XR Button Pressed";

    public override ICondition GetNewItem()
    {
        return new XRButtonPressedCondition();
    }
}