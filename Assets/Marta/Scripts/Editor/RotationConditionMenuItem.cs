using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class RotationConditionMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Rotation Target";

    public override ICondition GetNewItem()
    {
        return new RotationCondition();
    }
}