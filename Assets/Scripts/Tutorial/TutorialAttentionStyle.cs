using UnityEngine;

[CreateAssetMenu(menuName = "VR Game/Tutorial/Object Attention Style")]
public sealed class TutorialAttentionStyle : ScriptableObject
{
    [Min(1f)] public float scale = 1.2f;
    [Min(0f)] public float scaleBreath = 0.04f;
    [Min(0f)] public float floatAmplitude = 0.015f;
    [Min(0.1f)] public float period = 4.8f;
    [Min(0f)] public float approachDistance = 0.06f;
    [Min(0.1f)] public float transitionDuration = 0.85f;
    [Range(0f, 1f)] public float glowIntensity = 0.3f;
}
