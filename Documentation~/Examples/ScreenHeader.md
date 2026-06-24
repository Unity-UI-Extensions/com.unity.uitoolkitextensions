# Example: Screen Header

## Overview

This example demonstrates the configurable top app-bar control on its own. It shows a header with a back button, a centered title, an info button, and a stateful sound on/off toggle, and turns each interaction into a status-line update below. Reach for it when you want a standard mobile screen header whose buttons raise events rather than owning any app behaviour.

## Controls Featured

- [Screen Header](/uitoolkit/controls/screen-header/) — a full-width bar with edge action buttons and a centered title; the demo enables the back, info, and sound-toggle buttons and listens to their events

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject and attach a `UIDocument` component.
3. Create a `PanelSettings` asset configured for portrait mobile (Scale With Screen Size, reference resolution 1080 × 1920, Match → Height) and assign it to the `UIDocument`.
4. Add the `ScreenHeaderDemo` MonoBehaviour (found in `Examples~/ScreenHeader/`) to the same GameObject. It calls `GetComponent<UIDocument>()` in `Start`, so no manual reference assignment is needed.
5. Press **Play**.

> A ready-made scene is provided in the folder. A UXML-authored variant (`ScreenHeaderUxmlBinder` + `ScreenHeaderUxmlView.uxml`) declares the same header in markup and wires its events from C#. The button icons are supplied entirely through USS (`ScreenHeaderStyles.uss`) using the package's SVG icon assets, so the demo assigns no `Texture2D` references in code.

## What to Expect

A header bar sits across the top of the screen: a back button on the left edge, a centered title, and an info button plus a sound on/off toggle on the right edge. Below it, a card explains the screen and shows a status line.

- Tapping **back** logs "Back tapped — a real app would pop this screen." to the status line.
- Tapping **info** logs "Info tapped — show help or an about panel here."
- Tapping the **sound toggle** flips between the volume-on and volume-off glyphs (the swap happens automatically via the control's selected state class) and reports either "Sound muted." or "Sound on.".

The control owns no application state — it raises `Action1Clicked` (back), `Action3Clicked` (info), and `Action4Toggled` (sound), and the host decides what each one does.

## Key Code Patterns

Configuring which buttons are visible and subscribing to the header's events:

```csharp
header = UIToolkitExtensions.CreateVisualElement<ScreenHeader>(screen, "screenHeaderDemo__header");
header.Title = "header";
header.Configure(showAction1: true, showTitle: true, showAction2: false,
    showAction3: true, showAction4Toggle: true);

header.Action1Clicked += () => SetStatus("Back tapped — a real app would pop this screen.");
header.Action3Clicked += () => SetStatus("Info tapped — show help or an about panel here.");
header.Action4Toggled += isMuted => SetStatus(isMuted ? "Sound muted." : "Sound on.");
```
