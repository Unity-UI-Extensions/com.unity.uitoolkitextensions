# Example: Profile Editor

## Overview

This example demonstrates avatar display, image processing, and color theme selection working together in a profile customization screen. A `CircularImageButton` shows the user's avatar; a `GrayscaleImage` control displays a sample gradient image that can be toggled between full color and black-and-white via a `ToggleButton`; a five-color `ColorToggleGroup` lets the user pick a theme color and reflects the selection as a swatch and label.

## Controls Featured

- [CircularImageButton](../Controls/CircularImageButton.md) — circular masked image with an optional tap action; used as the profile avatar
- [GrayscaleImage](../Controls/GrayscaleImage.md) — image control that can render in full color or desaturated grayscale; toggled at runtime to demonstrate the effect
- [ColorToggleGroup](../Controls/ColorToggleGroup.md) — mutually exclusive group of `ColorToggleButton` items; manages single-selection across its children
- [ColorToggleButton](../Controls/ColorToggleButton.md) — individual colored circle button that forms part of the `ColorToggleGroup`
- [ToggleButton](../Controls/ToggleButton.md) — two-state toggle control; switches the `GrayscaleImage` between color and grayscale modes

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject named `ProfileEditorDemo` to the scene hierarchy.
3. Add a `UIDocument` component to the GameObject.
4. Create a `PanelSettings` asset (`Assets > Create > UI Toolkit > Panel Settings`) and configure it:
   - **Scale Mode:** Scale With Screen Size
   - **Reference Resolution:** 1080 × 1920
   - **Screen Match Mode:** Match Width Or Height, blended toward Height
5. Assign the `PanelSettings` asset to the `UIDocument` component's **Panel Settings** field.
6. Add the `ProfileEditorDemo` MonoBehaviour (found in `Examples~/ProfileEditor/`) to the same GameObject.
7. Press **Play**.

## What to Expect

The screen is divided into three vertical sections:

**Avatar row** — a `CircularImageButton` displays a placeholder avatar image. Tapping it would typically open a picker; in this demo it logs a message to the Console.

**Image preview row** — a `GrayscaleImage` shows a sample gradient image in full color by default. A `ToggleButton` labeled "B&W" sits beside it. Toggling it on desaturates the image to grayscale; toggling it off restores full color.

**Color theme row** — a `ColorToggleGroup` containing five `ColorToggleButton` items (pastel red, orange, yellow, green, and blue) is displayed horizontally. Tapping a color:

- Marks that `ColorToggleButton` as selected (visually distinct ring or scale).
- Updates a color swatch `VisualElement` and a text label below the group to reflect the chosen color name.

Only one color may be active at a time; selecting a new color automatically deselects the previous one.

## Key Code Patterns

Reacting to `ColorToggleGroup` selection changes and toggling the grayscale effect:

```csharp
// ColorToggleGroup fires OnColorSelected with the chosen Color value
_colorGroup.OnColorSelected += color =>
{
    _swatchElement.style.backgroundColor = color;
};

// ToggleButton fires OnClicked; read IsSelected for the new state
_grayscaleToggle.OnClicked += () =>
{
    _sampleImage.GreyscaleEnabled = _grayscaleToggle.IsSelected;
};
```
