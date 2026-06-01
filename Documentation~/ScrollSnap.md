# ScrollSnap

## Summary

`ScrollSnap` is a page-based UI Toolkit container that arranges children as pages and snaps to a page boundary after gesture or wheel input. It supports:

- Horizontal or vertical paging
- Programmatic page navigation (`GoToPage`, `MoveNext`, `MovePrevious`)
- Optional touch/pointer gesture control
- Optional validation/restriction flow for swipe attempts
- Smooth snap animation with configurable easing
- Page spacing via per-page paddings

Typical use cases:

- Onboarding or wizard flows
- Carousel-like page navigation
- Validation-gated step transitions (for example: required fields before moving forward)

## Properties

| Name | Description | Options |
|---|---|---|
| `Orientation` | Paging axis for layout and swipe direction. | `Horizontal` (default), `Vertical` |
| `PageSize` | Explicit page size in pixels. If `<= 0`, uses viewport resolved size (`width` for horizontal, `height` for vertical). | `float` (`<= 0` means auto) |
| `ManualMovementEnabled` | Enables pointer/touch and wheel gesture handling by the control. If disabled, input gestures are ignored and only programmatic navigation changes pages. | `true` / `false` (default `false`) |
| `OnlySinglePageSwipeAllowed` | Limits swipe transitions to one page max per completed gesture. Keeps behavior predictable for step-based flows. | `true` / `false` (default `true`) |
| `ValidatePageChange` | Enables swipe validation behavior using direction flags and optional async validator callback. | `true` / `false` (default `false`) |
| `CanMoveNextPage` | Forward movement gate when validation is enabled. Typically set externally before allowing the next forward step. | `true` / `false` (default `true`, reset to `false` after page change when validation is enabled) |
| `CanMoveBackPage` | Backward movement gate when validation is enabled. | `true` / `false` (default `true`, reset to `false` after page change when validation is enabled) |
| `AllowMoveBack` | Master backward permission gate when validation is enabled. If `false`, backward swipe attempts are restricted and snap back. | `true` / `false` (default `true`) |
| `ValidationDragLimit` | Maximum blocked-drag preview distance as a fraction of page size (`0..1`). Example: `0.2` allows 20% drag before clamp while restricted. | `float` in `0..1` (default `0.2`) |
| `IsValidatingPageChange` | Read-only state indicating an async page validation callback is currently running. | `bool` (read-only) |
| `PagePaddingLeft` | Left padding/gap per page (applied as margin). | `float` pixels, `>= 0` |
| `PagePaddingRight` | Right padding/gap per page (applied as margin). | `float` pixels, `>= 0` |
| `PagePaddingTop` | Top padding/gap per page (applied as margin). | `float` pixels, `>= 0` |
| `PagePaddingBottom` | Bottom padding/gap per page (applied as margin). | `float` pixels, `>= 0` |
| `PageCount` | Number of child pages in `contentContainer`. | `int` (read-only) |
| `CurrentPageIndex` | Current snapped page index. | `int` (read-only) |

### USS Custom Properties

| Name | Description | Default |
|---|---|---|
| `--scrollsnap-easing` | Snap animation easing function name. | `Linear` |
| `--scrollsnap-page-padding-left` | Left page margin/padding from USS. | `0px` |
| `--scrollsnap-page-padding-right` | Right page margin/padding from USS. | `0px` |
| `--scrollsnap-page-padding-top` | Top page margin/padding from USS. | `0px` |
| `--scrollsnap-page-padding-bottom` | Bottom page margin/padding from USS. | `0px` |
| `--scrollsnap-validation-drag-limit` | Optional validation drag-limit value (fraction). Use values like `0.2`, `0.15`, `0.3`. | Not set unless provided |

## Events

| Name | Description | Arguments |
|---|---|---|
| `PageChanged` | Fired when `CurrentPageIndex` changes after a completed transition/snap. | `(int currentPageIndex)` |
| `OnPageStartChange` | Fired when a swipe attempt resolves to a target page and validation logic begins. Host can pre-load target content and inspect whether movement is currently allowed by flags. | `(int targetPage, bool moveAllowed)` |
| `OnPageChangeRestricted` | Fired when a swipe attempt is denied and the control has completed returning/snapping back to the active page. Useful for user feedback animations/toasts. The argument is the attempted destination page index. | `(int attemptedTargetPage)` |
| `OnValidatePageTransition` | Optional async validator callback. Called when validation mode is enabled and direction flags allow movement. Return `true` to proceed, `false` to restrict and snap back. | `(int targetPage) => Task<bool>` |

Note: `OnPageChangePrevented` is not a built-in `ScrollSnap` event name. It is a consumer-defined callback method name commonly subscribed to `OnPageChangeRestricted`, for example: `scrollSnap.OnPageChangeRestricted += OnPageChangePrevented;`.

## Using the Control in Manual Mode

Manual mode means gesture input is disabled and page movement is API-driven.

- Set `ManualMovementEnabled = false`
- Navigate only through `GoToPage`, `MoveNext`, `MovePrevious`
- Use `force: true` when you want to bypass validation gates

Example:

