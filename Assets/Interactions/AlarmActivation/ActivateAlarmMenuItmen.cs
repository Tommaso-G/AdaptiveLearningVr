using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ActivateAlarmMenuItem : MenuItem<ICondition>
{
   public override string DisplayedName => "Custom/ActivateAlarm";

   public override ICondition GetNewItem()
   {
       return new ActivateAlarmCondition();
   }
}