using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework.Constraints;
using Unity.XR.CoreUtils;
using UnityEngine.AI;

public class CorrectDoorButton : MonoBehaviour
{
    [SerializeField]
    private MapButton correctButton;
    [SerializeField]
    private MapButton[] mapButtons;
    private Transform currentBlockedDoor;
    [SerializeField]
    private List<SpawnArea> spawnPlaneGrid;
    [SerializeField]
    private Image resultImage;
    [SerializeField]
    private Sprite youImage;
    [SerializeField]
    private Sprite alertImage;
    [SerializeField]
    private Sprite unlokedImage;
    [SerializeField]
    private Sprite lockedImage;
    [SerializeField]
    private Sprite correctImage;
    [SerializeField]
    private Sprite wrongImage;
    [SerializeField]
    private NavMeshAgent childAgent;
    private bool cliked = false;
    private bool setting = false;
    private Collider currentUITrigger;

    [SerializeField] private Transform feedbackPos;

    public ErrorReporter ErrorReporter;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mapButtons = FindObjectsByType<MapButton>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(b => b.selectableDoor).ToArray();
        SpawnArea.onSpawnAreaChange += UpdateAreaIcon;
        spawnPlaneGrid = new List<SpawnArea>();
        //setBlockedDoor();
    }

    public void CallCorrectButton(ExitDoor door)
    {
        resultImage.gameObject.SetActive(false);
        cliked = false;
        currentBlockedDoor = door.transform;
        currentUITrigger = door.triggerUICollider;
        calculateCorrectButton();
    }

    private void calculateCorrectButton()
    {
        if (currentBlockedDoor == null)
        {
            return;
        }
        // dove sono io -> currentBlockedDoor
        Vector2 currentBlockedDoorPosition = new Vector2(currentBlockedDoor.position.x, currentBlockedDoor.position.z);

        // qual è la porta più vicina
        float minSqrDistance = float.MaxValue;

        foreach (MapButton b in mapButtons)
        {
            Transform otherTransform = b.ExitDoor.transform;
            Image[] imageToSet = b.GetComponentsInChildren<Image>(true);
            imageToSet[1].gameObject.SetActive(false);

            if (otherTransform == currentBlockedDoor)
            {
                imageToSet[0].sprite = lockedImage;
                imageToSet[1].sprite = youImage;
                imageToSet[1].gameObject.SetActive(true);
                continue;
            }
            else
            {
                imageToSet[0].sprite = unlokedImage;
            }

            if (spawnPlaneGrid.Contains(b.obstacleSpawnArea?.GetComponent<SpawnArea>()))
            {
                imageToSet[1].sprite = alertImage;
                imageToSet[1].gameObject.SetActive(true);
                continue;
            }

            if (Mathf.Abs(otherTransform.position.y - currentBlockedDoor.position.y) > 10f)
            {
                Debug.Log("Non sullo stesso piano");
                continue;
            }

            Vector2 otherDoor = new Vector2(otherTransform.position.x, otherTransform.position.z);

            float sqrDist = (otherDoor - currentBlockedDoorPosition).sqrMagnitude;

            if (sqrDist < minSqrDistance)
            {
                minSqrDistance = sqrDist;
                correctButton = b;
            }
        }
    }
    public void CheckCorrectButton(MapButton button)
    {
        if (!cliked)
        {
            if (button.ExitDoor.transform == currentBlockedDoor)
            {
                return;
            }

            if (button == correctButton)
            {
                resultImage.sprite = correctImage;
            }
            else
            {
                resultImage.sprite = wrongImage;
                wrongButtonSelected();
            }

            resultImage.gameObject.SetActive(true);
            currentUITrigger.enabled = false;
            cliked = true;
        }

    }

    private void wrongButtonSelected()
    {
        if (ErrorReporter != null)
        {
            ErrorReporter.RegisterError(gameObject.name);
        }
        else
        {
            Debug.LogError("[ExtinguisherStream] ErrorReport non linkato.");
        }
    }

    public void setBlockedDoor()
    {
        foreach (MapButton b in mapButtons)
        {
            ExitDoor door = b.ExitDoor;
            door.Deselect();
            bool blocked = door.blocked;
            door.isBlock(blocked);
            if (blocked)
            {
                print(b.gameObject.name);
                b.ExitDoor.triggerUICollider.enabled = true;
                feedbackPos.SetWorldPose(b.ExitDoor.feedbackPos.GetWorldPose());
                AITarget aiTarget = childAgent.GetComponent<AITarget>();
                if (!aiTarget.gameObject.activeSelf)
                {
                    aiTarget.gameObject.SetActive(true);
                }
                else
                {
                    aiTarget.resetTarget();
                }
                Transform target = b.ExitDoor.transform.Find("ChildPos");

                childAgent.Warp(target.position);
                childAgent.transform.rotation = target.rotation;

            }
        }
        setting = false;
    }

    private void UpdateAreaIcon(SpawnArea spawnArea, bool occupied)
    {
        print("OnAreaChange respond");
        if (occupied)
        {
            spawnPlaneGrid.Add(spawnArea);
            calculateCorrectButton();
        }
        else
        {
            spawnPlaneGrid.Remove(spawnArea);
            calculateCorrectButton();
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O) && !setting)
        {
            setting = true;
            setBlockedDoor();
        }
    }

    private void OnDestroy()
    {
        SpawnArea.onSpawnAreaChange -= UpdateAreaIcon;
    }
}
