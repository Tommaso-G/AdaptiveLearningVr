using UnityEditor;
using UnityEngine;

public class FeedbackSpawnerWindow : EditorWindow
{
    private GameObject prefab;
    private Vector3 spawnScale = Vector3.one;

    [MenuItem("Tools/Feedback Spawner")]
    public static void ShowWindow()
    {
        GetWindow<FeedbackSpawnerWindow>("Feedback Spawner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Feedback Prefab Spawner", EditorStyles.boldLabel);

        prefab = (GameObject)EditorGUILayout.ObjectField(
            "Prefab",
            prefab,
            typeof(GameObject),
            false);

        spawnScale = EditorGUILayout.Vector3Field("Spawn Scale", spawnScale);

        GUILayout.Space(10);

        GUI.enabled = prefab != null;

        if (GUILayout.Button("Spawn"))
        {
            Spawn();
        }

        GUI.enabled = true;

        if (GUILayout.Button("Despawn"))
        {
            Despawn();
        }
    }

    private void Spawn()
    {
        feedbackPositionIcon[] icons =
            FindObjectsByType<feedbackPositionIcon>(FindObjectsSortMode.None);

        int created = 0;

        // Recupero una sola volta tutti i marker esistenti
        SpawnedFeedbackMarker[] markers =
            FindObjectsByType<SpawnedFeedbackMarker>(FindObjectsSortMode.None);

        foreach (feedbackPositionIcon icon in icons)
        {
            bool alreadyExists = false;

            foreach (var marker in markers)
            {
                if (Vector3.Distance(marker.transform.position, icon.transform.position) < 0.001f)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
                continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            instance.transform.position = icon.transform.position;
            instance.transform.rotation = icon.transform.rotation;
            instance.transform.localScale = spawnScale;
            instance.transform.SetParent(null);

            if (instance.GetComponent<SpawnedFeedbackMarker>() == null)
                instance.AddComponent<SpawnedFeedbackMarker>();

            Undo.RegisterCreatedObjectUndo(instance, "Spawn Feedback");

            created++;
        }

        Debug.Log($"Creati {created} prefab.");
    }

    private void Despawn()
    {
        SpawnedFeedbackMarker[] markers =
            FindObjectsByType<SpawnedFeedbackMarker>(FindObjectsSortMode.None);

        int removed = 0;

        foreach (var marker in markers)
        {
            Undo.DestroyObjectImmediate(marker.gameObject);
            removed++;
        }

        Debug.Log($"Eliminati {removed} prefab.");
    }
}