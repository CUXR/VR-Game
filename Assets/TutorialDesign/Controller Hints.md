# Reusable controller tutorials

This system only presents tutorials. It does not add, replace or remap locomotion.
The crawl hint follows the existing George rig's left-hand Y binding.

## Configure a hint

The scene uses `Assets/Prefabs/Controller Only Display.prefab`. Its two definitions are:

- `Resources/TutorialDesign/Grab Hint`: nearby, unobstructed Wire; left grip; PRESS To GRAB.
- `Resources/TutorialDesign/Crawl Hint`: nearby, unobstructed Incinerator Vent; left Y; PRESS Y To CRAWL.

Each Controller Hint definition contains the target tag or exact object name, range,
hand, controller prefab, highlighted bone, label, label offsets, reveal/shrink timing,
and optional object attention style. Duplicate a definition to configure another use,
then add it to the display component's **Additional Hints** list. No new presenter
or visibility code is required. Object attention currently targets static mesh renderers.
Name matching ignores spaces and case. Visibility uses the headset, physical source
geometry and collider occlusion; attention animation does not move detection bounds.

When both targets require the same hand, the display cycles every 4 seconds. The
outgoing presenter restores ownership before the incoming one hides the scene model.
The controller stays opaque at its settled size; only the new accent and text fade in.

## Configure object attention

`Resources/TutorialDesign/Attention Style` controls scale, scale breathing, floating
amplitude and period, finite approach distance, entry/exit duration and glow intensity.
The original object's collider, rigidbody, interaction points and materials are retained.
Grabbing or disabling restores the original renderer. Visual copies are owned and removed
when the hint owner is disabled/destroyed.

## Glow and button breathing

Controller button accents use the same unscaled breathing phase as the white coat.
Controller body accents remain independent. Hint changes use a separate short accent fade.

## Breathing haptics

Tutorial hand hints send vibration to the hand selected by their hint definition through
XRI's `HapticsUtility` on the existing OpenXR setup; no Meta SDK is required.
Vibration follows the white button coat's material `_BreathPeriod`, opacity range and
real-time cosine phase. As the white coat becomes more opaque, pulses become stronger
and more frequent; as it becomes more transparent, they become softer and less frequent.
This repeating gradient replaces the earlier two equally spaced pulses per breath.
The current material period is 2.4 seconds.

`TutorialHandControllerVisual` exposes minimum/maximum amplitude (defaults 0.04/0.55),
minimum/maximum pulse rate (2/14 pulses per second), and pulse duration (0.08 seconds).
Duration is capped at 70% of the current pulse interval to keep pulses distinct.
Strength also follows the hint's reveal opacity. No new pulses are sent while the hint
is hidden, disabled, retiring, or the application is unfocused. Missed pulses after a
frame stall are discarded rather than replayed together. Existing short pulses finish
naturally. Actual strength, rhythm and OpenXR device output still need headset validation.

## Object glow

Object glow preserves the original surface hue. Each object uses one camera-facing,
elliptical Gaussian source sized from its visual bounds, rather than its fine mesh outline.
This avoids subpixel wire sampling and face-normal shimmer during head movement. The source
plane lies just ahead of its own bounds to avoid self-occlusion, and fades against scene
depth. Three separable Gaussian levels create broad scatter from the half-resolution HDR
mask. Scene depth masks solid surfaces during compositing, preserving the material without
cutting a ring around the object's bounds. There is no per-face or timed flicker modulation.
Only registered hint objects feed this pass; it does not enable global scene bloom, HDR,
change the renderer asset, or turn the controller glass into a glow source. Pass textures
inherit the camera's XR texture layout. Fully occluded emitters do not contribute.

The multi-scale separable Gaussian approach is informed by
[Froyok's bloom analysis](https://www.froyok.fr/blog/2021-12-ue4-custom-bloom/).
The restricted source mask and low-resolution filtering are deliberate implementation
choices for this project's standalone VR target; device performance still needs profiling.
See also [Unity's mobile and VR post-processing guidance](https://docs.unity.com/en-us/engine/6000.5/manual/post-processing-and-full-screen-effects/urp/integration-with-post-processing).

Editor menu: **VR Game > UI > Prepare Reusable Controller Hints** prepares the
Y-highlight controller and font glyphs. The original grip prefab is preserved.
