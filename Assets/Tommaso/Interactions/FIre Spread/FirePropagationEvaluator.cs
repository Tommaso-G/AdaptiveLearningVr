using UnityEngine;
using System.Collections;
using System.Linq;

public class FirePropagationEvaluator : MonoBehaviour
{
    public LayerMask flammableLayer;

    private void OnEnable()
    {
        StartCoroutine(CheckPropagationAfterDelay());
    }

    private IEnumerator CheckPropagationAfterDelay()
    {
        yield return new WaitForSeconds(100f);
        float level = GetGlobalPropagationLevel();
        Debug.Log($"[FirePropagationEvaluator] Livello di propagazione dopo 100s: {level}");
    }

    public float GetGlobalPropagationLevel()
    {
        ProximitySpawner[] allSpawners = FindObjectsByType<ProximitySpawner>(FindObjectsSortMode.None);

        int maxSpawners = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Count(go => ((1 << go.layer) & flammableLayer.value) != 0);

        if (maxSpawners == 0)
            return 0f;

        // Spawn progress: quanti spawner sono presenti rispetto al massimo
        float spawnProgress = Mathf.Clamp01((float)allSpawners.Length / maxSpawners);

        // Scale progress: media della scalatura di tutti gli spawner presenti
        float scaleProgress = 0f;
        if (allSpawners.Length > 0)
        {
            float totalScale = 0f;
            foreach (var spawner in allSpawners)
            {
                totalScale += Mathf.Clamp01(
                    (spawner.transform.localScale.x - spawner.spawnInitialScale) /
                    (spawner.targetScale - spawner.spawnInitialScale)
                );
            }
            scaleProgress = totalScale / allSpawners.Length;
        }

        float result = (spawnProgress * 0.5f + scaleProgress * 0.5f);
        return Mathf.Round(result * 100f);
    }
}