# Example: Content Explorer

## Overview

This example demonstrates deferred content loading combined with an expandable section layout. A `LoadingIcon` spinner is shown while the demo simulates a data-fetch delay; once the delay elapses the spinner is hidden and three `CollapsibleSection` containers are revealed, each populated with `IconLabelButton` items. Tapping any item updates a status label at the bottom of the screen.

## Controls Featured

- [CollapsibleSection](../Controls/CollapsibleSection.md) — expandable/collapsible container with a tappable header; used to group related `IconLabelButton` items
- [IconLabelButton](../Controls/IconLabelButton.md) — button composed of an icon and a text label; represents individual selectable content items within each section
- [LoadingIcon](../Controls/LoadingIcon.md) — animated spinner shown during the simulated loading period before content is revealed

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject named `ContentExplorerDemo` to the scene hierarchy.
3. Add a `UIDocument` component to the GameObject.
4. Create a `PanelSettings` asset (`Assets > Create > UI Toolkit > Panel Settings`) and configure it:
   - **Scale Mode:** Scale With Screen Size
   - **Reference Resolution:** 1080 × 1920
   - **Screen Match Mode:** Match Width Or Height, blended toward Height
5. Assign the `PanelSettings` asset to the `UIDocument` component's **Panel Settings** field.
6. Add the `ContentExplorerDemo` MonoBehaviour (found in `Examples~/ContentExplorer/`) to the same GameObject.
7. Press **Play**.

## What to Expect

On entering Play mode:

1. The screen shows only the `LoadingIcon` spinner, centered vertically.
2. After **1.5 seconds** the spinner fades out and three `CollapsibleSection` containers slide into view.
3. Each section has a distinct header title and contains 3–4 `IconLabelButton` items.
4. Tapping a section header toggles that section open or closed.
5. Tapping an `IconLabelButton` item within an open section updates the status label at the bottom of the screen with the item's name.

All three sections start collapsed; tap a header to expand it.

## Key Code Patterns

Building collapsible sections with `IconLabelButton` items after a simulated load delay:

```csharp
private IEnumerator SimulateLoad()
{
    loadingIcon.PlayLoading(customSpeed: 0.9f, blockInteraction: true);
    yield return new WaitForSeconds(1.5f);

    loadingIcon.StopLoading();
    loadingIcon.parent.style.display = DisplayStyle.None;

    contentScroll.style.opacity = 1f; // triggers CSS opacity transition
    statusLabel.text = "Tap any item to select it.";
}

private void BuildSection(VisualElement parent, string title, string[] items)
{
    var section = new CollapsibleSection();
    section.TitleText = title;

    foreach (var itemText in items)
    {
        var captured = itemText;
        var btn = new IconLabelButton();
        btn.Text = captured;
        btn.Clicked += () => statusLabel.text = $"Selected: {captured}";
        section.AddBodyContent(btn);
    }

    parent.Add(section);
}
```
