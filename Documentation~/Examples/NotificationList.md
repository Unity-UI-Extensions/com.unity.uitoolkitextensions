# Example: Notification List

## Overview

This example builds a mobile-style notification feed that grows on demand. It pairs an elastic, overscrolling list with an unread-count badge and a pill action button, showing how to drive a live "load more" flow and keep an unread counter in sync as items are added and cleared.

## Controls Featured

- [Elastic List View](/uitoolkit/controls/elastic-list-view/) — hosts the notification rows with iOS-style elastic overscroll and raises a swipe-up "load more" request that the demo answers by appending three more items
- [Notification Badge](/uitoolkit/controls/notification-badge/) — sits at the top-right of the card and tracks the unread count; tapping it marks everything read and hides the badge
- [Pill Button](/uitoolkit/controls/pill-button/) — the gradient "Add Notification" button that appends one row and bumps the unread count

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject and attach a `UIDocument` component.
3. Create a `PanelSettings` asset configured for portrait mobile (Scale With Screen Size, reference resolution 1080 × 1920, Match → Height) and assign it to the `UIDocument`.
4. Add the `NotificationListDemo` MonoBehaviour (found in `Examples~/NotificationList/`) to the same GameObject. It calls `GetComponent<UIDocument>()` in `Start`, so no manual reference assignment is needed.
5. Press **Play**.

> A ready-made scene is provided in the folder. A UXML-authored variant (`NotificationListUxmlBinder` + `NotificationListUxmlView.uxml`) builds the same screen declaratively and queries the controls by name.

## What to Expect

A card titled "Notifications" appears with a small unread badge anchored to its top-right corner. The list is seeded with four items, each a coloured accent stripe beside a message and a "Just now" timestamp.

- Tapping **Add Notification** appends a new randomly generated row and increments the badge count (the display clamps to `99+`).
- Tapping the **badge** marks all notifications read: the count resets to zero and the badge hides itself.
- When the content fits the card, the list still gives a finger-following bounce; when it overflows it scrolls with elastic overscroll.
- Swiping up past the bottom edge requests more items: the demo simulates a short fetch (about 700 ms), shows the load-more spinner, then appends three more rows. Swipe input is unreliable in the editor, so use the **Add Notification** button there and test the swipe-to-load on device.

## Key Code Patterns

Enabling elasticity and load-more, then servicing the request by appending a batch between `BeginLoadMore` and `EndLoadMore`:

```csharp
listView = UIToolkitExtensions.CreateVisualElement<ElasticListView>(card, "notificationListDemo__list");
listView.EmptyStateText = "You're all caught up.";
listView.EnableTouchElasticity(0.12f);
listView.EnableLoadMore();
listView.LoadMoreRequested += OnLoadMoreRequested;

private void OnLoadMoreRequested()
{
    if (loadingMore) return;

    loadingMore = true;
    listView.BeginLoadMore();

    listView.schedule.Execute(() =>
    {
        for (int i = 0; i < LoadMoreBatch; i++)
        {
            listView.AddItem(CreateNotificationItem(RandomMessage()));
        }

        listView.EndLoadMore();
        loadingMore = false;
    }).StartingIn(SimulatedFetchMs);
}
```

Keeping the badge in sync — add bumps the count, tapping it clears:

```csharp
addButton.Clicked += OnAddClicked;
badge.RegisterCallback<ClickEvent>(_ => MarkAllRead());

private void OnAddClicked()
{
    listView.AddItem(CreateNotificationItem(RandomMessage()));
    unreadCount++;
    badge.SetCount(unreadCount);
}

private void MarkAllRead()
{
    unreadCount = 0;
    badge.SetCount(0);
}
```
