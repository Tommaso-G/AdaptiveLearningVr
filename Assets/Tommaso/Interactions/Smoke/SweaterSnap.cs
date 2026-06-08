using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PositionSwitcher : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [Header("Riferimenti di scena")]
    public Collider areaControllata;
    public GameObject oggettoCheScompare;
    public GameObject oggettoCheCompare;

    [Header("Impostazioni dissolvenza")]
    public float durataDissolvenza = 1.5f;

    [Header("Eventi")]
    public UnityEvent onScompareStart;
    public UnityEvent onScompareEnd;

    private bool inCorso = false;

    private void Start()
    {
    ImpostaAlpha(oggettoCheCompare, 0f);
    }

    public void ControllaEPosiziona()
    {
        if (inCorso || IsCompleted)
            return;

        if (areaControllata == null || oggettoCheScompare == null || oggettoCheCompare == null)
        {
            Debug.LogWarning("⚠️ Mancano riferimenti nello script PositionSwitcher!");
            return;
        }

        if (areaControllata.bounds.Contains(transform.position))
        {
            StartCoroutine(EseguiDissolvenza());
        }
    }

    private IEnumerator EseguiDissolvenza()
    {
        inCorso = true;

        // Assicurati che oggettoCheScompare sia visibile e oggettoCheCompare sia invisibile
        ImpostaAlpha(oggettoCheScompare, 1f);
        ImpostaAlpha(oggettoCheCompare, 0f);
        oggettoCheCompare.SetActive(true);
        onScompareStart?.Invoke();
        float tempo = 0f;
        while (tempo < durataDissolvenza)
        {
            tempo += Time.deltaTime;
            float t = Mathf.Clamp01(tempo / durataDissolvenza);

            ImpostaAlpha(oggettoCheScompare, 1f - t);
            ImpostaAlpha(oggettoCheCompare, t);

            yield return null;
        }

        ImpostaAlpha(oggettoCheScompare, 0f);
        ImpostaAlpha(oggettoCheCompare, 1f);

        oggettoCheScompare.SetActive(false);
        onScompareEnd?.Invoke();
        IsCompleted = true;
        inCorso = false;
    }

    private void ImpostaAlpha(GameObject obj, float alpha)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor")) // URP usa _BaseColor
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
                // Se nessuna delle due esiste (come il tuo Outline Mask), salta silenziosamente
            }
        }
    }
}
