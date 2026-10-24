# VR-Game Tutorial: Implementation and Evolving Intent Reference

Updated: October 22, 2026. Scope: VR-Game only.

## Purpose and evidence

This document is both an implementation reference and a reusable interpretation memory. Its semantic rules exist to reduce repeated guessing, clarification, token usage, and adjustment cycles. Apply previously confirmed meanings to similar future requests, then improve the rules when the user supplies a correction.

Evidence: current tutorial scripts, resource definitions, materials, shaders, editor importers, repository documentation and history; the VR-Game conversation `01a12415-19e1-7790-afc7-50b91a730888`; and the retrieved conversation titled “设计教程UI形式”. Unrelated portfolio and other project conversations are excluded. Historical requests establish intent; current files establish implementation. This is not a claim that every earlier conversation or every headset behavior was independently verified.

## 1. Current interaction and display capabilities

### Contextual tutorial ownership

`Assets/Prefabs/Controller Only Display.prefab` uses `ControllerOnlyDisplay`, reusable `ControllerHintDefinition` assets, `ControllerHintPresenter`, and `WireViewVisibility`.

| Hint | Target | Hand and highlighted control | Label | Current configuration |
|---|---|---|---|---|
| Grab | `Wire` tag | Left grip; `left_b_trigger_grip` | `PRESS To GRAB` | 2.5 m; object attention enabled |
| Crawl | Exact object name `Incinerator Vent`, ignoring spaces and case | Left Y; `b_button_y` | `PRESS Y To CRAWL` | 2.5 m; object attention disabled |

The tutorial presents the existing controls. It does not implement or remap crawling. The earlier guess of right A was corrected to left Y. Never revive that guess without inspecting the actual rig and receiving a new requirement.

Visibility requires nearby physical source geometry within the headset view and collider-based occlusion checks. An object behind a wall must not qualify merely because it falls inside the camera frustum. Animated visual proxies must not change detection bounds. Missing camera or expired visibility hides the presenter; the configured exit delay is 0.2 seconds.

Multiple eligible hints using the same hand alternate every 4 seconds. The outgoing owner restores the scene visual before the incoming owner captures it. A continuing hand keeps the controller at settled scale and opacity; the new accent and label fade over 0.25 seconds. Eligibility for target attention is evaluated independently of which hint wins the hand slot.

### Controller replacement, animation and restoration

The tutorial visual attaches to the scene controller and inherits its tracking. The original renderers are hidden while the replacement is active, and their previous states are restored on retirement, disable or destruction. Keep the existing tracking, interaction components and input bindings.

The replacement reveals and shrinks simultaneously from 1.5 times authored size to normal size. The presenter uses `revealDuration / 1.2` and `shrinkDuration / 1.2`: with the current 0.65 and 1.25 second definition values, approximately 0.542 and 1.042 seconds. Shrinking reuses the panel motion curve and rebound, applied to scale only. Tracking rotation is not animated by that curve. Current retirement immediately disables the replacement and restores the original; do not describe it as a verified exit crossfade.

The scene visual owns tracking continuity. Repeated independent device polls previously caused periodic disappearance and replayed reveals. A brief tracking flag fluctuation must not restart reveal or shrink.

`TutorialControllerInputAnimation` supports X, Y, menu, trigger, grip, and four stick directions through bone motion definitions. `TutorialButtonPressAnimation` retains press demonstration behavior; `TutorialControllerModelDisplay` retains automatic illustration rotation, grip-driven handoff and presentation fading for reusable display code. These capabilities must be distinguished from the current tracked contextual presentation.

### Object attention

`WireAttentionVisual` creates owned visual copies for static mesh renderers while preserving source interaction geometry, rigidbody, attachment points and material appearance. Grabbing or disabling restores source rendering and cleans up owned visuals.

`TutorialAttentionStyle` code defaults: scale 1.2; scale breath 0.04; float amplitude 0.015 m; period 4.8 seconds; finite approach distance 0.06 m; transition duration 0.85 seconds; glow intensity 0.3. Inspect the assigned asset before changing a live instance: serialized values can override defaults.

The visual floats smoothly with eased direction changes, breathes in scale at the same frequency, and approaches the headset by a finite offset. It must not continuously travel toward the player. Preserve the physical pickup position.

