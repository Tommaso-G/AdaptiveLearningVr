using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class GetStepObjMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Get object reference";

    public override ICondition GetNewItem()
    {
        return new GetStepObj();
    }
}