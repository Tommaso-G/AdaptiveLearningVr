using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class FeedbackButtonClickedMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Custom/Feedback Button Clicked";

    public override ICondition GetNewItem()
    {
        return new FeedbackButtonClickedCondition();
    }
}