# Example: Social Links

## Overview

This example demonstrates an editable list of social-media links used "as is". Each row is a platform-labelled URL field; new rows are added by picking a platform from a wheel picker, and existing rows can be removed. It shows how to seed rows, supply placeholder text per platform, and read the entered links back out as data.

## Controls Featured

- [Social Link Container](/uitoolkit/controls/social-link-container/) — the editable list itself, shown in edit mode so the "+ Add Social" button and per-row remove (✕) buttons are visible; the demo seeds two rows and reads them back with `GetSocials()`
- [Pill Button](/uitoolkit/controls/pill-button/) — the gradient "Show Entered Links" button that dumps the current rows into the status label

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject and attach a `UIDocument` component.
3. Create a `PanelSettings` asset configured for portrait mobile (Scale With Screen Size, reference resolution 1080 × 1920, Match → Height) and assign it to the `UIDocument`.
4. Add the `SocialLinksDemo` MonoBehaviour (found in `Examples~/SocialLinks/`) to the same GameObject. It calls `GetComponent<UIDocument>()` in `Start`, so no manual reference assignment is needed.
5. Press **Play**.

> A ready-made scene is provided in the folder. A UXML-authored variant (`SocialLinksUxmlBinder` + `SocialLinksUxmlView.uxml`) declares the container with `edit-mode="true"` and seeds/reads the rows from C#.

## What to Expect

A card titled "Social Links" shows the container labelled "Your Profiles", pre-seeded with an Instagram row (`@uitoolkit.extensions`) and a YouTube row (`UnityUIExtensions`). Because edit mode is on, a "+ Add Social" button and a ✕ button on each row are visible.

- Tap **+ Add Social** to insert a row with a wheel picker offering only platforms not already in the list; pick one to commit the row, or dismiss the picker without choosing to discard it.
- Type a handle or URL into a row's field. The placeholder is resolved per platform (e.g. "Enter your Instagram handle or URL").
- Tap a row's **✕** to remove it.
- Tap **Show Entered Links** to read every non-empty row back via `GetSocials()` and list each `platform: url` in the status label.

## Key Code Patterns

Configuring the container, supplying a placeholder resolver, and seeding rows:

```csharp
socialLinks = UIToolkitExtensions.CreateVisualElement<SocialLinkContainer>(card, "socialLinksDemo__container");
socialLinks.Label = "Your Profiles";
socialLinks.IsInEditMode = true;
socialLinks.PlatformPlaceholderResolver = platform => $"Enter your {platform} handle or URL";

socialLinks.CreateSocial(SocialLinkContainer.SocialPlatform.Instagram, "@uitoolkit.extensions");
socialLinks.CreateSocial(SocialLinkContainer.SocialPlatform.YouTube, "UnityUIExtensions");
```

Reading the entered links back out as data:

```csharp
private void OnShowClicked()
{
    List<SocialLink> socials = socialLinks.GetSocials();
    if (socials.Count == 0)
    {
        statusLabel.text = "No links entered yet.";
        return;
    }

    var builder = new StringBuilder();
    foreach (SocialLink social in socials)
    {
        builder.AppendLine($"{social.platform}: {social.url}");
    }

    statusLabel.text = builder.ToString().TrimEnd();
}
```
