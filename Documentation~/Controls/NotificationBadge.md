# NotificationBadge

## Summary

`NotificationBadge` is a small, rounded unread-count badge — the kind that sits over an icon or avatar to show a number of pending items. It renders a single count `Label` inside a pill/dot root. The badge manages its own visibility: when the count is zero or less it hides itself (`display: none`), and when the count reaches 100 or more the label clamps to `"99+"` so the badge never grows unbounded.

The control owns only the count. It carries no positioning logic of its own — overlay it on a host element (icon, tab, avatar) with USS (`position: absolute` plus `top`/`right`) in your own stylesheet.

Typical use cases:

- Unread message / notification counts over an inbox or bell icon
- Cart item counts on a shop button
- Pending-item badges on tab bar or avatar elements

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `Count` | Gets or sets the displayed count. Setting it updates visibility and the label text. Bindable in UXML via the `count` attribute. | `int` |

## USS Classes

| Class | Description |
| --- | --- |
| `notificationBadge` | Root element. Style its background, size, and `border-radius` here, and position it over its host (e.g. `position: absolute`). |
| `notificationBadge__count` | The count `Label` centered within the badge. Picking is ignored so taps pass through to the host. |

## Public Methods

| Signature | Description |
| --- | --- |
| `SetCount(int value)` | Sets the count, then shows the badge (`Count > 0`) or hides it (`Count <= 0`). Displays `"99+"` for any value of 100 or more. Equivalent to assigning `Count`. |

## Using the Control

### Overlay a badge on an icon button

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class InboxButtonController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private NotificationBadge _badge;

    private void OnEnable()
    {
        var inboxButton = _document.rootVisualElement.Q<VisualElement>("inbox-button");

        _badge = new NotificationBadge();
        inboxButton.Add(_badge);   // position via USS on .notificationBadge

        _badge.Count = 3;          // badge appears showing "3"
    }

    public void MarkAllRead()
    {
        _badge.Count = 0;          // badge hides itself
    }
}
```

### In UXML

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:ext="UnityUIToolkit.Extensions">
    <ui:VisualElement name="inbox-button" class="icon-button">
        <ext:NotificationBadge count="5" />
    </ui:VisualElement>
</ui:UXML>
```

The badge clamps automatically: `_badge.Count = 250;` displays `"99+"`.
