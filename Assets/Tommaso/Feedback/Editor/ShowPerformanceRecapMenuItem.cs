using VRBuilder.Core.Behaviors;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;
using UnityEngine;

public class ShowPerformanceRecapMenuItem : MenuItem<IBehavior>
{
    /// <inheritdoc />
    public override string DisplayedName => "Custom/Istantiate Performance Recap";

    /// <inheritdoc />
    public override IBehavior GetNewItem()
    {
        return new ShowPerformanceRecapBehaviour();
    }
}

