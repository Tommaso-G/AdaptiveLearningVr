using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class CheckTimerBehaviorMenuItem : MenuItem<IBehavior>
{
    public override string DisplayedName => "Custom/Check Timer";

    public override IBehavior GetNewItem()
    {
        return new CheckTimerBehavior();
    }
}