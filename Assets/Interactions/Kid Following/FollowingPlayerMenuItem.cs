using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class FollowingPlayerBehaviorMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Follow Player";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new FollowPlayerBehavior();
    }
}
