using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class GameCompletedMenuItem : MenuItem<ICondition>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Profilazione/Game Completed";

    /// <inheritdoc />
    public override ICondition GetNewItem()
    {
        return new GameCompletedCondition();
    }
}
