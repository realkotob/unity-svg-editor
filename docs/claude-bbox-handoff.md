# Claude Handoff: Bounding Box / Canvas Drift Issue

## Current Symptom

- Bounding box overlay is still not aligned with the actual SVG preview.
- After editing or dragging, the overlay appears to jump back toward the original file position.
- The user perception is: "the preview or bbox is still following the original asset, not the edited copy."

## What Was Already Changed

These changes are already in the workspace and should be treated as the current baseline.

- Snapshot contract was renamed from mixed `SceneViewport` / `SceneBounds` semantics into:
  - `DocumentViewportRect`
  - `VisualContentBounds`
  - `VisualBounds`
  - `HitGeometry`
  - `BoundsQuality`
- Transient preview fallback to the original `VectorImageAsset` was reduced.
- Hover outline was added.
- Stale committed selection viewport caching was removed.
- Triangle fallback bounds were changed to use a transformed fallback path.

Relevant files:

- [PreviewSnapshot.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewSnapshot.cs)
- [PreviewElementGeometry.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewElementGeometry.cs)
- [PreviewSnapshotGeometryBuilder.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewSnapshotGeometryBuilder.cs)
- [CanvasProjectionMath.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasProjectionMath.cs)
- [DocumentPreviewService.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Document/DocumentPreviewService.cs)

## Confirmed Structural Problems

### 1. Source-of-truth split is still incomplete

`DocumentSession` now has `InteractionPreviewSourceText` and `ActiveCanvasSourceText`, but `ActiveCanvasSourceText` is not actually used anywhere.

Evidence:

