using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class OpenBoxMenuItem : MenuItem<ICondition>
{
   public override string DisplayedName => "Custom/OpenBox";

   public override ICondition GetNewItem()
   {
       return new OpenBoxCondition();
   }
}