```csharp
var snap = new ScrollSnap();
snap.ManualMovementEnabled = false;

// Programmatic navigation
snap.GoToPage(2, animate: true);
snap.MoveNext(animate: true);

// Always move, even if validation mode and flags are restrictive
snap.MoveNext(animate: true, force: true);
```

Recommended when:

- Navigation is fully controlled by external UI buttons
- You need deterministic transitions with no user drag gestures
- Validation/permissions are handled outside swipe interaction

## Using the Control in Swipe Mode

Swipe mode means gesture input is enabled and users can drag/swipe pages directly.

- Set `ManualMovementEnabled = true`
- Optionally keep `OnlySinglePageSwipeAllowed = true` for step-by-step flows
- Enable `ValidatePageChange = true` to gate movement with restriction behavior

Example:

```csharp
var snap = new ScrollSnap();
snap.ManualMovementEnabled = true;
snap.OnlySinglePageSwipeAllowed = true;
snap.ValidatePageChange = true;

// Host-controlled movement gates
snap.CanMoveNextPage = false;
snap.CanMoveBackPage = true;
snap.AllowMoveBack = true;

snap.OnPageStartChange += (targetPage, moveAllowed) =>
{
    // Prepare content for target page, optionally show pre-transition UI
};

snap.OnPageChangeRestricted += targetPage =>
{
    // Show why movement is blocked
};

snap.OnValidatePageTransition = async targetPage =>
{
    // Async validation (API call, form checks, etc.)
    await Task.Yield();
    return true;
};
```

## Restricted Movement and Event Behavior

When `ValidatePageChange = true`, swipe transitions follow this flow:

1. User swipes and a target page is derived.
2. Direction gates are checked:
   - Forward: `CanMoveNextPage`
   - Backward: `AllowMoveBack` and `CanMoveBackPage`
3. `OnPageStartChange(targetPage, moveAllowed)` is fired.
4. If blocked by gates:
   - Drag is clamped by `ValidationDragLimit`
    - On release, the control snaps back
    - `OnPageChangeRestricted(targetPage)` is fired after snap-back completes
5. If allowed by gates and `OnValidatePageTransition` is assigned:
   - Async callback runs
   - `true` => transition proceeds
    - `false` => restricted snap-back and `OnPageChangeRestricted` (after snap-back completes)
6. On successful transition completion, `PageChanged(newPageIndex)` is fired.

## Authorization Flow (Validation-Gated Navigation)

Use this flow when page movement must be authorized by host logic (for example onboarding step completion, server-side checks, required field validation).

### Core Idea

- `OnPageStartChange` is the early signal: user attempted a transition.
- `moveAllowed` indicates whether current direction gates allow a possible transition.
- `OnValidatePageTransition` is the async authorization hook.
- `OnPageChangeRestricted` is the post-snap-back signal (fires after return to the original page is complete).

### Direction Detection

At `OnPageStartChange(targetPage, moveAllowed)`, compare:

- `CurrentPageIndex` (current/origin page)
- `targetPage` (attempted destination)

Rules:

- `targetPage > CurrentPageIndex` => forward
- `targetPage < CurrentPageIndex` => backward
- `targetPage == CurrentPageIndex` => no effective move

`CurrentPageIndex` does not advance until a successful page transition completes and `PageChanged` is raised.

### Event Sequence: Authorized Success

1. `OnPageStartChange(targetPage, true)`
2. Optional `OnValidatePageTransition(targetPage)` returns `true`
3. Transition animates to target
4. `PageChanged(newPageIndex)`

### Event Sequence: Restricted by Gates

1. `OnPageStartChange(targetPage, false)`
2. Control animates back to origin page
3. `OnPageChangeRestricted(attemptedTargetPage)` fires after snap-back completion

### Event Sequence: Async Authorization Denied

1. `OnPageStartChange(targetPage, true)`
2. `OnValidatePageTransition(targetPage)` returns `false`
3. Control animates back to origin page
4. `OnPageChangeRestricted(attemptedTargetPage)` fires after snap-back completion

### Recommended Host Pattern

- In `OnPageStartChange`: detect direction and optionally do lightweight target prep only when `moveAllowed` is true.
- In `OnPageChangeRestricted`: start validation/error animation (this now runs when the control has already returned to the origin page).
- In `PageChanged`: finalize UI state for the newly active page.

### Important Notes

- While async validation is running, `IsValidatingPageChange` is `true` and additional swipe gesture processing is ignored.
- `CanMoveNextPage` and `CanMoveBackPage` are reset to `false` after a page change when validation mode is enabled.
- Use fraction values for drag limit configuration (for example `0.2` for 20%).
- Programmatic movement remains available at all times via the `force` override:

```csharp
snap.MoveNext(animate: true, force: true);
```

### Programmatic API Summary

| Method | Purpose |
|---|---|
| `GoToPage(int index, bool animate = true, bool force = false)` | Navigate to a specific page |
| `MoveNext(bool animate = true, bool force = false)` | Navigate to next page |
| `MovePrevious(bool animate = true, bool force = false)` | Navigate to previous page |

Use `force: true` when a host flow has completed external validation and wants to advance immediately regardless of current swipe-gate state.
