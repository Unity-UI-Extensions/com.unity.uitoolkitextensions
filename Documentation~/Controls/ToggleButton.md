# ToggleButton

## Summary

`ToggleButton` is a simple image button that toggles its selected state on every pointer-down event. `OnClicked` fires on every press regardless of the resulting state, making it straightforward to react to both selection and deselection in a single handler. Use `ForceSelect` and `ForceDeselect` to set state programmatically without firing the event.

Typical use cases:

- Audio mute / unmute toggle
- Dark mode / light mode switch
- Any binary feature toggle with icon feedback

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `IsSelected` | Gets or sets the current selected state. Setting this directly applies or removes the `--selected` modifier but does not fire `OnClicked`. | `bool` |

## USS Classes

| Class | Description |
| --- | --- |
| `toggleButton` | Root element. |
| `toggleButton__image` | The image element. 100% width and height, `scale-to-fit` background. |
| `toggleButton--selected` | Modifier applied when `IsSelected` is `true`. Use in USS to swap icon or apply tint. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `OnClicked` | Fired on every pointer-down event, before the state is toggled. The new `IsSelected` value is already reflected when the handler runs. | none |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetImage(Texture2D texture)` | Sets the background texture of the image element. |
| `ForceSelect()` | Sets `IsSelected = true` and applies `--selected` without firing `OnClicked`. |
| `ForceDeselect()` | Sets `IsSelected = false` and removes `--selected` without firing `OnClicked`. |

## Using the Control

### Audio Toggle

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class AudioToggleController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private Texture2D _audioOnIcon;
    [SerializeField] private Texture2D _audioOffIcon;

    private ToggleButton _audioToggle;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _audioToggle = new ToggleButton();
        _audioToggle.SetImage(_audioOnIcon);

        // Restore saved preference silently
        bool isMuted = PlayerPrefs.GetInt("AudioMuted", 0) == 1;
        if (isMuted)
            _audioToggle.ForceSelect();

        _audioToggle.OnClicked += OnAudioToggled;
        root.Q<VisualElement>("toolbarContainer").Add(_audioToggle);
    }

    private void OnAudioToggled()
    {
        bool isMuted = _audioToggle.IsSelected;
        AudioListener.volume = isMuted ? 0f : 1f;
        PlayerPrefs.SetInt("AudioMuted", isMuted ? 1 : 0);

        // Swap icon based on new state
        _audioToggle.SetImage(isMuted ? _audioOffIcon : _audioOnIcon);
        Debug.Log($"Audio {(isMuted ? "muted" : "unmuted")}");
    }
}
```

### USS Icon Swap via Modifier

Rather than swapping the texture in code you can declare both icon states in USS:

```uss
.toggleButton__image {
    background-image: url("audio-on.png");
}

.toggleButton--selected .toggleButton__image {
    background-image: url("audio-off.png");
}
```
