using System.Collections.Generic;
using UnityEngine;

// Reusable timing model. The caller owns text, layout, and rendering.
public sealed class TerminalTypewriterSequence
{
    public enum Phase { Waiting, Typing, Pausing, Scrolling, Complete }

    private readonly IReadOnlyList<string> lines;
    private readonly float secondsPerCharacter;
    private readonly float scrollDuration;
    private readonly float linePause;
    private readonly float initialDelay;
    private float nextCharacterAt;
    private float phaseStartAt;
    private float startsAt;

    public int LineIndex { get; private set; }
    public int CharacterCount { get; private set; }
    public Phase CurrentPhase { get; private set; }
    public bool IsTyping => CurrentPhase == Phase.Typing;
    public float ScrollProgress { get; private set; }

    public TerminalTypewriterSequence(IReadOnlyList<string> lines, float secondsPerCharacter,
        float scrollDuration, float linePause, float initialDelay)
    {
        this.lines = lines;
        this.secondsPerCharacter = Mathf.Max(0.001f, secondsPerCharacter);
        this.scrollDuration = Mathf.Max(0.001f, scrollDuration);
        this.linePause = Mathf.Max(0f, linePause);
        this.initialDelay = Mathf.Max(0f, initialDelay);
    }

    public void Restart(float now)
    {
        LineIndex = 0;
        CharacterCount = 0;
        ScrollProgress = 0f;
        CurrentPhase = Phase.Waiting;
        startsAt = now + initialDelay;
    }

    public void Tick(float now)
    {
        if (lines == null || lines.Count == 0) return;
        switch (CurrentPhase)
        {
            case Phase.Waiting:
                if (now >= startsAt)
                {
                    CurrentPhase = Phase.Typing;
                    nextCharacterAt = now + secondsPerCharacter;
                }
                break;
            case Phase.Typing:
                if (now >= nextCharacterAt)
                {
                    CharacterCount = Mathf.Min(CharacterCount + 1, lines[LineIndex].Length);
                    nextCharacterAt = now + secondsPerCharacter;
                    if (CharacterCount == lines[LineIndex].Length)
                    {
                        CurrentPhase = Phase.Pausing;
                        phaseStartAt = now;
                    }
                }
                break;
            case Phase.Pausing:
                if (now - phaseStartAt >= linePause)
                {
                    CurrentPhase = LineIndex == lines.Count - 1 ? Phase.Complete : Phase.Scrolling;
                    phaseStartAt = now;
                }
                break;
            case Phase.Scrolling:
                float progress = Mathf.Clamp01((now - phaseStartAt) / scrollDuration);
                ScrollProgress = progress * progress * (3f - 2f * progress);
                if (progress >= 1f)
                {
                    LineIndex++;
                    CharacterCount = 0;
                    ScrollProgress = 0f;
                    CurrentPhase = Phase.Typing;
                    nextCharacterAt = now + secondsPerCharacter;
                }
                break;
        }
    }
}