- [DocumentSession.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Document/Source/DocumentSession.cs#L17)
- Search result: `ActiveCanvasSourceText` is only declared, never consumed.

Why this matters:

- The codebase conceptually has `original`, `working`, and `interaction preview` sources.
- The runtime still does not have one authoritative "canvas source" API.
- Different subsystems still infer source state indirectly from `PreviewSnapshot`, `WorkingSourceText`, or a transient parameter.

Impact:

- Canvas image and bbox can still be driven by different assumptions about which source is current.

### 2. Move delta conversion depends on the current preview snapshot, not the drag-start snapshot

`CanvasElementDragController` converts viewport delta to scene delta using `host.PreviewSnapshot` during drag.

Evidence:

- [CanvasElementDragController.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasElementDragController.cs#L99)
- [CanvasElementDragController.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasElementDragController.cs#L167)

Why this matters:

- During drag, `host.PreviewSnapshot` is replaced repeatedly by transient preview snapshots.
- If the active viewport rect falls back to content bounds, then moving the content changes the snapshot bounds.
- That means the scene/viewport mapping basis can change while the pointer is still dragging.

This is the strongest remaining explanation for:

- bbox drift
- "jumping back"
- move behavior that feels tied to the original asset position

### 3. Projection math is still synthetic, not derived from the actual rendered image rect

`CanvasProjectionMath` computes its own content viewport rect from `ViewportFrameState.GetFrameContentViewportRect(...)`.
The actual preview image is a UI Toolkit `Image` with `ScaleMode.ScaleToFit`.

Evidence:

- [CanvasProjectionMath.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasProjectionMath.cs#L45)
- [DocumentLifecycleView.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Document/DocumentLifecycleView.cs#L40)
- [/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Packages/com.newmassmedia.unity-uitoolkit-foundation/Runtime/Tooling/Shared/ViewportFrameState.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Packages/com.newmassmedia.unity-uitoolkit-foundation/Runtime/Tooling/Shared/ViewportFrameState.cs#L155)

Why this matters:

- Overlay projection assumes a rect produced by foundation fit math.
- The preview image is rendered by UI Toolkit image scaling rules.
- These are not guaranteed to be identical, especially if:
  - `viewBox` is missing
  - content bounds are used as fallback
  - `preserveAspectRatio` semantics differ
  - vector image viewport and UI Toolkit `ScaleToFit` do not match exactly

Impact:

- Even if bbox math is correct in scene space, overlay can still draw in the wrong place.

### 4. Transform patching still uses scene-space pivots directly

Scale transforms are emitted by prepending `translate(pivot) scale(...) translate(-pivot)` directly from the scene-space pivot.

Evidence:

- [TransformStringBuilder.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Document/Transforms/TransformStringBuilder.cs#L45)
- [StructureDocumentEditService.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Document/Structure/StructureDocumentEditService.cs#L128)

Why this matters:

- SVG `transform` on an element is not guaranteed to interpret that pivot in the same coordinate space as the editor's scene/world rect.
- If the element has parent transforms, group transforms, or inherited coordinate changes, using scene pivot directly is wrong.

Impact:

- Resize and future rotate behavior can visually drift even if selection rects look correct at drag start.

### 5. Fallback bounds fix is plausible but still unverified

Fallback bounds now transform the four corners of `VectorUtils.SceneNodeBounds(node)` with `worldTransform`.

Evidence:

- [PreviewSnapshotGeometryBuilder.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Preview/PreviewSnapshotGeometryBuilder.cs#L162)

Why this is still risky:

- It is not yet verified whether `VectorUtils.SceneNodeBounds(node)` is strictly local-space for this node, or already partially aggregated.
- If `SceneNodeBounds` already accounts for descendant transforms in some cases, this can double-apply transforms.

Status:

- This may be correct.
- It is not yet proven.
- It needs fixture-based validation before trusting it as canonical fallback behavior.

## Most Likely Root Cause Order

If Claude only checks three things first, check them in this order:

1. `CanvasElementDragController` move delta conversion uses the current transient `PreviewSnapshot` instead of a drag-start mapping.
2. Overlay projection math does not necessarily match the actual rendered image rect.
3. Scale patching uses scene pivots without converting into the element's effective transform space.

## Suggested Fix Strategy

### Step 1. Freeze drag-start projection

Do not use the current `host.PreviewSnapshot` for move delta conversion after drag begins.

Instead:

- capture drag-start scene-to-viewport mapping once
- keep it immutable for the whole interaction
- convert viewport delta using that frozen mapping

Likely implementation location:

- [CanvasElementDragController.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasElementDragController.cs)
- [CanvasProjectionMath.cs](/Users/maemi/Documents/Git/04.Unity/CoreLibrary/Assets/unity-svg-editor/Editor/Scripts/Workspace/Canvas/CanvasProjectionMath.cs)

### Step 2. Make preview image rect explicit

Stop inferring image placement indirectly.

Options:

- compute and store the actual content rect once, then use it for both:
  - preview image placement
  - overlay projection
- or move preview rendering into a container whose exact rect is also the overlay rect

Goal:

- one rect source for both image and overlay

### Step 3. Treat interaction preview as the only active canvas source during drag

`ActiveCanvasSourceText` should stop being a dead field and become the actual source used by canvas-facing systems.

Goal:

- preview snapshot
- overlay hit test
- bbox
- selection

must all come from the same active source at all times.

### Step 4. Fix transform patch coordinate space

Before applying `scale(...)` or later `rotate(...)`, convert pivot from editor scene space into the element's transform space that the SVG attribute expects.

This likely requires:

- using `PreviewElementGeometry.WorldTransform`
- deriving inverse matrix
- converting scene pivot into local / parent-relative space before writing transform strings

## Minimal Reproduction Cases Claude Should Add

1. SVG with no `viewBox`, small content, then drag move repeatedly
2. SVG with negative coordinates, then click/select/drag
3. Element inside transformed `<g>` with scale or translation
4. Resize on element inside transformed parent
5. Triangle tessellation fallback case

## Current Recommendation

Do not keep trying to patch bbox symptoms one-by-one.
The next work should be:

1. freeze drag-start mapping
2. unify actual preview rect and overlay rect
3. make `ActiveCanvasSourceText` real
4. fix transform-space conversion for scale / rotate

Without that, bbox fixes will continue to regress.
