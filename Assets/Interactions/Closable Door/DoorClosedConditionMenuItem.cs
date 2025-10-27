using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class DoorClosedConditionMenuItem : MenuItem<ICondition>
{
    // Nome che appare nel menu (puoi cambiare il percorso "Custom" come vuoi)
    public override string DisplayedName => "Custom/Door Closed";

    // Ritorna una nuova istanza della condizione
    public override ICondition GetNewItem()
    {
        return new DoorClosed();
    }
}
