using System;

// One analytic curve drives position, rotation and uniform scale. Its value,
// velocity and acceleration remain continuous, including when reversed midway.
// At rest, 20*t^3*(1-t)^3 adds a single ~2.8% overshoot to smootherstep.
public sealed class TutorialPanelDockMotion
{
    public readonly struct Sample
    {
        public readonly float value;
        public readonly float velocity;
        public readonly float acceleration;

        public Sample(double value, double velocity, double acceleration)
        {
            this.value = (float)value;
            this.velocity = (float)velocity;
            this.acceleration = (float)acceleration;
        }
    }

    private double startedAt;
    private double duration;
    private double target;
    private double c0, c1, c2, c3, c4, c5, c6;

    public void Retarget(float destination, float seconds, double now, bool rebound = true)
    {
        Sample current = Evaluate(now);
        startedAt = now;
        duration = Math.Max(0.1, seconds);
        target = destination;
        double delta = target - current.value;
        double v = current.velocity * duration;
        double a = current.acceleration * duration * duration;
        double bounce = rebound ? 20.0 * delta : 0.0;
        // Quintic Hermite boundary conditions plus one endpoint-flat bump.
        // Carry the incoming velocity and acceleration into the new curve.
        c0 = current.value;
        c1 = v;
        c2 = a * 0.5;
        c3 = 10.0 * delta - 6.0 * v - 1.5 * a + bounce;
        c4 = -15.0 * delta + 8.0 * v + 1.5 * a - 3.0 * bounce;
        c5 = 6.0 * delta - 3.0 * v - 0.5 * a + 3.0 * bounce;
        c6 = -bounce;
    }

    public Sample Evaluate(double now)
    {
        if (duration <= 0.0) return new Sample(target, 0.0, 0.0);
        double t = (now - startedAt) / duration;
        if (t <= 0.0) return new Sample(c0, c1 / duration, 2.0 * c2 / (duration * duration));
        if (t >= 1.0) return new Sample(target, 0.0, 0.0);
        double value = (((((c6 * t + c5) * t + c4) * t + c3) * t + c2) * t + c1) * t + c0;
        double velocity = ((((6.0 * c6 * t + 5.0 * c5) * t + 4.0 * c4) * t
            + 3.0 * c3) * t + 2.0 * c2) * t + c1;
        double acceleration = (((30.0 * c6 * t + 20.0 * c5) * t + 12.0 * c4) * t
            + 6.0 * c3) * t + 2.0 * c2;
        return new Sample(value, velocity / duration, acceleration / (duration * duration));
    }
}
