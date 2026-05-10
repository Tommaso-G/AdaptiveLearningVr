using UnityEngine;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ExceededTimeBehaviorMenuItem : MenuItem<IBehavior>
{
    public override string DisplayedName => "Custom/Chapter timer exceeded";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new ExceededTimeBehavior();
    }
}
