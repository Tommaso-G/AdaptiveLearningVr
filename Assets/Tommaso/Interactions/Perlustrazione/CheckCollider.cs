using UnityEngine;
using System.Collections.Generic;
using VRBuilder.Core;

public class ColliderSequenceChecker : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [Tooltip("Lista di collider — l'oggetto deve entrare nel più vicino")]
    public List<Collider> colliders = new List<Collider>();


    [Tooltip("Nome dello step sbagliato da passare all'ErrorEvent")]
    public string wrongStepName = "l'uscita non era la più vicina";

    [Tooltip("Riferimento all'ExecutionOrderController per il lampeggio rosso")]
    public ExecutionOrderController executionOrderController;

    private Collider _piuVicinoCorrente;

    private void Start()
    {
        if (colliders == null || colliders.Count == 0) return;

        Collider piuVicino = null;
        float distanzaMin = float.MaxValue;

        foreach (Collider c in colliders)
        {
            if (c == null) continue;
            float dist = Vector3.Distance(transform.position, c.transform.position);
            if (dist < distanzaMin)
            {
                distanzaMin = dist;
                piuVicino = c;
            }
        }

        _piuVicinoCorrente = piuVicino;
        Debug.Log($"Collider più vicino all'avvio: {_piuVicinoCorrente?.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCompleted) return;
        if (colliders == null || colliders.Count == 0) return;
        if (!colliders.Contains(other)) return;

        string chapterName = ProcessRunner.Current?.Data.Current?.Data.Name ?? "Unknown Chapter";

        if (other == _piuVicinoCorrente)
        {
            Debug.Log($"Collider corretto: {other.name}");
        }
        else
        {
            Debug.Log($"Collider sbagliato: '{other.name}', più vicino era '{_piuVicinoCorrente?.name}'");

            if (executionOrderController != null)
                executionOrderController.DifferentStepWarningHighlight(gameObject);

            ErrorEvent.OnError?.Invoke(chapterName, wrongStepName, gameObject.name);
        }

        IsCompleted = true;
    }
}