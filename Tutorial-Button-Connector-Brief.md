# VR Tutorial Button Connector — Implementation Brief

## Access and handoff to another AI

### Current location and publication status

- Local Windows project: `C:\NEW\Unity\VR-Game`
- This document: `C:\NEW\Unity\VR-Game\Tutorial-Button-Connector-Brief.md`
- Repository-relative path: `Tutorial-Button-Connector-Brief.md`
- Configured GitHub remote: `https://github.com/CUXR/VR-Game.git`
- At the time this handoff section was written, this document was an untracked local file: it had **not been committed or pushed to GitHub**. Check its current status before assuming it is available remotely.

### Option A — AI running on this computer (recommended for implementation)

Open `C:\NEW\Unity\VR-Game` as the other AI tool's workspace and allow that tool to read the project through its own permission controls. Ask it to read this Markdown file first, then inspect the relevant scripts, controller models/materials, prefabs, and scene. Grant write access only if you want it to implement changes.

Suggested instruction to the other AI:

> Read C:\NEW\Unity\VR-Game\Tutorial-Button-Connector-Brief.md and inspect the current local Unity project. Diagnose the button endpoint problem and verify the result visually. Preserve existing uncommitted changes. Do not commit or push unless I ask you to. Coordinate before editing files another AI is actively changing.

The local workspace includes uncommitted work that may not exist on GitHub. For diagnosing the result currently visible in Unity, use this working copy or a deliberately prepared snapshot of it.

### Option B — AI in a separate web chat (recommended for reviewing the brief)

Upload this `.md` file as an attachment, or paste its contents into that chat. A local file link from the original conversation does **not** give a separate AI access to this computer. Upload relevant screenshots too: the images from the original conversation are not embedded in this document.

The brief alone is sufficient for discussing requirements, but not for verifying or fixing the implementation. For that, provide the relevant project files and assets, or connect the other AI to the repository. Do not upload credentials or unrelated private files.

### Option C — AI with GitHub access

The document must first be committed and pushed to a chosen branch before another AI can retrieve it from GitHub. Then provide the actual branch or commit and the file's GitHub link. Do not assume it is on `Mike` or `production`, and do not push unrelated local changes just to share this document.

If the repository is private, the other AI's GitHub integration or account must have repository access authorized by the owner. A GitHub URL alone does not grant access. Repository visibility and the other AI's authorization have not been verified here. GitHub only exposes committed and pushed content; local uncommitted Unity changes will be missing.

### Permissions and practical limits

This document cannot grant permissions. The assistant writing it has not granted another AI filesystem access, changed repository collaborators, or published the project. Another tool needs its own user-authorized local workspace access, file upload, or GitHub connection. Do not share account passwords or access tokens in a chat to enable access.

For an independent second opinion, upload this document and screenshots. For implementation against the exact current project, use a locally authorized AI workspace. Avoid simultaneous edits to the same files by multiple AI tools.

## Goal

Make it immediately clear which physical controller button the player should press. The tutorial text must connect to the **actual highlighted button's outer surface**, not to the controller body, controller origin, an internal bone pivot, or an arbitrary point near the controller.

This applies consistently to every tutorial that points to a button, including the grab/grip prompt and the crawl/Y-button prompt.

## Text placement

- Keep tutorial text in front of the headset using lazy follow: it should smoothly follow the player's view rather than remain attached to the hand/controller.
- The previous requested distance was 1.2 m. The latest request is to move the text **slightly closer to the headset and slightly higher**. Exact new values have not been specified; choose a modest adjustment and verify it visually.
- Keep the existing uppercase display and existing bold/light emphasis unchanged.

## Connector shape

The reference uses a three-part callout:

1. A diagonal segment leading from the relevant button toward the text.
2. A short horizontal segment beside the text.
3. A short vertical end cap next to the text.

The horizontal segment and vertical end cap have fixed lengths. The diagonal segment changes its length and angle as the controller moves relative to the headset-following text.

Reference: [Figma tutorial design](https://www.figma.com/design/jLnMNYFM8i4YwlF5k8C22A/Game-Tutorial?node-id=98-58&m=dev).

## Critical requirement: the button-side endpoint

- Find the **specific glowing button used by the active prompt**. A generic controller anchor is insufficient.
- Anchor the endpoint to that button's visible exterior surface, and keep it attached to the button as the controller moves or rotates.
- Do not terminate inside the controller mesh or on a different part of its shell.
- Do not push the endpoint out to the silhouette of the entire controller: that loses the relationship to the individual button.
- An earlier reference allowed a small visual gap beside the button. The latest clarification prioritizes the actual button surface. If a tiny offset is necessary to prevent rendering artifacts, it must still read unmistakably as pointing at that button, rather than floating elsewhere.
- Apply the same visual convention to grip and Y; do not solve one prompt with an extra label while leaving the other inconsistent.

## Why another pass is needed

Previous attempts appeared to connect to the controller as a whole, or to points inside it. The user has repeatedly reported that the result does not identify the intended button. A plausible transform reference in code is not sufficient evidence that the rendered endpoint is correct.

## Validation requirements

Inspect the rendered result using Unity screenshots or a representative visual simulation, in addition to reviewing the code.

Check both grab/grip and crawl/Y prompts with:

- The controller in a normal interaction pose.
- The controller rotated so its front and side are visible from different angles.
- The controller moved relative to the headset-following label.
- The headset turned while the controller remains stationary.

For each visible target button, confirm that the endpoint is on its outer surface, that the line clearly identifies that button, and that the short horizontal segment and vertical cap retain their lengths. Check for clipping, endpoint drift, and accidental attachment to the controller body.

If the target button is hidden by the controller, explicitly evaluate the resulting presentation rather than silently moving the endpoint to an unrelated visible part of the shell.

Provide before/after images or a clearly described visual simulation. State which checks were actually performed and distinguish them from checks that still require a headset. Do not claim headset validation based only on source-code inspection.

## Scope

Preserve the existing tutorial flow, timing, haptics, typography, and button highlighting except where necessary for these placement and connector fixes. This brief does not request adding new button-name labels or redesigning the interaction.

Implementation starting point: `Assets/Scripts/Tutorial/ControllerGrabPrompt.cs`. Verify the actual highlighted geometry and its transforms before selecting an endpoint; do not assume a controller or bone origin lies on the button surface.
