using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class  DynamicObjectInColliderMenuItem : MenuItem<ICondition>
{
    public override string DisplayedName => "Custom/Dynamic Object In Collider";

    public override ICondition GetNewItem()
    {
        return new  DynamicObjectInColliderCondition();
    }
}