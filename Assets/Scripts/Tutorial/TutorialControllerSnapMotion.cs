using System;

// Timing functions shared by the display movement and distance-based fade.
public static class TutorialControllerSnapMotion
{
    public static bool GripPress(float grip, ref bool held)
    {
        bool next = held ? grip > 0.55f : grip >= 0.75f;
        bool pressed = next && !held;
        held = next;
        return pressed;
    }

    public static float Travel(float progress)
    {
        float t = Clamp01(progress);
        return t * t * t;
    }

    public static float Smooth(float progress)
    {
        float t = Clamp01(progress);
        return t * t * (3f - 2f * t);
    }

    public static float DisplayOpacity(float distance, float fadeDistance, float fadeEndDistance = 0f)
    {
        return Smooth((distance - fadeEndDistance) / Math.Max(0.001f, fadeDistance - fadeEndDistance));
    }

    private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
}
