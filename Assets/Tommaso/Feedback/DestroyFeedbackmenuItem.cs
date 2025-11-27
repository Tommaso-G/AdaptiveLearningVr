using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class DestroyFeedbackMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Destroy Feedback";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new DestroyFeedbackBehaviour();
    }
}
