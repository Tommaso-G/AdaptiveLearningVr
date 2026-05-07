using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OutlineBlinkController : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private List<GameObject> targets = new List<GameObject>();

    [Header("Pulse Settings")]
    [SerializeField] private float minOutlineWidth = 2f;
    [SerializeField] private float maxOutlineWidth = 8f;
    [SerializeField] private float halfCycleDuration = 0.5f;

    [SerializeField] private bool autoStart = true;

    private readonly List<Outline> outlines = new List<Outline>();
    private Coroutine pulseCoroutine;

    void Start()
    {
        if (autoStart)
            StartBlink();
    }

    // ---------------------------------------------------------------------
    // PUBLIC API
    // ---------------------------------------------------------------------

    public void StartBlink()
    {
        CacheOutlines();

        if (outlines.Count == 0)
        {
            Debug.LogWarning("[OutlineBlink] Nessun Outline trovato nei targets.");
            return;
        }

        StopBlink();
        SetEnabled(true);

        pulseCoroutine = StartCoroutine(Pulse());
    }

    public void StopBlink()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        SetEnabled(false);
    }

    // ---------------------------------------------------------------------
    // CORE LOGIC
    // ---------------------------------------------------------------------

    private void CacheOutlines()
    {
        outlines.Clear();

        foreach (var go in targets)
        {
            if (go == null) continue;

            var found = go.GetComponentsInChildren<Outline>(true);

            foreach (var o in found)
            {
                if (o != null && !outlines.Contains(o))
                {
                    outlines.Add(o);
                }
            }
        }
    }

    private void SetEnabled(bool state)
    {
        foreach (var o in outlines)
        {
            if (o == null) continue;

            o.enabled = state;

            if (state)
                o.OutlineWidth = minOutlineWidth;
        }
    }

    private IEnumerator Pulse()
    {
        float lo = Mathf.Min(minOutlineWidth, maxOutlineWidth);
        float hi = Mathf.Max(minOutlineWidth, maxOutlineWidth);

        float t = 0f;
        bool increasing = true;

        while (true)
        {
            t += Time.deltaTime / halfCycleDuration;

            if (t >= 1f)
            {
                t = 0f;
                increasing = !increasing;
            }

            float normalized = increasing ? t : 1f - t;

            float width = Mathf.Lerp(lo, hi, normalized);

            for (int i = outlines.Count - 1; i >= 0; i--)
            {
                if (outlines[i] == null) continue;
                outlines[i].OutlineWidth = width;
            }

            yield return null;
        }
    }

    // ---------------------------------------------------------------------
    // EDITOR HELPERS
    // ---------------------------------------------------------------------

    public void AddTarget(GameObject go)
    {
        if (go != null && !targets.Contains(go))
            targets.Add(go);
    }

    public void ClearTargets()
    {
        targets.Clear();
    }
}