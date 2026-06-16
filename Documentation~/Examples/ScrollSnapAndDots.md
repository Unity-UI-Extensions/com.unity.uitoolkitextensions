# Example: Scroll Snap and Dots

## Overview

This example demonstrates paged horizontal scrolling with synchronized navigation controls. A `ScrollSnap` container holds five full-width pages; a `PageDotIndicator` beneath it stays in sync as the user moves between pages. The fourth page showcases a `ComingSoonMessage` placeholder to demonstrate how unavailable content can be gracefully handled inside a paged layout.

## Controls Featured

- [ScrollSnap](../Controls/ScrollSnap.md) — provides snap-to-page horizontal scrolling with swipe gesture support and programmatic prev/next navigation
- [PageDotIndicator](../Controls/PageDotIndicator.md) — renders a row of dots whose active dot mirrors the current scroll page
- [ComingSoonMessage](../Controls/ComingSoonMessage.md) — drop-in placeholder shown on page 4 to represent content that is not yet available

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject named `ScrollSnapDotsDemo` to the scene hierarchy.
3. Add a `UIDocument` component to the GameObject.
4. Create a `PanelSettings` asset (`Assets > Create > UI Toolkit > Panel Settings`) and configure it:
   - **Scale Mode:** Scale With Screen Size
   - **Reference Resolution:** 1080 × 1920
   - **Screen Match Mode:** Match Width Or Height, blended toward Height
5. Assign the `PanelSettings` asset to the `UIDocument` component's **Panel Settings** field.
6. Add the `ScrollSnapAndDotsDemo` MonoBehaviour (found in `Examples~/ScrollSnapAndDots/`) to the same GameObject.
7. Press **Play**.

## What to Expect

On entering Play mode you will see a full-width, full-height paged view with five pages:

- **Pages 1–3 and 5** each display a distinct solid background color and a centered page-number label.
- **Page 4** displays a `ComingSoonMessage` in place of content.
- A row of five dots appears below the scroll area; the dot corresponding to the active page is highlighted.
- **Previous** and **Next** buttons flank the dot indicator. The Previous button is hidden on page 1 and the Next button is hidden on page 5.

Swipe left or right anywhere on the scroll area to advance or retreat one page. The dots and buttons update immediately after each snap completes.

## Key Code Patterns

Wiring the `PageDotIndicator` to the `ScrollSnap` page-change event:

```csharp
_scrollSnap.OnPageChanged += pageIndex =>
{
    _dotIndicator.SetActiveDot(pageIndex);
    _prevButton.style.display = pageIndex > 0
        ? DisplayStyle.Flex : DisplayStyle.None;
    _nextButton.style.display = pageIndex < _scrollSnap.PageCount - 1
        ? DisplayStyle.Flex : DisplayStyle.None;
};
```
