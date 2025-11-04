using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ButtonSelectedConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Button Selected";

    public override ICondition GetNewItem()
    {
        return new ButtonSelectedCondition();
    }
}