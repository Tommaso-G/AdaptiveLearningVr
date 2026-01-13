
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class SequentialFeedbackBehaviourMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/GetFeedbackPosition";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new GetFeedbackPosition();
    }
}
