using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class OpenUiBehaviorMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Open UI Panel";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new OpenUiBehavior();
    }
}
