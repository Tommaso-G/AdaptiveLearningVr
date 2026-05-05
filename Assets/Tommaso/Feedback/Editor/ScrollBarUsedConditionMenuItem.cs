using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class FeedbackScrollUsedMenuItem : MenuItem<ICondition>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Feedback Scroll Used";

    /// <inheritdoc />
    public override ICondition GetNewItem()
    {
        return new FeedbackScrollUsedCondition();
    }
}