using Unity.VisualScripting;
using UnityEngine;

public class RotatingCylinder : MonoBehaviour
{
    public float speed = 10f;

    public float tolerance = 0.1f;

    public enum Axis
    {
        x,
        y,
        z
    }

    public Axis rotationAxis;

    public Vector3 defaultRotation;
    public void StartRotation(float value)
    {
        float angle = value * 360f;

        if (rotationAxis == Axis.x)
        {
            transform.localRotation = Quaternion.Euler(angle, defaultRotation.y, defaultRotation.z);
        }
        else if (rotationAxis == Axis.y)
        {
            transform.localRotation = Quaternion.Euler(defaultRotation.x, angle, defaultRotation.z);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(defaultRotation.x, defaultRotation.y, angle);
        }
    }
}
