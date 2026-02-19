using NAudio.Gui;
using System;
using System.Collections;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Content.Interaction;

public class BoxPlayer : MonoBehaviour
{
    [SerializeField]
    XRPushButton upButton;
    [SerializeField]
    XRPushButton downButton;
    [SerializeField]
    XRPushButton leftButton;
    [SerializeField]
    XRPushButton rightButton;
    [SerializeField]
    Transform referenceAxis;

    Transform movingObj;

    private float width;

    private bool canMove = true;
    public bool isDownRL { get; private set; }
    public bool isDownUD { get; private set; }

    private Vector3 lastPivot;

    private Coroutine coroutine;

    private Vector3 initialPos;

    private SpecialTile lastTileHit = null;

    private enum Direction
    {
        UP,
        DOWN,
        LEFT,
        RIGHT
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movingObj = transform;
        if (movingObj != null)
        {
            Vector3 sizes = movingObj.GetComponent<Collider>().bounds.size;
            width = GetComponent<Collider>().bounds.size.x;
            initialPos = movingObj.position;
        }

        coroutine = null;
        isDownRL = false;
        isDownUD = false;

        upButton.onPress.AddListener(CallMoveUp);
        //upButton.onRelease.AddListener();

        downButton.onPress.AddListener(CallMoveDown);
        ////downButton.onRelease.AddListener();

        leftButton.onPress.AddListener(CallMoveLeft);
        ////leftButton.onRelease.AddListener();

        rightButton.onPress.AddListener(CallMoveRight);
        ////rightButton.onRelease.AddListener();
    }

    private void CallMoveRight()
    {
        if(coroutine == null && canMove)
        {
            coroutine = StartCoroutine("MoveRight");
        }
    }

    private void CallMoveLeft()
    {
        if (coroutine == null && canMove)
        {
            coroutine = StartCoroutine("MoveLeft");
        }
    }

    private void CallMoveUp()
    {
        if (coroutine == null && canMove)
        {
            coroutine = StartCoroutine("MoveUp");
        }
    }

    private void CallMoveDown()
    {
        if (coroutine == null && canMove)
        {
            coroutine = StartCoroutine("MoveDown");
        }
    }
    private IEnumerator MoveRight()
    {
        Vector3 pivot = calculatePivot(isDownRL, isDownUD, Direction.RIGHT);
        Vector3 startPos = movingObj.position;
        Quaternion startRot = movingObj.rotation;
        Vector3 offset = startPos - pivot;
        Vector3 endPos = Vector3.zero;
        Quaternion endRot = Quaternion.identity;
        Quaternion rot = Quaternion.identity;

        if (isDownUD)
        {
            rot = Quaternion.AngleAxis(-90f, referenceAxis.forward);
            endPos = pivot + rot * offset;
            endRot = rot * startRot;
        }
        else
        {
            if (isDownRL)
            {
                rot = Quaternion.AngleAxis(-90f, referenceAxis.forward);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownRL = false;
            }
            else
            {
                rot = Quaternion.AngleAxis(-90f, referenceAxis.forward);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownRL = true;
            }
        }

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            Quaternion deltaRot = Quaternion.Slerp(Quaternion.identity, rot, t);

            movingObj.position = pivot + deltaRot * offset;
            movingObj.rotation = deltaRot * startRot;

            yield return null;
        }

