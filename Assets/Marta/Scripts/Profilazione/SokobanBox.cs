using System.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class SokobanBox : MonoBehaviour
{
    [SerializeField]
    Transform referenceAxis;
    [SerializeField]
    Transform target;
    [SerializeField]
    AudioSource audio;
    [SerializeField]
    SokobanManager sokobanManager;
    [SerializeField]
    bool fillerBox = false;


    private bool isMoving = false;
    private bool targetReached = false;


    private Vector3 startPos;
    private Color startCol;
    private Vector3 lastPos;

    private bool blockUp = false;
    private bool blockDown = false;
    private bool blockLeft = false;
    private bool blockRight = false;

    private float size = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startPos = transform.position;
        startCol = GetComponent<Renderer>().material.color;
        size = GetComponent<Collider>().bounds.size.x;
        sokobanManager.Register(this);
    }

    public void moveWithJoystick(float x, float y)
    {
        if (isMoving) return;
        //print("x: " + x + ", y: " + y);
        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            if (x > 0.5f)
            {
                isMoving = true;
                StartCoroutine(MoveRight());
            }
            else if (x < -0.5f)
            {
                isMoving = true;
                StartCoroutine(MoveLeft());
            }
        }
        else
        {
            if (y > 0.5f)
            {
                isMoving = true;
                StartCoroutine(MoveUp());
            }
            else if (y < -0.5f)
            {
                isMoving = true;
                StartCoroutine(MoveDown());
            }
        }
    }

    public IEnumerator MoveUp()
    {
        print("blockUp da MoveUp(): " + blockUp + " da " + this.gameObject.name);
        if (!blockUp)
        {
            lastPos = transform.position;
            transform.position = transform.position + referenceAxis.up * size;
            sokobanManager.dataSender?.AddMove();

        }
        yield return new WaitForSeconds(1f);
        isMoving = false;
    }
    public IEnumerator MoveDown()
    {
        if (!blockDown)
        {
            lastPos = transform.position;
            transform.position = transform.position - referenceAxis.up * size;
            sokobanManager.dataSender?.AddMove();

        }
        yield return new WaitForSeconds(1f);
        isMoving = false;
    }
    public IEnumerator MoveLeft()
    {
        if (!blockLeft)
        {
            lastPos = transform.position;
            transform.position = transform.position - referenceAxis.right * size;
            sokobanManager.dataSender?.AddMove();

        }

        yield return new WaitForSeconds(1f);
        isMoving = false;
    }

    public IEnumerator MoveRight()
    {
        print("blockRight da MoveRight(): " + blockRight + " da " + this.gameObject.name);
        if (!blockRight)
        {
            lastPos = transform.position;
            transform.position = transform.position + referenceAxis.right * size;
            sokobanManager.dataSender?.AddMove();
        }

        yield return new WaitForSeconds(1f);
        isMoving = false;
    }

    private void BoxRaycast()
    {
        Ray upRay = new Ray(transform.position, referenceAxis.up);
        Ray downRay = new Ray(transform.position, -referenceAxis.up);
        Ray leftRay = new Ray(transform.position, -referenceAxis.right);
        Ray rightRay = new Ray(transform.position, referenceAxis.right);
        Ray bottomRay = new Ray(transform.position, referenceAxis.forward);

        bool hitUp = Physics.Raycast(upRay, out RaycastHit upHit, size);
        bool hitDown = Physics.Raycast(downRay, out RaycastHit downHit, size);
        bool hitRight = Physics.Raycast(rightRay, out RaycastHit rightHit, size);
        bool hitLeft = Physics.Raycast(leftRay, out RaycastHit leftHit, size);

        bool boxUp = hitUp && upHit.collider != GetComponent<Collider>() && upHit.collider.CompareTag("Box");
        bool boxDown = hitDown && downHit.collider != GetComponent<Collider>() && downHit.collider.CompareTag("Box");
        bool boxRight = hitRight && rightHit.collider != GetComponent<Collider>() && rightHit.collider.CompareTag("Box");
        bool boxLeft = hitLeft && leftHit.collider != GetComponent<Collider>() && leftHit.collider.CompareTag("Box");

        if (Physics.Raycast(bottomRay, out RaycastHit bottomHit, size))
        {
            Debug.DrawRay(transform.position, referenceAxis.forward * size, UnityEngine.Color.red);

            if (bottomHit.collider.CompareTag("Hole"))
            {
                transform.position = bottomHit.transform.position;

                if (fillerBox)
                {
                    targetReached = true;
                    isMoving = true;
                    sokobanManager.TargetReached();
                    Deselect();
                    blockUp = true;
                    blockDown = true;
                    blockRight = true;
                    blockLeft = true;
                }

            }
            else if (bottomHit.collider.transform == target && !targetReached)
            {
                targetReached = true;
                isMoving = true;
                sokobanManager.TargetReached();
                Deselect();
                audio?.Play();
                blockUp = true;
                blockDown = true;
                blockRight = true;
                blockLeft = true;
            }
        }

        if (hitUp || hitDown)
        {
            if (boxUp || boxDown)
            {
                blockUp = boxUp;
                blockDown = boxDown;
                //print("blockUp da Raycast(): " + blockUp + " blockDown da Raycast(): " + blockDown);
            }
            else
            {
                Debug.DrawRay(transform.position, referenceAxis.up * size, UnityEngine.Color.red);
                blockUp = true;
                blockDown = true;
                //print("blocco parete");
            }
        }
        else
        {
            blockUp = false;
            blockDown = false;
        }


        if (hitRight || hitLeft)
        {
            if (boxRight || boxLeft)
            {
                blockRight = boxRight;
                blockLeft = boxLeft;
                //print("blockRight da Raycast(): " + blockRight + " blockLeft da Raycast(): " + blockLeft);
            }
            else
            {
                Debug.DrawRay(transform.position, -referenceAxis.right * 0.5f * size, UnityEngine.Color.red);
                blockRight = true;
                blockLeft = true;
                //print("Blocco parete sd/dx");
            }
        }
        else
        {
            blockRight = false;
            blockLeft = false;
        }
    }

    public void Select()
    {
        if (targetReached) return;

        gameObject.GetComponent<Renderer>().material.color = Color.blue;
        isMoving = false;
        sokobanManager.Select(this);
    }

    public void Deselect()
    {
        isMoving = true;
        gameObject.GetComponent<Renderer>().material.color = startCol;
    }

    public void Reset()
    {
        transform.position = startPos;
        targetReached = false;
        blockUp = false;
        blockDown = false;
        blockRight = false;
        blockLeft = false;
        Deselect();

    }

    public void StartMoveUp()
    {
        print("da Up: " + isMoving);
        if (isMoving) return;

        isMoving = true;
        StartCoroutine(MoveUp());
    }

    public void StartMoveDown()
    {
        if (isMoving) return;

        isMoving = true;
        StartCoroutine(MoveDown());
    }

    public void StartMoveLeft()
    {
        if (isMoving) return;

        isMoving = true;
        StartCoroutine(MoveLeft());
    }

    public void StartMoveRight()
    {
        if (isMoving) return;

        isMoving = true;
        StartCoroutine(MoveRight());
    }

    public void StepBack()
    {
        if (targetReached) return;
        transform.position = lastPos;
    }

    public void AutoCompletition()
    {
        targetReached = true;
        isMoving = true;
        sokobanManager.TargetReached();
        Deselect();
        blockUp = true;
        blockDown = true;
        blockRight = true;
        blockLeft = true;
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    Reset();
        //}

        if (targetReached) return;

        BoxRaycast();

        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            AutoCompletition();
        }
        //if (Input.GetKeyDown(KeyCode.G))
        //{
        //    isMoving = true;
        //    StartCoroutine(MoveUp());
        //}

        //if (Input.GetKeyDown(KeyCode.B))
        //{
        //    isMoving = true;
        //    StartCoroutine(MoveDown());
        //}

        //if (Input.GetKeyDown(KeyCode.N))
        //{
        //    isMoving = true;
        //    StartCoroutine(MoveRight());
        //}

        //if (Input.GetKeyDown(KeyCode.V))
        //{
        //    isMoving = true;
        //    StartCoroutine(MoveLeft());
        //}
    }
}
