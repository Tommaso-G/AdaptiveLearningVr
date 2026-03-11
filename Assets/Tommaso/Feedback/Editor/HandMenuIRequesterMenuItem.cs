using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class OpenHandMenuBehaviorMenuItem : MenuItem<IBehavior>
{
    public override string DisplayedName => "Custom/Open Hand Menu";

    public override IBehavior GetNewItem()
    {
        return new OpenHandMenuBehavior();
    }
}