### Retained panel and legacy text capabilities

The user explicitly removed the visible rectangle, its elements and the black dimming overlay from the controller-only experience, while retaining reusable blur, curvature and animation code. A retained class or style asset does not establish that a panel is currently displayed. `Glass Blur.md` mentions a `CurvedTutorialPanelController`; that name is a historical integration reference and was not found as a current source file in this review.

Retained building blocks include `CurvedPanelMeshBuilder`, `TutorialPanelStyle`, `TutorialPanelDockMotion`, `TerminalTypewriterSequence`, controller handoff helpers and panel shaders. Terminal phases are Waiting, Typing, Pausing, Scrolling and Complete.

`TutorialController` is a separate legacy singleton that assigns `InteractableInterface.GetTutorialText()` to TMP UI text and can clear it. `Assets/Resources/tutorial.json` retains desktop wording for wire pickup, generic pickup, throw/release, door interaction, locked door, missing generator wires and repaired generator. Its numeric priorities are data, not proof of a current priority arbitration system. Desktop E/click wording must not be copied into the VR grip/Y hints.

## 2. Materials, rendering and layout

### Frosted glass method

Panel and controller glass share a leased, camera-specific Gaussian backdrop through `TutorialGlassBlur`. The blur runs while a consumer holds a lease and releases when the final consumer leaves. It uses URP RenderGraph and preserves XR texture array dimensions and eye slices.

The backdrop is the resolved opaque scene snapshot including the skybox. Transparent particles and other transparent UI are not in that snapshot. The pyramid uses quarter, eighth, sixteenth and thirty-second resolution levels, with filtered downsampling and horizontal/vertical Gaussian passes. The contiguous normalized nine-tap filter uses five bilinear reads per axis. The controller reads medium blur; the panel blends medium and broad according to projected size.

Replace the background with the blurred scene before tinting. Apply `_UIFade` to the resulting silhouette. Applying glass density to the completed blur a second time reintroduces crisp scene edges. `_BlurRadius` is panel design-space softness; `_BlurPixels` is controller screen-space softness. Large radii saturate at the broadest level rather than spacing sparse samples farther apart. Preserve sharp text and XR stereo correctness. Quest performance remains an on-device profiling question.

### Controller body and button settings

Use `Assets/TutorialDesign/Shaders/ControllerDisplay.shader` and the body/white-grip materials. Runtime material property blocks can override serialized material settings, including per-material-slot blocks; inspect those before assuming an Inspector change has failed.

| Property | Reference value | Meaning |
|---|---|---|
| `_BaseColor` body | (0.30, 0.425, 0.50, 0.50), shader default | Blue-grey tint and glass density |
| `_TintStrength` body | 0.82, shader default | Tint contribution |
| `_SurfaceBrightness` body | 1.35, shader default | Surface brightness |
| `_BlurPixels` | 36 | Screen-space frost softness |
| `_BoundaryLineStrength` | 0.045 | Fine boundary opacity |
| `_BoundaryGlowStrength` | 0.56 | Soft boundary light |
| `_BoundaryWidth` | 3 mask texels | Boundary core width |
| `_BoundarySoftness` | 4.5 | Shell spread multiplier: 13.5 texels |
| `_ButtonSoftness` | 2.25 | Button spread multiplier: 6.75 texels |
| `_RimStrength` / `_RimSoftStrength` | 0.28 / 0.30 | Narrow silhouette and inward wash |
| `_RimPower` | 3.5 | Edge falloff |
| White `_BreathOpacityMin` / Max | 0.32 / 0.98, material values | Visible white coat breathing range |
| White `_BreathPeriod` | 2.4 seconds | White coat and button accent phase |

The highlighted button has a body-matched frosted underlay plus a breathing white coat. The underlay remains visible when the coat becomes transparent. White breathing must affect the button surface visibly, rather than leaving only a tiny edge spot. Button glow follows the white coat's unscaled breathing phase. General controller body accents remain independent. Preserve normal-dependent shading so the highlighted curved control retains shape.

The shared 1024 px boundary distance mask identifies actual button borders and shell creases; it must not draw UV seams or coplanar triangle edges as white lines. Button priority is 85%, crease priority 55%. Derivative-based coverage stabilizes small tracked models. The source battery-indicator artwork card remains inactive because it has no matching glass mask. After changing the source FBX, run **VR Game > UI > Rebuild Controller Glass Boundaries**.

