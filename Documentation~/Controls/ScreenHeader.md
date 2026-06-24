# ScreenHeader

## Summary

`ScreenHeader` is a configurable top app-bar: an optional notch spacer (to clear a device safe-area), a centered title, and up to four edge buttons. The first three (`action1`, `action2`, `action3`) are simple click buttons; the fourth (`action4`) is a stateful on/off toggle that swaps between two icons.

The control owns **no application state**. Button icons are supplied by the host (via the icon properties or USS), and every interaction is surfaced as an event — `Action1Clicked`, `Action2Clicked`, `Action3Clicked`, and `Action4Toggled(bool)`. The host decides what each button does and is responsible for any persistence behind the action-4 toggle. The buttons themselves are reused `ToggleButton` elements, so they inherit that control's press styling.

> The four buttons are deliberately generic — they are common-interaction slots (back, search, share, favourite, …), not fixed roles. Show only the ones you need with `Configure(...)` or the per-button `ShowActionN` properties.

Typical use cases:

- A consistent top bar across screens (title + back + contextual actions)
- A detail screen with a back button, a share button, and a favourite toggle
- A list screen with a title and a search action

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `Title` | Gets or sets the centered title text. An empty string hides the title label. | `string` |
| `Action1Icon` | Icon for the first (left-edge) action button. | `Texture2D` |
| `Action2Icon` | Icon for the second action button. | `Texture2D` |
| `Action3Icon` | Icon for the third action button. | `Texture2D` |
| `Action4ToggleOnIcon` | Icon shown by the action-4 toggle in its **on** state. | `Texture2D` |
| `Action4ToggleOffIcon` | Icon shown by the action-4 toggle in its **off** state. | `Texture2D` |
| `ShowAction1` | Whether the first action button is visible. | `bool` |
| `ShowAction2` | Whether the second action button is visible. | `bool` |
| `ShowAction3` | Whether the third action button is visible. | `bool` |
| `ShowAction4Toggle` | Whether the action-4 toggle is visible. | `bool` |
| `IsAction4ToggleSelected` | (Read-only) Whether the action-4 toggle is currently on. | `bool` |

## USS Classes

| Class | Description |
| --- | --- |
| `screenHeader` | Root element. |
| `screenHeader__notchSpacer` | Top spacer that reserves room for a device notch / status bar. |
| `screenHeader__bar` | The horizontal bar holding the title and buttons. |
| `screenHeader__title` | Centered title label. |
| `screenHeader__action1Button` | First (left-edge) action button. |
| `screenHeader__action2Button` | Second action button. |
| `screenHeader__action3Button` | Third action button. |
| `screenHeader__action4Toggle` | Fourth slot — the stateful on/off toggle button. |

## Events

| Name | Description | Arguments |
| --- | --- | --- |
| `Action1Clicked` | Raised when the first action button is tapped. | none |
| `Action2Clicked` | Raised when the second action button is tapped. | none |
| `Action3Clicked` | Raised when the third action button is tapped. | none |
| `Action4Toggled` | Raised when the action-4 toggle changes state; the icon swaps automatically. | `bool` (new selected state) |

## Public Methods

| Signature | Description |
| --- | --- |
| `Configure(bool showAction1, bool showTitle, bool showAction2, bool showAction3, bool showAction4Toggle)` | Sets the visibility of the title and all four buttons in one call. |
| `SetAction1ButtonVisible(bool)` / `SetAction2ButtonVisible(bool)` / `SetAction3ButtonVisible(bool)` / `SetAction4ToggleVisible(bool)` | Per-button visibility setters (equivalent to the matching `ShowActionN` property). |
| `SetAction4ToggleSelected(bool)` | Forces the action-4 toggle on/off without raising `Action4Toggled`, and refreshes its icon. Use to restore persisted state. |

## Using the Control

### A detail screen header

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class DetailScreenController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private Texture2D _backIcon;
    [SerializeField] private Texture2D _shareIcon;
    [SerializeField] private Texture2D _favOnIcon;
    [SerializeField] private Texture2D _favOffIcon;

    private ScreenHeader _header;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;

        _header = new ScreenHeader();
        _header.Title = "Track Details";

        // Slot 1 = back, slot 3 = share, slot 4 = favourite toggle. Hide slot 2.
        _header.Configure(showAction1: true, showTitle: true,
                          showAction2: false, showAction3: true, showAction4Toggle: true);

        _header.Action1Icon = _backIcon;
        _header.Action3Icon = _shareIcon;
        _header.Action4ToggleOnIcon = _favOnIcon;
        _header.Action4ToggleOffIcon = _favOffIcon;

        _header.Action1Clicked += () => Debug.Log("Back");
        _header.Action3Clicked += () => Debug.Log("Share");
        _header.Action4Toggled += isFavourite => Debug.Log($"Favourite: {isFavourite}");

        // Restore persisted favourite state without firing the event.
        _header.SetAction4ToggleSelected(PlayerPrefs.GetInt("fav") == 1);

        root.Insert(0, _header);
    }
}
```

### In UXML

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:ext="UnityUIToolkit.Extensions">
    <ext:ScreenHeader title="Home" show-action1="false" show-action3="true" />
</ui:UXML>
```
