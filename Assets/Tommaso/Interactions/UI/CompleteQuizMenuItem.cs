using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class CompleteQuiznMenuItem : MenuItem<ICondition>
{
   public override string DisplayedName => "Custom/CompleteQuiz";

   public override ICondition GetNewItem()
   {
       return new CompleteQuizCondition();
   }
}