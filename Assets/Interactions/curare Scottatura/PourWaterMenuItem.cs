using VRBuilder.Core.Conditions;
using VRBuilder.Core.Editor.UI.StepInspector.Menu;

public class PourWaterConditionMenuItem : MenuItem<ICondition>
{
    // Nome che appare nel menu (puoi cambiare il percorso "Custom" come vuoi)
    public override string DisplayedName => "Custom/PourWater";

    // Ritorna una nuova istanza della condizione
    public override ICondition GetNewItem()
    {
        return new PourWaterCondition();
    }
}
