using System;
using UnityEngine;

/// <summary>Ordered pairs of route marks. All times use scaled game time.</summary>
[DisallowMultipleComponent]
public sealed class AlternatingClimbFlow : MonoBehaviour
{
    [Tooltip("Bottom to top: Left 1, Right 1, Left 2, Right 2, ...")]
    public Renderer[] strokes = Array.Empty<Renderer>();
    public Camera vrCamera;
    [Tooltip("Optional movable support for consecutive strokes after the floor pair, e.g. the cart.")]
    public Transform movingSurface;
    [Min(0)] public int movingStrokeCount = 2;
    [Min(0.05f)] public float stepInterval = 0.36f;
    [Min(0)] public float pairPause = 0.24f;
    [Min(0.1f)] public float strokeDuration = 1.8f;
    [Min(0)] public float loopPause = 0;
    public bool loop = true;
    [Min(0)] public float hdrIntensity = 1f;
    [Min(0)] public float impactIntensity = 0.18f;
    [Min(0.01f)] public float impactDuration = 0.32f;
    [Min(0)] public float downwardSlide = 0.05f;
    [Range(0,0.3f)] public float timingVariation = 0.04f;
    public float noiseSeed = 17.3f;
    [Tooltip("X: normalized step index; Y: interval multiplier (clamped 0.25–3).")]
    public AnimationCurve stepTiming = new AnimationCurve(
        new Keyframe(0,1.1f), new Keyframe(0.4f,0.9f), new Keyframe(1,1.15f));
    [Tooltip("X: normalized age; Y: lifecycle. Continuous slopes avoid repeated stalls; shader eases opacity.")]
    public AnimationCurve progressSpeed = new AnimationCurve(
        new Keyframe(0,0,0.8f,0.8f), new Keyframe(0.3f,0.34f,1.05f,1.05f),
        new Keyframe(1,1,0.8f,0.8f));

    static readonly int Progress = Shader.PropertyToID("_StrokeProgress");
    static readonly int Intensity = Shader.PropertyToID("_HDRIntensity");
    static readonly int Impact = Shader.PropertyToID("_Impact");
    static readonly int Viewer = Shader.PropertyToID("_ViewerPosition");
    sealed class State
    {
        public Renderer renderer;
        public Vector3 origin;
        public Quaternion rotation;
        public Vector3 surfaceOrigin;
        public Quaternion surfaceRotation;
        public bool followsSurface;
        public MaterialPropertyBlock original, block;
        public float age = -1, progress;
    }
    State[] states;
    int next;
    float untilNext, elapsed;
    bool running;

    void OnEnable() { Restart(); }
    void OnDisable() { Restore(); }

    [ContextMenu("Restart Climb Flow")]
    public void Restart()
    {
        Restore();
        if (!isActiveAndEnabled) return;
        states = new State[strokes == null ? 0 : strokes.Length];
        for (int i=0; i<states.Length; i++)
        {
            var r = strokes[i];
            if (!r) continue;
            bool duplicate = false;
            for (int j=0; j<i; j++) if (states[j]?.renderer == r) duplicate = true;
            if (duplicate) continue;
            var s = new State { renderer=r, origin=r.transform.localPosition, rotation=r.transform.localRotation,
                original=new MaterialPropertyBlock(), block=new MaterialPropertyBlock() };
            if (movingSurface && i >= 2 && i - 2 < movingStrokeCount)
            {
                s.followsSurface = true;
                s.surfaceOrigin = movingSurface.InverseTransformPoint(r.transform.position);
                s.surfaceRotation = Quaternion.Inverse(movingSurface.rotation) * r.transform.rotation;
            }
            r.GetPropertyBlock(s.original);
            states[i]=s;
            Apply(s,1,0,0);
        }
        next=0; untilNext=0; elapsed=0; running=states.Length>0;
        if (!vrCamera) vrCamera=Camera.main;
    }

    void Restore()
    {
        if (states == null) return;
        foreach (var s in states)
        {
            if (s == null || !s.renderer) continue;
            s.renderer.transform.SetLocalPositionAndRotation(s.origin, s.rotation);
            s.renderer.SetPropertyBlock(s.original);
        }
        states=null;
        running=false;
    }

    void Update()
    {
        if (states == null) return;
        if (!vrCamera) vrCamera=Camera.main;
        float dt=Time.deltaTime;
        elapsed+=dt;
        foreach (var s in states) if (s != null && s.age>=0) s.age+=dt;
        if (running)
        {
            untilNext-=dt;
            // Bound work after a very long frame; normal frames preserve scheduled overlap.
            int safety=0;
            while (untilNext<=0 && running && safety++<64)
            {
                var s=states[next];
                if (s != null) { s.age=-untilNext; s.progress=0; }
                float index=states.Length<=1 ? 0 : (float)next/(states.Length-1);
                float multiplier=stepTiming == null ? 1 : Mathf.Clamp(stepTiming.Evaluate(index),0.25f,3);
                float organic=(Mathf.PerlinNoise(noiseSeed,elapsed*0.37f+next)-0.5f)*2*timingVariation;
                float interval=Mathf.Max(0.05f,stepInterval*multiplier*(1+organic));
                if ((next & 1) != 0) interval += pairPause;
                next++;
                if (next>=states.Length)
                {
                    next=0;
                    if (!loop) running=false;
                    interval+=Mathf.Max(0,loopPause);
                }
                // Restart a loop while the upper marks are still fading. Never retrigger
                // an individual mark before its own fade completes (also supports 4 marks).
                var upcoming=states[next];
                if (upcoming != null && upcoming.age>=0)
                    interval=Mathf.Max(interval,strokeDuration-upcoming.age-untilNext);
                untilNext+=interval;
            }
        }
        foreach (var s in states)
        {
            if (s == null || !s.renderer || s.age<0) continue;
            float age=Mathf.Clamp01(s.age/Mathf.Max(0.1f,strokeDuration));
            float p=age>=1 ? 1 : Mathf.Clamp01(progressSpeed == null ? age : progressSpeed.Evaluate(age));
            s.progress=Mathf.Max(s.progress,p);
            float impact=1-Mathf.SmoothStep(0,1,s.age/Mathf.Max(0.01f,impactDuration));
            Apply(s,s.progress,hdrIntensity,impact*impactIntensity);
            // Slide along this mark's own surface, including floor and cart planes.
            var mark = s.renderer.transform;
            if (s.followsSurface && movingSurface)
                mark.SetPositionAndRotation(movingSurface.TransformPoint(s.surfaceOrigin), movingSurface.rotation * s.surfaceRotation);
            else
                mark.SetLocalPositionAndRotation(s.origin, s.rotation);
            // A folded trace must keep its crease aligned to the shelf edge.
            if (!mark.TryGetComponent<FoldedClimbStroke>(out _))
                mark.position -= mark.up * (downwardSlide * Mathf.SmoothStep(0,1,s.age/0.42f));
            if (age>=1)
            {
                s.age=-1;
            }
        }
    }

    void Apply(State s,float progress,float intensity,float impact)
    {
        s.renderer.GetPropertyBlock(s.block);
        s.block.SetFloat(Progress,progress);
        s.block.SetFloat(Intensity,intensity);
        s.block.SetFloat(Impact,impact);
        Vector3 p=vrCamera ? vrCamera.transform.position : Vector3.zero;
        s.block.SetVector(Viewer,new Vector4(p.x,p.y,p.z,vrCamera ? 1 : 0));
        s.renderer.SetPropertyBlock(s.block);
    }
}
