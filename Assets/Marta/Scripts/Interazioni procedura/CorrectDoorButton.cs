using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class CorrectDoorButton : MonoBehaviour
{
    [Header("Porte")]
    [SerializeField] private List<ExitDoor> doors = new List<ExitDoor>();

    [Header("UI Mappa")]
    [SerializeField] private Sprite youImage;
    [SerializeField] private Sprite alertImage;
    [SerializeField] private Sprite lockedImage;
    [SerializeField] private Sprite correctImage;
    [SerializeField] private Sprite wrongImage;
    [SerializeField] private Image resultImage;
    [SerializeField] private Transform feedbackPos;

    [Header("AI")]
    [SerializeField] private NavMeshAgent childAgent;

    [Header("Errori")]
    public ErrorReporter ErrorReporter;

    // Stato interno
    private MapButton[] mapButtons;
    private MapButton correctButton;
    private ExitDoor blockedDoor;
    private Collider currentUITrigger;
    private List<SpawnArea> occupiedSpawnAreas = new List<SpawnArea>();
    private bool clicked = false;

    //private void Awake()
    //{
    //    // Popola mapButtons in Awake, così è pronto per Start
    //    mapButtons = FindObjectsByType<MapButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
    //        .Where(b => b.selectableDoor)
    //        .ToArray();
    //}

    private void Start()
    {
        // Popola mapButtons in Awake, così è pronto per Start
        mapButtons = FindObjectsByType<MapButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(b => b.selectableDoor)
            .ToArray();
        SpawnArea.onSpawnAreaChange += UpdateAreaIcon;
        SelectAndBlockRandomDoor();
    }

    // ── Selezione porta random ────────────────────────────────────────────

    private void SelectAndBlockRandomDoor()
    {
        if (doors == null || doors.Count == 0)
        {
            Debug.LogWarning("[CorrectDoorButton] Lista porte vuota o null!");
            return;
        }

        // Reset tutte le porte
        foreach (var door in doors)
            door.blocked = false;

        // Seleziona una porta random
        int idx = Random.Range(0, doors.Count);
        blockedDoor = doors[idx];
        blockedDoor.blocked = true;

        Debug.Log($"[CorrectDoorButton] Porta bloccata: {blockedDoor.gameObject.name}");

        ApplyBlockedDoor();
    }

    private void ApplyBlockedDoor()
    {
        foreach (MapButton b in mapButtons)
        {
            ExitDoor door = b.ExitDoor;
            door.Deselect();
            bool isBlocked = door.blocked;
            door.isBlock(isBlocked);

            if (isBlocked)
            {
                b.ExitDoor.triggerUICollider.enabled = true;
                feedbackPos.SetWorldPose(b.ExitDoor.feedbackPos.GetWorldPose());

                AITarget aiTarget = childAgent.GetComponent<AITarget>();
                aiTarget.gameObject.SetActive(true);
                aiTarget.resetTarget();

                Transform target = b.ExitDoor.transform.Find("ChildPos");
                childAgent.Warp(target.position);
                childAgent.transform.rotation = target.rotation;
            }
        }
    }

    public void CallCorrectButton(ExitDoor door)
    {
        resultImage.gameObject.SetActive(false);
        clicked = false;
        currentUITrigger = door.triggerUICollider;
        CalculateCorrectButton(door.transform);
    }

    private void CalculateCorrectButton(Transform currentDoorTransform)
    {
        Vector2 currentPos = new Vector2(currentDoorTransform.position.x, currentDoorTransform.position.z);
        float minSqrDist = float.MaxValue;
        correctButton = null;

        foreach (MapButton b in mapButtons)
        {
            Transform otherTransform = b.ExitDoor.transform;
            Image[] images = b.GetComponentsInChildren<Image>(true);
            images[1].gameObject.SetActive(false);

            if (otherTransform == currentDoorTransform)
            {
                images[0].sprite = lockedImage;
                images[1].sprite = youImage;
                images[1].gameObject.SetActive(true);
                continue;
            }

            if (b.obstacleSpawnArea != null && occupiedSpawnAreas.Contains(b.obstacleSpawnArea.GetComponent<SpawnArea>()))
            {
                images[1].sprite = alertImage;
                images[1].gameObject.SetActive(true);
                continue;
            }

            if (Mathf.Abs(otherTransform.position.y - currentDoorTransform.position.y) > 10f)
                continue;

            Vector2 otherPos = new Vector2(otherTransform.position.x, otherTransform.position.z);
            float sqrDist = (otherPos - currentPos).sqrMagnitude;

            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                correctButton = b;
            }
        }
    }

    public void CheckCorrectButton(MapButton button)
    {
        if (clicked) return;
        if (button.ExitDoor.transform == blockedDoor.transform) return;

        resultImage.sprite = button == correctButton ? correctImage : wrongImage;

        if (button != correctButton)
            WrongButtonSelected();

        resultImage.gameObject.SetActive(true);
        currentUITrigger.enabled = false;
        clicked = true;
    }

    private void WrongButtonSelected()
    {
        if (ErrorReporter != null)
            ErrorReporter.RegisterError(gameObject.name);
        else
            Debug.LogError("[CorrectDoorButton] ErrorReporter non linkato.");
    }

    private void UpdateAreaIcon(SpawnArea spawnArea, bool occupied)
    {
        if (occupied)
            occupiedSpawnAreas.Add(spawnArea);
        else
            occupiedSpawnAreas.Remove(spawnArea);

        // Ricalcola solo se c'è già una porta attiva nel trigger
        if (currentUITrigger != null && currentUITrigger.enabled == false)
            return;

        CalculateCorrectButton(blockedDoor.transform);
    }

    private void OnDestroy()
    {
        SpawnArea.onSpawnAreaChange -= UpdateAreaIcon;
    }
}