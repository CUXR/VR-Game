# Tutorial frosted glass

The tutorial rectangle and glass controller share a Gaussian background texture.
This approximates the soft frosted appearance of visionOS glass; it does not use
Apple's system material or claim to reproduce its private rendering algorithm.

## Integration

`CurvedTutorialPanelController` acquires `TutorialGlassBlur` while the panel fades
in/out or the glass controller is visible on the hand. The last lease releases
the blur. Multiple panels using one camera share one pass.

The pass is queued through URP's `beginCameraRendering` callback and public
`EnqueuePass` API. Its color input requests a resolved opaque scene texture.
Existing cameras, XR anchors, renderer assets, MSAA and interaction profiles are
not changed. Unity 6000.6 / URP 17.6 RenderGraph is required.

## Filtering and cost

- Four levels at 1/4, 1/8, 1/16 and 1/32 of the camera resolution.
- Each level has a filtered downsample, horizontal Gaussian and vertical Gaussian.
- A normalized contiguous 9-tap kernel uses five bilinear texture reads per axis.
- The controller reads the medium blur; the panel blends medium and broad blur
  according to its projected size. Smaller docked panels keep proportional blur.
- XR textures retain their array dimension and eye slices. URP's RenderGraph
  blitter draws the correct eye; `CommandBuffer.Blit` is not used.
- RenderGraph owns transient textures. No per-frame native materials or property
  blocks are created. Shared blur runs only for cameras with an active lease.

This avoids sparse, widely separated ring samples and their repeated silhouettes.
It is a reduced-resolution Gaussian pyramid, not a full-resolution large-kernel
Gaussian. Actual Quest frame cost needs an on-device measurement.

## Appearance

The panel's Figma tint, inset highlights, rounded corners and fades stay in its
material. The controller body uses a lighter blue-grey tint, softened shading,
and 50% glass density, revealing more of the blurred scene behind it. The
silhouette uses a quieter edge (28%) and a brighter surface wash (30%) inward.
The edge uses a 3.5-power falloff and the wash a 3.5/3-power falloff, tapering out
at the edge peak. The lower powers spread the gradient across the shell.
Real button boundaries use a shared 1024px distance mask. Fine-line opacity
(4.5%) and soft-glow strength (56%) are independently adjustable. Button glow
has a tighter 6.75-texel spread (2.25 times the core width), while shell seams
retain the broad 13.5-texel spread (4.5 times the core width). The mask priority
distinguishes buttons (85%) from shell creases (55%) and weights their highlights.
Spread is capped within the mask's distance range. UV derivatives maintain line coverage
on the small tracked model. Accents stay below the white grip's brightness.
The white grip mesh emits directly, with these secondary highlights disabled;
normal-dependent brightness preserves its curved shape instead of clipping all
faces to solid white. The legacy glow material and shader have been removed.
The source FBX's flat battery-indicator artwork card is inactive in both tutorial
prefabs; it has no matching glass UV mask. Text stays sharp. Existing docking and
controller animations are unchanged.

The distance mask is generated in the Editor by `TutorialControllerGlassMaskImporter`.
Use **VR Game > UI > Rebuild Controller Glass Boundaries** after changing the
source FBX. UV seams and coplanar triangle edges do not generate white lines.
The displayed and skinned controller share the source UVs, so boundary highlights
remain attached while the buttons move. No new runtime render pass is needed.

Glass replaces the background with its blurred version before applying tint
opacity. Its silhouette then fades with `_UIFade`. Applying material opacity to
the final blur layer a second time would mix crisp scene edges back into it.

The backdrop is the URP opaque scene snapshot, including the skybox. Transparent
particles and other transparent UI are not captured in this snapshot. Glass
continues to draw above scene geometry, matching the tutorial overlay behavior.

`_BlurRadius` controls the panel's design-space softness. `_BlurPixels` controls
the controller's screen-space softness. Their shared filter saturates at the
broadest pyramid level instead of increasing the distance between sparse taps.