### Breathing haptics

`TutorialHandControllerVisual` sends haptic impulses through XRI `HapticsUtility`
using the existing OpenXR setup; Meta SDK is not required. `ControllerHintPresenter`
passes the hint definition's hand so vibration follows the corresponding controller.

The latest requested behavior is a repeating gradient synchronized with the white
button coat's transparency, replacing the earlier two evenly spaced pulses per breath.
Use the shader's real-time cosine phase and material `_BreathPeriod` (currently 2.4 s).
Normalize white opacity across `_BreathOpacityMin` and `_BreathOpacityMax`: more opaque
means stronger, faster pulses; more transparent means softer, less frequent pulses.
The same cycle repeats without a separate haptic breathing clock.

Component defaults: amplitude 0.04–0.55, pulse rate 2–14 pulses/second, pulse duration
0.08 s capped at 70% of the current interval. Amplitude also follows reveal opacity.
No new pulses are sent when the hint is hidden, disabled, retiring, or the app loses
focus. Existing short pulses finish naturally; missed pulses after frame stalls are
discarded rather than replayed in a burst. Serialized component settings may override
defaults. Implementation is code-observed; perceived intensity, continuity and device
output still require headset testing and user acceptance.

### Wire glow method

Preserve the original surface hue and shading. Use a registered camera-facing elliptical Gaussian emitter based on visual bounds, placed slightly forward to avoid self-occlusion. A half-resolution HDR source mask and three separable Gaussian levels produce broad scatter. Depth controls occlusion and compositing. Fully occluded sources contribute no halo.

Do not trace every wire face, pulse brightness per face, wash the object grey-white, or carve an artificial ring around its bounds. Fine geometry must not cause shimmer during head motion. Only registered hint objects feed this pass; it does not require enabling global scene bloom or turning controller glass into an emitter. Relevant files: `TutorialGlowBloom.cs`, `WireAttentionGlow.shader`, `TutorialGaussianBlur.shader`, and `TutorialGlowComposite.shader`.

### Label placement and typography

`ControllerGrabPrompt` uses Inter ExtraLight for the prefix and Inter Medium for the action. It keeps both on one baseline using the shaped trailing-space advance, disables wrapping and raycast targeting, and measures actual ink bounds. Current nominal font size is 49.66; the combined ink width is scaled to approximately 70% of controller physical length. Controller length comes from actual geometry's principal axis, with baked skinned bounds normalized for FBX units.

The label belongs on headset-view right of the highlighted button and faces the headset. Controller rotation must not rotate the text away or move it around the controller in local coordinates. Head orientation provides the placement basis; head position only determines facing. Distance from the headset must not alter controller-to-label spacing.

| Setting | Grab | Crawl |
|---|---|---|
| Normal text offset | (-0.23, -0.28) | (-0.23, 0) |
| Right text offset | 0.45 | 0.60 |
| Forward text offset | 0.04 | 0.04 |

Offsets are fractions of model length, including its current reveal scale. Horizontal means the physical long axis lies predominantly along headset-view right. Enter the horizontal state at 60 degrees from view-up and exit at 50 degrees; blend over approximately 0.1 seconds. In horizontal mode remove the additional normal left/down offset while retaining right and forward placement. Update in LateUpdate and before render without doubling the blend speed.

### Retained panel reference values

These are reusable style values, not instructions to restore the removed panel:

| Group | Values |
|---|---|
| Geometry | Headset distance 1.5 m; vertical offset -0.18 m; canvas scale 0.00065; width 2100; height 900; corner radius 140; grid 64 by 16; foreground depth 0.2 |
| Text | Size 43.666; reference ink width 381; forward depth 0.5; tilt 10 degrees |
| Appearance | Fade 0.35 s; backdrop opacity 0.5 |
| Illustration | Height ratio 1; forward offset 0.5; horizontal offset 0; rotation amplitude 15 degrees; period 4 s |
| Handoff | Snap 0.85 s; fade distance 0.35 m; fade end 0.1 m; hand fade 0.65 s; display fade 0.18 s |
| Dock | Duration 1.25 s; position (0, -0.5, 0.45); pitch 30 degrees; scale 0.4; opacity multiplier 0.8; foreground depth 0.02 |
| Terminal | 0.055 s/character; scroll 0.36 s; pause 0.24 s; initial delay 0.2 s; bottom row Y 1135; row spacing 61; indicator 1.2 Hz; cursor 1.6 Hz; duty 0.55 |

