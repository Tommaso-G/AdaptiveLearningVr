using UnityEngine;
using VRBuilder.Core;

public class InteractionListener : MonoBehaviour
{
    public ExecutionOrderController executionOrderController;

    private IProcess process;
    private InteractionSource source;

    public void Initialize(IProcess process)
    {
        this.process = process;
    }

    private void Awake()
    {
        source = GetComponentInParent<InteractionSource>(true);

        if (source == null)
        {
            Debug.LogWarning($"Impossibile attaccare InteractionListener a {gameObject.name}. Non ha una Interaction Source.");
            enabled = false;
            return;
        }

        Debug.LogWarning($"InteractionListener attaccato a {gameObject.name} o ad un suo parent.");
    }

    private void OnEnable()
    {
        if (source != null)
            source.OnInteraction += HandleInteraction;
    }

    private void OnDisable()
    {
        if (source != null)
            source.OnInteraction -= HandleInteraction;
    }

    private void HandleInteraction(InteractionData data)
    {
        if (executionOrderController == null || process == null)
            return;

        string chapterName = process.Data.Current.Data.Name;

        executionOrderController.checkForObjInStep(
            data.source,
            chapter_name: chapterName,
            data.context,
            data.errorString
        );
        Debug.Log($"InteractionListener attaccato a {gameObject.name} ha lanciato HandleInteraction.");
    }
}