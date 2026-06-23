# CircularImageButton

## Summary

`CircularImageButton` is a circular button that displays an image or sprite at full bleed with a 50% border-radius. When no image is set it shows a centered overlay (typically an upload icon and label) to prompt the user to select a photo.

Typical use cases:

- Avatar display and photo selection in profile screens
- Circular media thumbnails in lists or grids
- Any tap target that must hold a user-supplied image

## Properties

This control has no data properties. Configure it through method calls and respond to the `Clicked` event.

## USS Classes

| Class | Description |
| --- | --- |
| `circularImageButton` | Root element. Applies circular clip via `border-radius: 50%`. |
| `circularImageButton__image` | The image layer. Absolutely positioned, fills the button, `border-radius: 50%`. |
| `circularImageButton__noImageOverlay` | Overlay shown when no image is set. Absolutely positioned and centered. |
| `circularImageButton__icon` | Icon inside the no-image overlay (typically an upload/camera glyph). |
| `circularImageButton__uploadLabel` | Text label inside the no-image overlay. |
| `circularImageButton--hasImage` | Modifier applied to the root when an image is present. Hides the no-image overlay. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `Clicked` | Fired when the button receives a pointer-up event inside its bounds. | none |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetImage(Texture2D texture, bool isDefault = false)` | Sets the displayed image from a `Texture2D`. Pass `isDefault = true` to treat the image as a placeholder (does not apply the `--hasImage` modifier). |
| `SetImage(Sprite sprite, bool isDefault = false)` | Sets the displayed image from a `Sprite`. Same `isDefault` semantics. |
| `SetUploadLabel(string text)` | Updates the text shown inside the no-image overlay. |
| `ClearImage()` | Removes the current image and restores the no-image overlay. |
| `SetImageTint(Color color)` | Applies a tint color to the image element. |

## Using the Control

### Basic Setup

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class AvatarController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private Texture2D _defaultAvatar;

    private CircularImageButton _avatarButton;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _avatarButton = new CircularImageButton();

        // Show a default placeholder image without hiding the overlay
        _avatarButton.SetImage(_defaultAvatar, isDefault: true);
        _avatarButton.SetUploadLabel("Tap to change");

        _avatarButton.Clicked += OnAvatarTapped;
        root.Add(_avatarButton);
    }

    private void OnAvatarTapped()
    {
        // Open native photo picker, then call SetImage with the result
        Debug.Log("Avatar tapped — open photo picker");
    }

    private void ApplyPickedPhoto(Texture2D picked)
    {
        // isDefault: false — hides the overlay and marks the button as having an image
        _avatarButton.SetImage(picked, isDefault: false);
    }

    private void ResetAvatar()
    {
        _avatarButton.ClearImage();
    }
}
```

### Tint and Dynamic Color

```csharp
// Grey-out the avatar when the profile is locked
_avatarButton.SetImageTint(new Color(1f, 1f, 1f, 0.4f));

// Restore full color
_avatarButton.SetImageTint(Color.white);
```
