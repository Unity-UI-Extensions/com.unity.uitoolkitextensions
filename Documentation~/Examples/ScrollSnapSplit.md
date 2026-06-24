# Example: Scroll Snap (Split Views)

## Overview

This example proves that the same scroll-snap control is fully usable from both C# and UXML by building two identical paging carousels side by side in one scene. The left half is constructed entirely in code; the right half is authored as a UXML asset and only has its live page readout wired from C#. It is the example to study when deciding between the "build it in C#" and "layout in UXML, behaviour in C#" approaches.

## Controls Featured

- [Scroll Snap](/uitoolkit/controls/scroll-snap/) — a five-page swipeable carousel that snaps to a page on release; built once in pure C# (left) and once from UXML (right), with both panels reporting the current page via the `PageChanged` event

## Scene Setup

The provided `ScrollSnapSplitDemo` scene hosts both halves on a single shared `PanelSettings`. To recreate it:

1. Create a new Unity scene.
2. Create a `PanelSettings` asset configured for portrait mobile (Scale With Screen Size, reference resolution 1080 × 1920, Match → Height).
3. Add a GameObject with a `UIDocument` for the **C# half**: add the `ScrollSnapSplitCSharpView` MonoBehaviour (found in `Examples~/ScrollSnapSplit/`), assign the `PanelSettings`, and assign that `UIDocument` to the script's `uiDocument` field. Leave the document's Source Asset empty — this view builds its tree in code.
4. Add a second GameObject with a `UIDocument` for the **UXML half**: add the `ScrollSnapSplitUxmlView` MonoBehaviour, set the document's **Source Asset** to `ScrollSnapSplitView.uxml`, assign the same `PanelSettings`, and assign that `UIDocument` to the script's `uiDocument` field.
5. Press **Play**.

Because both panels share one `PanelSettings`, each view's `Start` calls the shared `ScrollSnapSplitLayout.OccupyHalf` helper to pin its root to an exact 50% slot (left or right), so the two panels sit side-by-side without overlapping or contending for pointer events.

## What to Expect

The screen is split down the middle. Both halves show the same five full-screen colour pages (red, blue, green, purple, orange), each with a large "Page N" label and an "N of 5" sub-label. A header labels the left panel "Built in C#" and the right panel "Built in UXML", and a "Swipe ← / →" hint sits at the bottom.

- Swipe left or right within either panel to page through; the control always rests snapped on a page.
- The header readout updates live to "Showing page N of 5" as the page changes, driven by the `PageChanged` event in both panels.
- Manual swiping is explicitly enabled (`ManualMovementEnabled` / `manual-movement-enabled="true"`); children still receive their own pointer events.

## Key Code Patterns

The C# half instantiates `ScrollSnap`, enables manual movement, adds page children, and subscribes to `PageChanged`:

```csharp
scrollSnap = new ScrollSnap();
scrollSnap.style.flexGrow = 1;
scrollSnap.ManualMovementEnabled = true;
screen.Add(scrollSnap);

for (var i = 0; i < PageColors.Length; i++)
{
    scrollSnap.Add(BuildPage(i, PageColors.Length));
}

scrollSnap.PageChanged += UpdateReadout;
UpdateReadout(scrollSnap.CurrentPageIndex);
```

The UXML half declares the identical control in markup — each child element automatically becomes a page:

```xml
<ext:ScrollSnap manual-movement-enabled="true" style="flex-grow: 1;">
    <ui:VisualElement style="background-color: #E74C3C; justify-content: center; align-items: center;">
        <ui:Label text="Page 1" />
        <ui:Label text="1 of 5" />
    </ui:VisualElement>
    <!-- four more page VisualElements … -->
</ext:ScrollSnap>
```

C# then only binds behaviour onto the control cloned from UXML:

```csharp
scrollSnap = root.Q<ScrollSnap>();
readoutLabel = root.Q<Label>("page-readout");
scrollSnap.PageChanged += UpdateReadout;
UpdateReadout(scrollSnap.CurrentPageIndex);
```
