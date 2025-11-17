using UnityEngine;

public class BottlePourWithCap : MonoBehaviour
{
    [Header("Effetto particellare (acqua, liquido, ecc.)")]
    public ParticleSystem pourEffect;

    [Header("Asse di versamento (es. Z = avanti)")]
    public Vector3 pourAxis = Vector3.forward;

    [Header("Angolo di attivazione (in gradi)")]
    [Range(0f, 90f)]
    public float pourAngleThreshold = 45f;

    [Header("Riferimento al Fixed Joint (opzionale)")]
    public FixedJoint fixedJoint;

    private bool isPouring = false;
    public bool canPour = false; // diventa true quando il tappo è stato preso
    private ParticleSystem.EmissionModule emissionModule;

    void Start()
    {
        if (pourEffect != null)
            emissionModule = pourEffect.emission;

        if (fixedJoint == null)
            fixedJoint = GetComponent<FixedJoint>();
    }

    void Update()
    {
        if (!canPour || pourEffect == null)
            return;

        // Calcola quanto l’asse di versamento è inclinato rispetto alla verticale
        float dot = Vector3.Dot(transform.TransformDirection(pourAxis), Vector3.up);
        bool shouldPour = dot < Mathf.Cos(pourAngleThreshold * Mathf.Deg2Rad);

        if (shouldPour && !isPouring)
            StartPour();
        else if (!shouldPour && isPouring)
            StopPour();
    }

    private void StartPour()
    {
        isPouring = true;

        if (!pourEffect.isPlaying)
            pourEffect.Play();

        emissionModule.enabled = true;
    }

    private void StopPour()
    {
        isPouring = false;
        emissionModule.enabled = false;
    }

    

}
