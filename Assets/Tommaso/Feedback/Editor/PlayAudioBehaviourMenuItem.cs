using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class PlayAudioBehaviourMenuItem : MenuItem<IBehavior>
{
    public override string DisplayedName => "Custom/Play Audio";

    public override IBehavior GetNewItem()
    {
        return new PlayAudioBehaviour();
    }
}