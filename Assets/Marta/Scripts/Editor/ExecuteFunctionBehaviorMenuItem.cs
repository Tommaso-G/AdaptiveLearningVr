using UnityEngine;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class ExecuteFuntionBehaviorMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Execute function";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new ExecuteFuntionBehavior();
    }
}