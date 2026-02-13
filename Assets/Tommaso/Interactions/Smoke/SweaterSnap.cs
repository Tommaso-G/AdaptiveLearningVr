using System.Collections;
using UnityEngine;

public class PositionSwitcher : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; }=false;

    [Header("Riferimenti di scena")]
    public Collider areaControllata;     // collider di attivazione
    public GameObject oggettoA;          // primo oggetto da muovere
    public GameObject oggettoB;          // secondo oggetto da muovere
    public Transform empty1;             // primo target
    public Transform empty2;             // secondo target
    public GameObject oggettoDaBloccare; // oggetto di cui impostare i rigidbody su isKinematic = true dopo l’animazione

    [Header("Impostazioni animazione")]
    public float durataAnimazione = 1.5f; // durata del movimento in secondi
    public AnimationCurve curvaAnimazione = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool movimentoInCorso = false;

    public void ControllaEPosiziona()
    {
        if (movimentoInCorso)
            return;

        if (areaControllata == null || oggettoA == null || oggettoB == null || empty1 == null || empty2 == null)
        {
            Debug.LogWarning("⚠️ Mancano riferimenti nello script PositionSwitcher!");
            return;
        }

        if (areaControllata.bounds.Contains(transform.position))
        {
            //Debug.Log("✅ Oggetto dentro il collider, avvio animazione.");

            // Determina quale oggetto è più vicino a empty1
            float distanzaA = Vector3.Distance(oggettoA.transform.position, empty1.position);
            float distanzaB = Vector3.Distance(oggettoB.transform.position, empty1.position);

            GameObject vicinoAEmpty1 = distanzaA <= distanzaB ? oggettoA : oggettoB;
            GameObject vicinoAEmpty2 = vicinoAEmpty1 == oggettoA ? oggettoB : oggettoA;

            // Avvia la routine combinata
            StartCoroutine(MuoviEntrambiEOscuraFisica(vicinoAEmpty1, empty1.position, vicinoAEmpty2, empty2.position));
        }
        else
        {
            //Debug.Log("❌ Oggetto fuori dal collider, nessuna azione eseguita.");
        }
    }

    /// <summary>
    /// Muove entrambi gli oggetti in parallelo, poi imposta i Rigidbody come cinetici.
    /// </summary>
    private IEnumerator MuoviEntrambiEOscuraFisica(GameObject obj1, Vector3 dest1, GameObject obj2, Vector3 dest2)
    {
        movimentoInCorso = true;

        // Avvia entrambi i movimenti
        IEnumerator move1 = MuoviOggetto(obj1, dest1);
        IEnumerator move2 = MuoviOggetto(obj2, dest2);

        // Avviali in parallelo
        Coroutine c1 = StartCoroutine(move1);
        Coroutine c2 = StartCoroutine(move2);

        // Attendi che entrambi finiscano
        yield return c1;
        yield return c2;

        // Ora imposta i rigidbody come cinetici
        ImpostaRigidBodyKinematic(oggettoDaBloccare);

        movimentoInCorso = false;
    }

    /// <summary>
    /// Muove un oggetto verso una destinazione con interpolazione dolce.
    /// </summary>
    private IEnumerator MuoviOggetto(GameObject oggetto, Vector3 destinazione)
    {
        Vector3 partenza = oggetto.transform.position;
        float tempo = 0f;

        while (tempo < durataAnimazione)
        {
            if (oggetto == null)
                yield break;

            tempo += Time.deltaTime;
            float t = curvaAnimazione.Evaluate(tempo / durataAnimazione);
            oggetto.transform.position = Vector3.Lerp(partenza, destinazione, t);
            yield return null;
        }

        oggetto.transform.position = destinazione;
    }

    /// <summary>
    /// Imposta tutti i rigidbody su isKinematic = true.
    /// </summary>
    private void ImpostaRigidBodyKinematic(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("⚠️ Nessun oggetto da cui modificare i rigidbody!");
            return;
        }

        Rigidbody[] rigidbodies = target.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
            rb.isKinematic = true;

        IsCompleted = true;

        //Debug.Log($"🔧 Impostati {rigidbodies.Length} Rigidbody su isKinematic=true su '{target.name}' e figli (dopo il movimento).");
    }
}
