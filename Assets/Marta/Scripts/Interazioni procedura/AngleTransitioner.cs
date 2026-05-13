using System.Collections;
using UnityEngine;

public class AngleTransitioner : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }

    [Header("Target")]
    public Transform target;
    public RotationAxis axis = RotationAxis.Y;

    [Header("Angles")]
    [Range(0f, 180f)] public float angleA = 0f;
    [Range(0f, 180f)] public float angleB = 90f;

    [Header("Transition")]
    public float duration = 0.5f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool _isAtA = true;
    private Coroutine _current;

    public void Toggle()
    {
        _isAtA = !_isAtA;
        StartTransition(_isAtA ? angleA : angleB);
    }

    private void StartTransition(float toAngle)
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(RotateCoroutine(GetCurrentAngle(), toAngle));
    }

    private IEnumerator RotateCoroutine(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyAngle(Mathf.LerpUnclamped(from, to, curve.Evaluate(t)));
            yield return null;
        }
        ApplyAngle(to);
        _current = null;
    }

    private void ApplyAngle(float angle)
    {
        Vector3 euler = target.localEulerAngles;
        switch (axis)
        {
            case RotationAxis.X: euler.x = angle; break;
            case RotationAxis.Y: euler.y = angle; break;
            case RotationAxis.Z: euler.z = angle; break;
        }
        target.localEulerAngles = euler;
    }

    private float GetCurrentAngle()
    {
        float raw = axis switch
        {
            RotationAxis.X => target.localEulerAngles.x,
            RotationAxis.Y => target.localEulerAngles.y,
            RotationAxis.Z => target.localEulerAngles.z,
            _ => 0f
        };
        return raw > 180f ? raw - 360f : raw;
    }
}