using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class SokobanManager : MonoBehaviour, ICompletableStep
{
    public bool IsCompleted { get; private set; } = false;

    [SerializeField]
    private List<SokobanBox> boxes = new();

    private int targetsReached = 0;

    private Vector2 joystickPos;
    private SokobanBox current;

    [SerializeField]
    private Image doneImg;
    public void Register(SokobanBox s)
    {
        if (!boxes.Contains(s))
            boxes.Add(s);
    }

    public void OnJoystickValueChangeY(float y)
    {
        if (current == null) return;
        joystickPos.y = y;
        current.moveWithJoystick(joystickPos.x, joystickPos.y);
    }

    public void OnJoystickValueChangeX(float x)
    {
        if (current == null) return;
        joystickPos.x = x;
        current.moveWithJoystick(joystickPos.x, joystickPos.y);
    }
    public void Select(SokobanBox s)
    {
        if (current == s) return;

        if (current != null)
            current.Deselect();

        current = s;
        current.Select();
    }

    public void TargetReached()
    {
        targetsReached++;
        if (targetsReached == boxes.Count)
        {
            doneImg.enabled = true;
            IsCompleted = true;
        }
    }

    public void Reset()
    {
        targetsReached = 0;
        doneImg.enabled = false;
        IsCompleted = false;

    }
    public void StepBackCurrent()
    {
        if (current == null) return;

        current.StepBack();
    }
}
