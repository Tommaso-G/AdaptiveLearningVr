using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.UI;

public class SlidingTile : MonoBehaviour
{
    [SerializeField]
    private Transform referenceAxis;

    public GameObject nextPosPrefab;

    private float size;

    private List<GameObject> instatiatedPoses = new List<GameObject>();

    private bool selected = false;

    [SerializeField]
    private Transform targetPos;

    [SerializeField]
    private SlidingTilesMgr mgr;

    public bool on {  get; private set; }
    public bool onTarget = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        size = transform.GetComponent<Collider>().bounds.size.x;
    }

    private void Raycast()
    {
        Ray upRay = new Ray(transform.position, referenceAxis.up);
        Ray downRay = new Ray(transform.position, -referenceAxis.up);
        Ray leftRay = new Ray(transform.position, -referenceAxis.right);
        Ray rightRay = new Ray(transform.position, referenceAxis.right);

        LayerMask mask = ~LayerMask.GetMask("Ignore Raycast");

        if (Physics.Raycast(upRay, out RaycastHit upHit, 6 * size, mask))
        {
            Debug.DrawRay(transform.position, referenceAxis.up * size, UnityEngine.Color.red);

            if (upHit.collider.CompareTag("Untagged"))
            {
                Vector3 nextPos = upHit.collider.transform.position;
                nextPos.z = nextPos.z - size;

                if (nextPos != transform.position)
                {
                    Ray checkRay = new Ray(nextPos - referenceAxis.forward * 2 * size, referenceAxis.forward);

                    Debug.DrawRay(checkRay.origin, referenceAxis.forward * 2 * size, UnityEngine.Color.red);

                    if (!Physics.Raycast(checkRay, out RaycastHit checkHit, 2 * size, mask))
                    {
                        InstatiateNextPos(nextPos);
                    }
                }
            }
            else if(upHit.collider.CompareTag("Box")){

                Vector3 nextPos = upHit.collider.transform.position;

                InstatiateNextPos(nextPos);
            }
        }


        if (Physics.Raycast(downRay, out RaycastHit downHit, 6 * size, mask))
        {
            Debug.DrawRay(transform.position, - referenceAxis.up * size, UnityEngine.Color.red);

            if (downHit.collider.CompareTag("Untagged"))
            {
                Vector3 nextPos = downHit.collider.transform.position;
                nextPos.z = nextPos.z + size;

                if (nextPos != transform.position)
                {
                    Ray checkRay = new Ray(nextPos - referenceAxis.forward * 2 * size, referenceAxis.forward);

                    Debug.DrawRay(checkRay.origin, referenceAxis.forward * 2 * size, UnityEngine.Color.red);

                    if (!Physics.Raycast(checkRay, out RaycastHit checkHit, 2 * size, mask))
                    {
                        InstatiateNextPos(nextPos);
                    }
                }
            }
            else if(downHit.collider.CompareTag("Box")){
                Vector3 nextPos = downHit.collider.transform.position;

                InstatiateNextPos(nextPos);
            }
        }


        if (Physics.Raycast(rightRay, out RaycastHit rightHit, 6 * size, mask))
        {
            Debug.DrawRay(transform.position, referenceAxis.right * size, UnityEngine.Color.red);

            if (rightHit.collider.CompareTag("Untagged"))
            {
                Vector3 nextPos = rightHit.collider.transform.position;
                nextPos.x = nextPos.x - size;

                if (nextPos != transform.position)
                {
                    Ray checkRay = new Ray(nextPos - referenceAxis.forward * 2 * size, referenceAxis.forward);

                    Debug.DrawRay(checkRay.origin, referenceAxis.forward * 2 * size, UnityEngine.Color.red);

                    if (!Physics.Raycast(checkRay, out RaycastHit checkHit, 2 * size, mask))
                    {
                        InstatiateNextPos(nextPos);
                    }
                }
            }
            else if(rightHit.collider.CompareTag("Box")){
                Vector3 nextPos = rightHit.collider.transform.position;

                InstatiateNextPos(nextPos);
            }
        }


        if (Physics.Raycast(leftRay, out RaycastHit leftHit, 6 * size, mask))
        {
            Debug.DrawRay(transform.position, -referenceAxis.right * size, UnityEngine.Color.red);

            if (leftHit.collider.CompareTag("Untagged"))
            {
                Vector3 nextPos = leftHit.collider.transform.position;
                nextPos.x = nextPos.x + size;

                if (nextPos != transform.position)
                {
                    Ray checkRay = new Ray(nextPos - referenceAxis.forward * 2 * size, referenceAxis.forward);

                    Debug.DrawRay(checkRay.origin, referenceAxis.forward * 2 * size, UnityEngine.Color.red);

                    if (!Physics.Raycast(checkRay, out RaycastHit checkHit, 2 * size, mask))
                    {
                        InstatiateNextPos(nextPos);
                    }
                }
            }
            else if(leftHit.collider.CompareTag("Box")){
                Vector3 nextPos = leftHit.collider.transform.position;

                InstatiateNextPos(nextPos);
            }
        }
    }

    private void InstatiateNextPos(Vector3 nextPos)
    {
        GameObject newPos = Instantiate(nextPosPrefab, nextPos, Quaternion.identity, transform.parent);
        Button btn = newPos.GetComponentInChildren<Button>();
        btn.onClick.AddListener(() => MoveToNextPos(newPos.transform));
        instatiatedPoses.Add(newPos);
    }

    public void MoveToNextPos(Transform t)
    {
        transform.position = t.position;
        checkPos();
        Deselected();
    }

    private void checkPos()
    {
        on = transform.position == targetPos.position;
        mgr.TileOnTarget();
    }

    public void Selected()
    {
        if (selected) return;
        if (onTarget) return;
        selected = true;
        Raycast();
    }

    public void Deselected()
    {
        if (instatiatedPoses == null) return;
        foreach(GameObject g in instatiatedPoses)
        {
            Destroy(g);
            print("Distrutto elemento");
        }
        instatiatedPoses.Clear();
        selected = false;
    }

    private void goToTarget()
    {
        transform.position = targetPos.position;
        checkPos();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            goToTarget();
        }
    }
}