        movingObj.position = endPos;
        movingObj.rotation = endRot;
        coroutine = null;
        yield return null;
    }

    private IEnumerator MoveLeft()
    {
        Vector3 pivot = calculatePivot(isDownRL, isDownUD, Direction.LEFT);
        Vector3 startPos = movingObj.position;
        Quaternion startRot = movingObj.rotation;
        Vector3 offset = startPos - pivot;
        Vector3 endPos = Vector3.zero;
        Quaternion endRot = Quaternion.identity;
        Quaternion rot = Quaternion.identity;

        if (isDownUD)
        {
            rot = Quaternion.AngleAxis(90f, referenceAxis.forward);
            endPos = pivot + rot * offset;
            endRot = rot * startRot;
        }
        else
        {

            if (isDownRL)
            {
                rot = Quaternion.AngleAxis(90f, referenceAxis.forward);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownRL = false;
            }
            else
            {
                rot = Quaternion.AngleAxis(90f, referenceAxis.forward);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownRL = true;
            }
        }

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            Quaternion deltaRot = Quaternion.Slerp(Quaternion.identity, rot, t);

            movingObj.position = pivot + deltaRot * offset;
            movingObj.rotation = deltaRot * startRot;
            yield return null;
        }

        movingObj.position = endPos;
        movingObj.rotation = endRot;
        coroutine = null;
        yield return null;
    }

    private IEnumerator MoveUp()
    {
        Vector3 pivot = calculatePivot(isDownRL, isDownUD, Direction.UP);
        Vector3 startPos = movingObj.position;
        Quaternion startRot = movingObj.rotation;
        Vector3 offset = startPos - pivot;
        Vector3 endPos = Vector3.zero;
        Quaternion endRot = Quaternion.identity;
        Quaternion rot = Quaternion.identity;
        if (isDownRL)
        {
            rot = Quaternion.AngleAxis(90f, referenceAxis.right);
            endPos = pivot + rot * offset;
            endRot = rot * startRot;
        }
        else
        {
            if (isDownUD)
            {
                rot = Quaternion.AngleAxis(90f, referenceAxis.right);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownUD = false;
            }
            else
            {
                rot = Quaternion.AngleAxis(90f, referenceAxis.right);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownUD = true;
            }
        }

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            Quaternion deltaRot = Quaternion.Slerp(Quaternion.identity, rot, t);

            movingObj.position = pivot + deltaRot * offset;
            movingObj.rotation = deltaRot * startRot;
            yield return null;
        }

        movingObj.position = endPos;
        movingObj.rotation = endRot;
        coroutine = null;
        yield return null;
    }

    private IEnumerator MoveDown()
    {
        Vector3 pivot = calculatePivot(isDownRL, isDownUD, Direction.DOWN);
        Vector3 startPos = movingObj.position;
        Quaternion startRot = movingObj.rotation;
        Vector3 offset = startPos - pivot;
        Vector3 endPos = Vector3.zero;
        Quaternion endRot = Quaternion.identity;
        Quaternion rot = Quaternion.identity;

        if (isDownRL)
        {
            rot = Quaternion.AngleAxis(-90f, referenceAxis.right);
            endPos = pivot + rot * offset;
            endRot = rot * startRot;
        }
        else
        {
            if (isDownUD)
            {
                rot = Quaternion.AngleAxis(-90f, referenceAxis.right);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownUD = false;
            }
            else
            {
                rot = Quaternion.AngleAxis(-90f, referenceAxis.right);
                endPos = pivot + rot * offset;
                endRot = rot * startRot;
                isDownUD = true;
            }
        }

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.5f;
            Quaternion deltaRot = Quaternion.Slerp(Quaternion.identity, rot, t);

            movingObj.position = pivot + deltaRot * offset;
            movingObj.rotation = deltaRot * startRot;
            yield return null;
        }

        movingObj.position = endPos;
        movingObj.rotation = endRot;
        coroutine = null;
        yield return null;
    }

    private Vector3 calculatePivot(bool isDownRL, bool isDownUD, Direction dir)
    {
        Vector3 pivot = Vector3.zero;
        if (isDownRL)
        {
            switch (dir)
            {
                case Direction.UP:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) + referenceAxis.forward * (width / 2);
                    break;
                case Direction.DOWN:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) - referenceAxis.forward * (width / 2);
                    break;
                case Direction.LEFT:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) - referenceAxis.right * width;
                    break;
                case Direction.RIGHT:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) + referenceAxis.right * width;
                    break;
            }
        }
        else if (isDownUD)
        {
            switch (dir)
            {
                case Direction.UP:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) + referenceAxis.forward * width;
                    break;
                case Direction.DOWN:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) - referenceAxis.forward * width;
                    break;
                case Direction.LEFT:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) - referenceAxis.right * (width / 2);
                    break;
                case Direction.RIGHT:
                    pivot = movingObj.position - referenceAxis.up * (width / 2) + referenceAxis.right * (width / 2);
                    break;
            }
        }
        else
        {
            switch (dir)
            {
                case Direction.UP:
                    pivot = movingObj.position - referenceAxis.up * width + referenceAxis.forward * (width / 2);
                    break;
                case Direction.DOWN:
                    pivot = movingObj.position - referenceAxis.up * width - referenceAxis.forward * (width / 2);
                    break;
                case Direction.LEFT:
                    pivot = movingObj.position - referenceAxis.up * width - referenceAxis.right * (width / 2);
                    break;
                case Direction.RIGHT:
                    pivot = movingObj.position - referenceAxis.up * width + referenceAxis.right * (width / 2);
                    break;
            }
        }
        lastPivot = pivot;
        return pivot;
    }

    public void Reset()
    {
        isDownRL = false;
        isDownUD = false;
        movingObj.position = initialPos;
        movingObj.rotation = Quaternion.Euler(0f, 0f, 0f);
        movingObj.GetComponent<Rigidbody>().isKinematic = true;
        lastTileHit = null;
        canMove = true;
    }
    private void OnDrawGizmos()
    {
        // disegna il pivot come sfera rossa
        Gizmos.color = UnityEngine.Color.red;
        Gizmos.DrawSphere(lastPivot, 0.05f);
    }

    private void CheckFall()
    {
        Vector3 rayUpPos = movingObj.position + movingObj.up * 0.5f * width;
        Vector3 rayDownPos = movingObj.position - movingObj.up * 0.5f * width;
        Ray upRay = new Ray(rayUpPos, -referenceAxis.up);
        Ray downRay = new Ray(rayDownPos, -referenceAxis.up);

        if (Physics.Raycast(upRay, out RaycastHit upHit, width) && upHit.collider.CompareTag("FallTile"))
        {
            Debug.DrawRay(rayUpPos, -referenceAxis.up * width, UnityEngine.Color.red);
            SpecialTile tile = upHit.collider.GetComponent<SpecialTile>();

            if(tile != lastTileHit && lastTileHit == null)
            {
                lastTileHit = tile;
                tile.TileEffect();
                print("Hit tile UP");
                canMove = false;
            }
        }else if (Physics.Raycast(downRay, out RaycastHit downHit, width) && downHit.collider.CompareTag("FallTile"))
        {
            Debug.DrawRay(rayDownPos, -referenceAxis.up * width, UnityEngine.Color.blue);
            SpecialTile tile = downHit.collider.GetComponent<SpecialTile>();

            if (tile != lastTileHit && lastTileHit == null)
            {
                lastTileHit = tile;
                tile.TileEffect();
                print("Hit tile DOWN");
                canMove = false;
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

        CheckFall();
        //if (coroutine == null && canMove)
        //{
        //    if (Input.GetKeyDown(KeyCode.H))
        //    {
        //        coroutine = StartCoroutine("MoveRight");
        //    }

        //    if (Input.GetKeyDown(KeyCode.F))
        //    {
        //        coroutine = StartCoroutine("MoveLeft");
        //    }

        //    if (Input.GetKeyDown(KeyCode.T))
        //    {
        //        coroutine = StartCoroutine("MoveUp");
        //    }

        //    if (Input.GetKeyDown(KeyCode.G))
        //    {
        //        coroutine = StartCoroutine("MoveDown");
        //    }
        //}

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Reset();

        }
    }
}