The panel shader retains tint (0.243137, 0.337255, 0.392157), `_BlurRadius` 338.438 and `_TintStrength` 0.82. Keep design pixels, world metres and screen pixels distinct.

## 3. Reusable semantic rules

The phrases below are English translations or normalized paraphrases of historical requests, not verbatim English quotations. Treat confirmed interpretations as defaults within the same tutorial context, not universal meanings for every future object.

| ID | Ambiguous expression | Reusable interpretation and concrete adjustment | Avoid repeating |
|---|---|---|---|
| S01 | Keep only the controller; delete everything cleanly | Remove visible panel, contents, dimmer and attached obsolete runtime components; retain reusable mathematical, blur and motion helpers | Hiding the rectangle while its animations still execute; deleting requested reusable code |
| S02 | Replace the controller model | Replace appearance under the existing tracked anchor; retain tracking and interactions; restore original renderer states on exit | Adding a separate tracking owner or changing controls |
| S03 | Shrink like the rectangle | Reuse easing, speed character and rebound for scale only | Copying the rectangle's rotation or docking angle |
| S04 | Appear and shrink together; 1.2 times faster | Start opacity reveal and 1.5-to-1 scale motion together; divide both durations by 1.2 | Waiting for the fade to finish before shrinking |
| S05 | Always on the right at the button's height | Use headset-view right and button anchor height; billboard toward headset | Controller-local right; screen-edge anchoring |
| S06 | The proportion belongs to the controller | Derive size and spacing from physical model length and current model scale | Distance-based spacing tied to head position |
| S07 | Forward, down and left, except when horizontal | Apply normal left/down offsets for upright pose; suppress them in horizontal pose; retain equal forward offset | Reversing horizontal and vertical classification |
| S08 | Too far forward, still not low enough | Tune depth and vertical offsets separately using the reference composition | Treating a depth correction as a size change |
| S09 | Frosted white button with breathing | Body-like frost underlay plus visible white opacity oscillation across the control surface | A disappearing hole, a constant white surface, or only an edge glint |
| S10 | Make button light breathe with the white | Share the white coat's time and phase with its accent light | Unrelated glow oscillator or whole-controller opacity breathing |
| S11 | Really in view | Require frustum, physical geometry and occlusion, plus configured range | Frustum-only triggering through walls |
| S12 | Close enough to reach | Initially implied a tight range; later explicitly superseded by 2.5 m | Reverting the confirmed range to a guessed arm length |
| S13 | Move toward the headset, not continuously | Ease to a finite approach offset and return on exit | Per-frame movement accumulating indefinitely |
| S14 | Slow breathing float; nonlinear direction changes | Smooth periodic vertical motion; synchronized scale breathing; eased approach and transitions | Linear ping-pong velocity corners or unrelated periods |
| S15 | Elegant soft light spreading out | Broad external Gaussian scatter with preserved surface hue, stable during head movement | Grey frosted replacement, sharp contour outline, per-face flicker |
| S16 | No need to follow the exact contour | Use a coarse bounds-based Gaussian source rather than dense mesh detail | Spending more geometry to reproduce every wire silhouette |
| S17 | The whole controller disappears every few seconds | Investigate tracking ownership and reveal resets; retain presentation continuity | Mistaking the bug for requested breathing |
| S18 | Alternate if both are together | Cycle button accent and label per hand; retain settled controller body | Restarting the full reveal and 1.5-times scale every cycle |
| S19 | Crawl tutorial only | Present the existing left Y binding; do not migrate locomotion | Implementing a new crawling system from an earlier A-button guess |
| S20 | Move crawl text a little farther right | Increase crawl-specific right offset to 0.60; preserve grab's 0.45 | Changing the shared typography or both hints |
| S21 | Several sides, not from above | Show actual asset previews at eye level: front, oblique and side | Top-down screenshots or unrelated primitive substitutes |
| S22 | First Mike, then production | Develop and commit in Mike first; promote only within the current authorized scope | Writing directly to production; ignoring a later request to keep work in Mike |
| S23 | Vibration follows white button transparency, from strong and fast to soft and weak, looping | Use the white coat's material period and cosine phase; map normalized opacity to both amplitude and pulse rate on the hint's selected hand | Keeping the superseded two-pulses-per-breath pattern; using an unrelated timer; confusing pulse repetition with motor frequency in Hz |

