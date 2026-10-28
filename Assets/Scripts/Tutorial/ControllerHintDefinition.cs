using UnityEngine;
using UnityEngine.XR;

[CreateAssetMenu(menuName = "VR Game/Tutorial/Controller Hint")]
public sealed class ControllerHintDefinition : ScriptableObject
{
    public XRNode hand = XRNode.LeftHand;
    public GameObject controllerPrefab;
    public string anchorBone = "left_b_trigger_grip";
    public string prefix = "PRESS To ";
    public string action = "GRAB";
    public string targetTag = "Wire";
    public string targetName;
    [Min(0.1f)] public float distance = 2.5f;
    public bool animateTarget = true;
    public TutorialAttentionStyle attentionStyle;
    public Vector2 normalTextOffset = new Vector2(-0.23f, -0.28f);
    public float rightTextOffset = 0.45f;
    public float forwardTextOffset = 0.04f;
    [Min(0.01f)] public float revealDuration = 0.65f;
    [Min(0.1f)] public float shrinkDuration = 1.25f;
    [Min(0f)] public float exitDelay = 0.2f;
}
