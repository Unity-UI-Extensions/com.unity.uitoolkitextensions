# ProceduralTextureUtility

**Namespace:** `UnityUIToolkit.Extensions`  
**Type:** `static class`  
**File:** `Runtime/Utility/ProceduralTextureUtility.cs`

---

## Summary

Factory class for creating runtime-generated `Texture2D` objects used by UI controls and demo scenes.
Centralises procedural texture logic so individual controls do not duplicate generation code.

> **Caller responsibility:** every texture returned by this class is unmanaged — call `Object.Destroy(texture)` when it is no longer needed to avoid GPU memory leaks.

---

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateHorizontalGradient(startColor, endColor, width)` | `Texture2D` | 1-pixel-tall horizontal gradient from `startColor` to `endColor`. |
| `CreateSpinnerArc(size, arcColor, sweepDegrees, ringFraction)` | `Texture2D` | Square texture with a single-color arc ring for use as a loading spinner icon. |
| `CreateSolidCircle(size, color)` | `Texture2D` | Square texture with a soft anti-aliased filled circle. |

---

## Method Details

### CreateHorizontalGradient

```csharp
public static Texture2D CreateHorizontalGradient(
    Color startColor,
    Color endColor,
    int width = 256)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `startColor` | — | Left-edge color. |
| `endColor` | — | Right-edge color. |
| `width` | `256` | Horizontal resolution in pixels. |

Returns a `width × 1` RGBA32 texture with pixels linearly interpolated between the two colors.
Used internally by `PillButton` and `StepProgressBar`.

---

### CreateSpinnerArc

```csharp
public static Texture2D CreateSpinnerArc(
    int size,
    Color arcColor,
    float sweepDegrees = 270f,
    float ringFraction = 0.15f)
```

| Parameter | Default | Description |
|-----------|---------|-------------|
| `size` | — | Width and height in pixels (square). |
| `arcColor` | — | Color of the arc stroke. |
| `sweepDegrees` | `270` | How many degrees of the ring are filled. |
| `ringFraction` | `0.15` | Ring thickness as a fraction of the radius. |

Pixels outside the ring annulus or outside the sweep angle are fully transparent.
The arc is centered at the top of the circle (−90° axis) and sweeps symmetrically.
Pass the returned texture to `LoadingIcon.SetIcon(texture)` to override the default SVG spinner.

---

### CreateSolidCircle

```csharp
public static Texture2D CreateSolidCircle(int size, Color color)
```

| Parameter | Description |
|-----------|-------------|
| `size` | Width and height in pixels (square). |
| `color` | Fill color; a 1-pixel soft edge is applied for anti-aliasing. |

Useful for generating circular avatar placeholders in demo scenes.

---

## Usage Examples

### Gradient button background

```csharp
Texture2D gradient = ProceduralTextureUtility.CreateHorizontalGradient(
    new Color(0.27f, 0.55f, 0.87f),
    new Color(0.48f, 0.28f, 0.85f));
background.style.backgroundImage = new StyleBackground(gradient);
// ...later...
Object.Destroy(gradient);
```

### Custom spinner icon on LoadingIcon

```csharp
private Texture2D spinnerTexture;

void Start()
{
    spinnerTexture = ProceduralTextureUtility.CreateSpinnerArc(
        size: 64,
        arcColor: new Color(0.27f, 0.55f, 0.87f),
        sweepDegrees: 270f);
    loadingIcon.SetIcon(spinnerTexture);
    loadingIcon.PlayLoading(customSpeed: 0.9f, blockInteraction: true);
}

void OnDestroy()
{
    if (spinnerTexture != null)
        Destroy(spinnerTexture);
}
```

### Avatar placeholder circle

```csharp
Texture2D avatar = ProceduralTextureUtility.CreateSolidCircle(128, new Color(0.6f, 0.4f, 0.8f));
circularImageButton.SetImage(avatar);
// Destroy when scene unloads
```

---

## Notes

- All methods use `TextureFormat.RGBA32` and `wrapMode = TextureWrapMode.Clamp`.
- Textures are not cached — call the method once and store the result; repeated calls allocate new GPU memory each time.
- `CreateSpinnerArc` produces a static texture; the rotation animation is applied by `LoadingIcon`'s USS transition on the image element.
