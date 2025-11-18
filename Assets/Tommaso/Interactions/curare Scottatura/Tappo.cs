using UnityEngine;

public class BottleCapInteractable : MonoBehaviour
{
    [Header("Riferimento alla bottiglia")]
    public BottlePourWithCap bottleScript;

    private FixedJoint fixedJoint;   // verrà trovato automaticamente
    private bool firstGrabDone = false;

    void Start()
    {
        // Cerca automaticamente un FixedJoint sullo stesso oggetto (il tappo)
        fixedJoint = GetComponent<FixedJoint>();

        if (fixedJoint == null)
        {
            Debug.LogWarning($"{name}: nessun FixedJoint trovato sul tappo!");
        }
    }

    // Funzione richiamata dall'evento OnSelectEntered di VRBuilder (o XR Grab)
    public void CapRemoved()
    {
        if (firstGrabDone)
            return;

        firstGrabDone = true;

        // 🔹 Distrugge il FixedJoint locale del tappo
        if (fixedJoint != null)
        {
            Destroy(fixedJoint);
            fixedJoint = null;
            Debug.Log($"{name}: FixedJoint distrutto, tappo staccato dalla bottiglia");
        }

        // 🔹 Avvisa la bottiglia che ora può versare
        if (bottleScript != null)
        {
            bottleScript.canPour = true;
            Debug.Log($"{bottleScript.name}: versamento abilitato (tappo rimosso)");
        }
    }
}
