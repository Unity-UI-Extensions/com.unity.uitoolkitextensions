# LoadingIcon

## Summary

`LoadingIcon` is a continuously rotating image element used to indicate background work. Rotation is driven by a scheduled callback that fires every 16 ms. An optional `blockInteraction` flag captures pointer events so the user cannot interact with elements beneath the spinner while it is active.

Typical use cases:

- Async operation feedback (API calls, data loading)
- Image upload or file transfer progress indicator
- Form submission spinner overlay

## Properties

This control is configured through method calls. The visibility and animation state are reflected in modifier classes.

## USS Classes

| Class | Description |
| --- | --- |
| `loadingIcon` | Root element. |
| `loadingIcon__image` | The rotating image. Fixed at 40 × 40 px. |
| `loadingIcon--animating` | Modifier applied while rotation is active. |
| `loadingIcon--visible` | Modifier applied while the icon is shown. Pair with USS to control opacity or display. |

## Events

This control does not emit events.

## Public Methods

| Signature | Description |
| --- | --- |
| `SetIcon(Texture2D texture)` | Sets the texture used for the spinning image. |
| `PlayLoading(float customSpeed = 1f, bool blockInteraction = false)` | Starts the rotation animation. `customSpeed` is the duration of one full 360° rotation in seconds; lower values spin faster. When `blockInteraction` is `true` the control captures all pointer events. |
| `StopLoading()` | Stops the rotation, releases pointer capture, and removes the `--animating` and `--visible` modifiers. |

## Using the Control

### Simple Loading Overlay

```csharp
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class UploadController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private Texture2D _spinnerTexture;

    private LoadingIcon _spinner;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _spinner = new LoadingIcon();
        _spinner.SetIcon(_spinnerTexture);

        root.Q<VisualElement>("overlayContainer").Add(_spinner);

        root.Q<Button>("uploadButton").clicked += () => _ = StartUpload();
    }

    private async Task StartUpload()
    {
        // 0.8s per rotation, block taps on underlying UI
        _spinner.PlayLoading(customSpeed: 0.8f, blockInteraction: true);

        try
        {
            await UploadFileAsync();
        }
        finally
        {
            _spinner.StopLoading();
        }
    }

    private async Task UploadFileAsync()
    {
        // Simulate async upload
        await Task.Delay(2000);
    }
}
```

### Speed Variants

```csharp
// Slow, calm indicator — 1.5 seconds per revolution
_spinner.PlayLoading(customSpeed: 1.5f);

// Fast, urgent indicator — 0.4 seconds per revolution
_spinner.PlayLoading(customSpeed: 0.4f);
```
