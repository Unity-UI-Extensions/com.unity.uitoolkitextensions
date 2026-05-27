# ComingSoonMessage

## Summary

`ComingSoonMessage` is a simple full-area placeholder element that fills its container and communicates that a feature or screen is not yet available. It renders a background layer, a prominent title, and a descriptive message label.

Typical use cases:

- Placeholder screens during development or staged rollouts
- Stub pages inside a `ScrollSnap` onboarding flow
- In-progress feature sections within a settings or navigation layout

## Properties

| Name | Description | Options |
| --- | --- | --- |
| `Title` | Gets or sets the large heading text. | `string` |

## USS Classes

| Class | Description |
| --- | --- |
| `comingSoonMessage` | Root element. Fills its parent and centers its contents. |
| `comingSoonMessage__background` | Decorative background layer (may carry color or texture). |
| `comingSoonMessage__title` | Large heading label. |
| `comingSoonMessage__label` | Secondary descriptive message label below the title. |

## Events

This control does not emit events.

## Public Methods

| Signature | Description |
| --- | --- |
| `SetMessage(string text)` | Sets the secondary descriptive message shown below the title. |

## Using the Control

### Stub Page in a ScrollSnap

```csharp
using UnityEngine;
using UnityEngine.UIElements;
using UnityUIToolkit.Extensions;

public class OnboardingController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    private void OnEnable()
    {
        var root = _document.rootVisualElement;
        var scrollSnap = root.Q<ScrollSnap>("onboardingSnap");

        // Page 1 — real content
        var welcomePage = new VisualElement();
        welcomePage.AddToClassList("sample-page");
        welcomePage.AddToClassList("sample-page--blue");
        scrollSnap.Add(welcomePage);

        // Page 2 — placeholder
        var placeholder = new ComingSoonMessage();
        placeholder.Title = "Social Features";
        placeholder.SetMessage("Connect with friends and share your progress. Launching soon.");
        scrollSnap.Add(placeholder);

        // Page 3 — another placeholder
        var placeholder2 = new ComingSoonMessage();
        placeholder2.Title = "Challenges";
        placeholder2.SetMessage("Weekly challenges and leaderboards are on their way.");
        scrollSnap.Add(placeholder2);
    }
}
```

### Dynamic Title Update

```csharp
// Swap placeholder text based on user role
if (user.IsPremium)
{
    _comingSoon.Title = "Advanced Analytics";
    _comingSoon.SetMessage("Your detailed stats dashboard is being prepared.");
}
else
{
    _comingSoon.Title = "Premium Feature";
    _comingSoon.SetMessage("Upgrade to unlock this section.");
}
```
