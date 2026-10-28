using UnityEngine;

public sealed class TutorialButtonPressAnimation : MonoBehaviour
{
    [SerializeField] private Vector3 pressDirectionLocal = Vector3.right;
    [SerializeField] private float pressDepthMetres = 0.004f;
    [SerializeField] private float cycleDuration = 2.4f;
    private Vector3 restPosition;
    private float elapsed;
    private bool followLeftGrip;

    public void FollowLeftGrip()
    {
        followLeftGrip = true;
    }

    public void SetPressDirection(Vector3 direction)
    {
        pressDirectionLocal = direction.normalized;
    }

    private void Awake() { restPosition = transform.localPosition; }

    private void Update()
    {
        elapsed = Mathf.Repeat(elapsed + Time.unscaledDeltaTime, cycleDuration);
        float press = followLeftGrip ? TutorialLeftControllerTracking.Grip()
            : 0.5f - 0.5f * Mathf.Cos(elapsed * 2f * Mathf.PI / cycleDuration);
        float metresPerLocalUnit = transform.parent.TransformVector(pressDirectionLocal).magnitude;
        transform.localPosition = restPosition + pressDirectionLocal
            * (pressDepthMetres / Mathf.Max(0.000001f, metresPerLocalUnit)) * press;
    }
}
