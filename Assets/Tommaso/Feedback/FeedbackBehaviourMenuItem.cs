using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class FeedbackBehaviourMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Instatiate Feedback";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new FeedbackManagerBehaviour();
    }
}
