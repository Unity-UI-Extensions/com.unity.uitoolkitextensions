# Example: Image Crop Overlay

## Overview

This example mirrors the Yperea profile-picture editing flow with package-local assets only. It starts with a generated portrait texture, opens `ImageCropOverlayControl` from a `CircularImageButton`, and applies the saved crop back into the screen preview.

## Controls Featured

- [CircularImageButton](../Controls/CircularImageButton.md) — preview and crop entry point
- [ImageCropOverlayControl](../Controls/ImageCropOverlayControl.md) — move/scale crop overlay and texture exporter
- [PillButton](../Controls/PillButton.md) — edit and reset actions

## Scene Setup

This package now ships a ready-made sample scene in `Examples~/ImageCropOverlay/ImageCropOverlayDemo.unity`.

If you want to recreate it manually instead:

1. Create a new Unity scene.
2. Add a GameObject with a `UIDocument`.
3. Assign the sample panel settings from `Examples~/Shared/UIToolkitExtensionsExamplePanelSettings.asset`.
4. Add the `ImageCropOverlayDemo` MonoBehaviour from `Examples~/ImageCropOverlay/` to the same GameObject.
5. Press Play.

## What to Expect

The sample screen shows:

- A large `CircularImageButton` displaying the generated portrait.
- A separate saved-preview card showing the current texture state.
- An `Edit Image` button that opens the cropper.
- A `Reset` button that restores the original generated portrait.
- A status line describing crop, cancel, save, and reset actions.

## Key Code Pattern

```csharp
var configuration = new ImageCropOverlayControl.Configuration
{
    Title = "Move and Scale",
    ExportSize = 512,
    CornerRadiusPercent = ImageCropOverlayControl.CircleCornerRadiusPercent,
};

ImageCropOverlayControl.Show(
    imageButton,
    ActiveTexture,
    configuration,
    cropped => ReplaceCroppedTexture(cropped));
```