### Interpretation and improvement loop

1. Identify the target object and property: placement, opacity, blur, glow, motion, trigger, or lifecycle.
2. Match the request to an existing semantic ID. Read its confirmed interpretation and relevant current asset values before exploring alternatives.
3. Check for explicit new overrides. The latest clear instruction wins; preserve older rules as superseded history where useful.
4. State one short interpretation only when multiple materially different meanings remain. Ask one focused question about that difference; do not re-ask settled facts.
5. Change the smallest relevant parameter or ownership rule. Keep unrelated dimensions stable so the result is easy to judge.
6. Validate using the failure case that originally exposed the ambiguity: upright/horizontal controller, head near/far, obstructed target, hint overlap, or actual wire under a horizontal camera orbit.
7. After a correction, update this document's matching row with the new meaning, parameter ownership, evidence and acceptance condition. Add a new row only for a distinct concept.
8. Record whether the result is user-confirmed, code-observed, visually checked, or still proposed. Code agreement alone does not establish aesthetic acceptance.

Use this record format for future refinements:

```text
Rule ID:
Date and VR-Game conversation:
Normalized phrase:
Target and coordinate frame:
Previously assumed meaning:
Corrected meaning:
Parameters/files changed:
Acceptance case and observed result:
Status: user-confirmed | code-observed | visually-checked | proposed
Supersedes:
```

Do not invent historical quotations or mark a change accepted solely because an assistant reported completion. Do not apply VR-Game preferences to unrelated projects. This document is a manually maintained memory; no background automation is implied.

## 4. Practical acceptance cases

- A visible wire within 2.5 m shows the left grip tutorial; a wall blocks it; the vent shows left Y with crawl-specific spacing.
- Upright, oblique and horizontal controller poses preserve readable headset-facing text. Moving the head closer does not independently enlarge the controller-to-text gap.
- Reveal and shrink begin together; the rebound changes scale without rotating the tracked hand. A continuing hint swap preserves settled size.
- White opacity and its button accent breathe together while the frosted underlay remains. Inspect material-slot overrides if breathing is invisible.
- On the hinted hand, white opacity, vibration strength and pulse density rise and fall together over repeated breaths. Verify the soft trough and rapid strong peak on a headset; hiding the hint or losing app focus stops new pulses without a catch-up burst.
- Wire surface color survives the halo. Small head movements do not produce face shimmer. A fully hidden object contributes no halo through a wall.
- Pickup, disable, destruction and scene transitions restore source renderers and release owned effects and blur leases.
- The removed panel and dimmer remain absent from the controller-only experience.

These are acceptance criteria for subsequent changes, not a claim of fresh headset testing during this documentation task.

## 5. Source map and narrative boundary

Core source directories: `Assets/Scripts/Tutorial`, `Assets/Resources/TutorialDesign`, `Assets/TutorialDesign/Materials`, `Assets/TutorialDesign/Shaders`, and tutorial importers under `Assets/Editor`. Supporting references: `Assets/TutorialDesign/Controller Hints.md`, `Assets/TutorialDesign/Glass Blur.md`, `Assets/Resources/tutorial.json`, and the controller display prefabs.

For new hints, duplicate a `ControllerHintDefinition`, assign its existing control, prefab, anchor, matching target, label and offsets, then add it to Additional Hints. Use **VR Game > UI > Prepare Reusable Controller Hints** to prepare generated hint assets and fonts. Avoid adding a new presenter for a configuration-only variation.

The earlier narrative discussion established a robot escaping an abandoned laboratory and explored diagnostic HUDs, wrist projections, recovery/calibration terminology and environmental guidance. These were design concepts. The discussion does not prove that a complete boot sequence, wrist projector, laboratory guidance system or module-recovery progression exists in the current tutorial. Future narrative work can reuse those ideas without treating them as implemented requirements.
