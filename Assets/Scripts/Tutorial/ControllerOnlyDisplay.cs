using UnityEngine;
using System.Collections.Generic;

/// <summary>Data-driven controller hints, sharing visibility, presentation and target attention.</summary>
[DisallowMultipleComponent]
public sealed class ControllerOnlyDisplay : MonoBehaviour
{
    [SerializeField] private ControllerHintDefinition grabHint;
    [SerializeField] private ControllerHintDefinition crawlHint;
    [SerializeField] private ControllerHintDefinition[] additionalHints = new ControllerHintDefinition[0];
    [SerializeField, Min(2f)] private float hintCycleSeconds = 4f;
    private readonly Dictionary<UnityEngine.XR.XRNode, float> cycleStarts = new Dictionary<UnityEngine.XR.XRNode, float>();
    private sealed class Hint
    {
        public ControllerHintDefinition definition;
        public ControllerHintPresenter presenter;
        public WireViewVisibility visibility;
        public readonly List<Transform> targets = new List<Transform>();
        public bool visible;
        public bool selected;
    }
    private readonly List<Hint> hints = new List<Hint>();
    private readonly List<WireAttentionVisual> effects = new List<WireAttentionVisual>();
    private readonly List<Transform> attentionTargets = new List<Transform>();
    private readonly HashSet<UnityEngine.XR.XRNode> visibleHands = new HashSet<UnityEngine.XR.XRNode>();
    private readonly HashSet<UnityEngine.XR.XRNode> continuingHands = new HashSet<UnityEngine.XR.XRNode>();
    private readonly List<Hint> eligibleHints = new List<Hint>();
    private readonly List<UnityEngine.XR.XRNode> staleCycles = new List<UnityEngine.XR.XRNode>();

    private void OnEnable()
    {
        Add(crawlHint != null ? crawlHint : Resources.Load<ControllerHintDefinition>("TutorialDesign/Crawl Hint"));
        Add(grabHint != null ? grabHint : Resources.Load<ControllerHintDefinition>("TutorialDesign/Grab Hint"));
        foreach (var definition in additionalHints) Add(definition);
    }
    private void Add(ControllerHintDefinition definition)
    {
        if (definition == null || hints.Exists(hint => hint.definition == definition)) return;
        hints.Add(new Hint { definition = definition, presenter = new ControllerHintPresenter(definition),
            visibility = new WireViewVisibility(definition.targetTag, definition.targetName) });
    }
    private void Update()
    {
        var camera = Camera.main;
        attentionTargets.Clear();
        var hands = visibleHands;
        hands.Clear();
        foreach (var hint in hints)
        {
            hint.targets.Clear();
            hint.visible = camera != null && camera.isActiveAndEnabled
                && hint.visibility.CollectVisible(camera, hint.definition.distance, hint.targets);
            hint.selected = false;
            if (hint.visible) hands.Add(hint.definition.hand);
            if (!hint.definition.animateTarget) continue;
            foreach (var target in hint.targets)
            {
                attentionTargets.Add(target);
                var effect = target.GetComponent<WireAttentionVisual>();
                if (effect == null) effect = target.gameObject.AddComponent<WireAttentionVisual>();
                effect.Configure(hint.definition.attentionStyle);
                if (!effects.Contains(effect)) effects.Add(effect);
            }
        }
        foreach (var hand in hands)
        {
            var eligible = eligibleHints;
            eligible.Clear();
            foreach (var hint in hints)
                if (hint.visible && hint.definition.hand == hand) eligible.Add(hint);
            int index = 0;
            if (eligible.Count > 1)
            {
                if (!cycleStarts.TryGetValue(hand, out float start)) cycleStarts[hand] = start = Time.unscaledTime;
                index = Mathf.FloorToInt((Time.unscaledTime - start) / hintCycleSeconds) % eligible.Count;
            }
            else cycleStarts.Remove(hand);
            eligible[index].selected = true;
        }
        staleCycles.Clear();
        foreach (var hand in cycleStarts.Keys)
            if (!hands.Contains(hand)) staleCycles.Add(hand);
        foreach (var hand in staleCycles) cycleStarts.Remove(hand);
        var continuing = continuingHands;
        continuing.Clear();
        foreach (var hint in hints)
            if (!hint.selected && hint.presenter.Model != null && hands.Contains(hint.definition.hand))
                continuing.Add(hint.definition.hand);
        // Dispose the outgoing owner before capturing original renderer state.
        foreach (var hint in hints)
            if (!hint.selected)
            {
                if (hands.Contains(hint.definition.hand)) hint.presenter.Hide();
                else hint.presenter.Update(false, camera);
            }
        foreach (var hint in hints)
            if (hint.selected) hint.presenter.Update(true, camera, continuing.Contains(hint.definition.hand));
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (effect == null) { effects.RemoveAt(i); continue; }
            effect.SetVisible(attentionTargets.Contains(effect.transform), camera);
        }
    }
    private void OnDisable()
    {
        foreach (var hint in hints) hint.presenter.Dispose();
        hints.Clear();
        cycleStarts.Clear();
        foreach (var effect in effects)
            if (effect != null)
            {
                effect.enabled = false;
                if (Application.isPlaying) Destroy(effect);
                else DestroyImmediate(effect);
            }
        effects.Clear();
    }
}
