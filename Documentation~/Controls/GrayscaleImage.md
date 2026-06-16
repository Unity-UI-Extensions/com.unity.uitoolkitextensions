# GrayscaleImage

## Summary

`GrayscaleImage` is an `ImmediateModeElement` that renders a sprite or texture using `Graphics.DrawTexture` and supports toggling a greyscale effect via a material property. The greyscale effect requires a custom `Material` that exposes `_MainTex` and `_GreyscaleEnabled` shader properties; without a compatible material the image renders in full color only.

Typical use cases:

- Profile or media images that switch to greyscale when disabled or locked
- Toggling greyscale on achievement/badge imagery for locked states
- Any image that needs a shader-driven color/greyscale toggle without a separate texture asset

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `SpriteProperty` | The `Sprite` to render. Mutually exclusive with `TextureProperty`. | `Sprite` |
| `TextureProperty` | The `Texture` to render. Mutually exclusive with `SpriteProperty`. | `Texture` |
| `scaleMode` | How the image is scaled within its bounds. | `ScaleMode` |
| `Material` | The material used for rendering. Must expose `_MainTex` and `_GreyscaleEnabled` for the greyscale feature. | `Material` |
| `GreyscaleEnabled` | Gets or sets whether the greyscale shader effect is active. Only functional when a compatible material is assigned. | `bool` |
| `MainTextureProperty` | The shader property name for the main texture. | `string` (default `"_MainTex"`) |
| `GreyscaleToggleProperty` | The shader property name for the greyscale toggle. | `string` (default `"_GreyscaleEnabled"`) |

### Constants

| Name | Value | Description |
| --- | --- | --- |
| `DefaultMainTextureProperty` | `"_MainTex"` | Default shader property name for the main texture. |
| `DefaultGreyscaleToggleProperty` | `"_GreyscaleEnabled"` | Default shader property name for the greyscale toggle. |

## USS Classes

| Class | Description |
| --- | --- |
| `grayscaleImage` | Root element. Dimensions should be set via USS or inline style; the element uses its resolved layout rect for `Graphics.DrawTexture`. |

## Events

This control does not emit events. Repaint is triggered automatically by the immediate-mode element lifecycle.

## Public Methods

This control exposes its API entirely through properties. No additional public methods are defined beyond those inherited from `ImmediateModeElement`.

## Using the Control

### Toggle Greyscale on a Locked Achievement

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class AchievementBadgeController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private Sprite _badgeSprite;
    [SerializeField] private Material _greyscaleMaterial; // shader with _MainTex + _GreyscaleEnabled

    private GrayscaleImage _badgeImage;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _badgeImage = new GrayscaleImage();
        _badgeImage.style.width = 80;
        _badgeImage.style.height = 80;
        _badgeImage.SpriteProperty = _badgeSprite;
        _badgeImage.Material = _greyscaleMaterial;
        _badgeImage.scaleMode = ScaleMode.ScaleToFit;

        root.Q<VisualElement>("badgeContainer").Add(_badgeImage);
    }

    public void SetLocked(bool locked)
    {
        _badgeImage.GreyscaleEnabled = locked;
    }
}
```

### Texture-Based Rendering with Custom Property Names

```csharp
// If your shader uses different property names:
_badgeImage.MainTextureProperty = "_BaseMap";
_badgeImage.GreyscaleToggleProperty = "_UseGreyscale";
_badgeImage.TextureProperty = _photoTexture;
_badgeImage.GreyscaleEnabled = true;
```
