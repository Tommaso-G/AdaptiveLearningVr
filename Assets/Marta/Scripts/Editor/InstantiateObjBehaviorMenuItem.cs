using UnityEngine;
using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class InstantiateObjBehaviorMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Instantiate Object";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new InstantiateObjBehavior();
    }
}