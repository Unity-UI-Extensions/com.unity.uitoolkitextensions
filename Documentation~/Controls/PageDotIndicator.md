# PageDotIndicator

## Summary

`PageDotIndicator` renders a row of dot indicators that communicate the current position within a paged sequence. All dots up to and including `CurrentPage` are styled as completed. The dot list is rebuilt automatically when `TotalPages` changes. Colors can be overridden via USS custom properties or inline method calls.

Typical use cases:

- Onboarding flow page position dots
- Carousel or `ScrollSnap` position indicator
- Multi-step form or wizard step markers

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `CurrentPage` | Gets or sets the zero-based index of the current page. Updates dot completed state. | `int` |
| `TotalPages` | Gets or sets the total number of dots to render. Changing this value rebuilds all dot elements. | `int` |
| `NormalizedProgress` | Gets the current progress as a value in `[0, 1]`. Computed from `CurrentPage / max(TotalPages - 1, 1)`. | `float` (read-only) |

## USS Classes

| Class | Description |
| --- | --- |
| `pageDotIndicator` | Root element. |
| `pageDotIndicator__dotsContainer` | Flex container that holds the individual dot elements. |
| `pageDotIndicator__dot` | Individual dot element. Fixed at 8 × 8 px with 4 px margin on each side. |
| `pageDotIndicator__dot--completed` | Modifier applied to every dot whose index is less than or equal to `CurrentPage`. |

## Events

This control does not emit events.

## Public Methods

| Signature | Description |
| --- | --- |
| `SetProgress(int currentPage, int totalPages)` | Sets both `CurrentPage` and `TotalPages` in one call. Rebuilds dots if `totalPages` changed. |
| `SetCompletedColor(string hex)` | Sets the background color of completed dots using a hex string (e.g. `"#e94560"`). |
| `SetPendingColor(string hex)` | Sets the background color of pending (not yet reached) dots using a hex string. |
| `SetColors(string completedHex, string pendingHex)` | Convenience method that sets both completed and pending colors in one call. |

## Using the Control

### Syncing with ScrollSnap

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class OnboardingController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private ScrollSnap _scrollSnap;
    private PageDotIndicator _dotIndicator;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        _scrollSnap = root.Q<ScrollSnap>("onboardingSnap");

        _dotIndicator = new PageDotIndicator();
        _dotIndicator.SetColors(completedHex: "#e94560", pendingHex: "#555555");
        _dotIndicator.SetProgress(currentPage: 0, totalPages: _scrollSnap.PageCount);

        root.Q<VisualElement>("footerContainer").Add(_dotIndicator);

        _scrollSnap.PageChanged += pageIndex =>
        {
            _dotIndicator.CurrentPage = pageIndex;
        };
    }
}
```

### Standalone Step Indicator

```csharp
// Set up a 5-step progress tracker, starting at step 2
_dotIndicator.SetProgress(currentPage: 2, totalPages: 5);

// Advance one step
_dotIndicator.CurrentPage++;

// Read normalized progress for use in other UI
float progress = _dotIndicator.NormalizedProgress; // 0.0 – 1.0
```
