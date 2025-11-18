using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class GrabWheelConditionMenuItem : MenuItem<ICondition>
{
   public override string DisplayedName => "Custom/Grab WheelChair";

   public override ICondition GetNewItem()
   {
       return new GrabbedCondition();
   }
}