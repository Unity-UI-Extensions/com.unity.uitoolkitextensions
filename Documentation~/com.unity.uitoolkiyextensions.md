<!-- Offline documentation -->

# About Unity UI Toolkit Extensions

The Unity UI Toolkit Extensions project is a collection of extension scripts/effects and controls to enhance your Unity UI Toolkit experience.

You can follow the UI Toolkit Extensions team for updates and news on:

## [Twitter - #unityuiextensions](https://twitter.com/search?q=%23unityuiextensions) / [Facebook](https://www.facebook.com/UnityUIExtensions/) / [YouTube](https://www.youtube.com/@UnityUIExtensions)

> Ways to get in touch:
>
> - [Gitter Chat](https://gitter.im/Unity-UI-Extensions/Lobby) site for the UI / UI Toolkit Extensions project
> - [GitHub Discussions](https://github.com/Unity-UI-Extensions/com.unity.uitoolkitextensions/discussions), if you have any questions, queries or suggestions

## Installing Unity UI Toolkit Extensions

To install this package, follow the instructions in the Package Manager documentation.

For more details on [Getting Started](https://unity-ui-extensions.github.io/GettingStarted) please checkout the [online documentation here](https://unity-ui-extensions.github.io/).

## Using Unity UI Toolkit Extensions

The UI Toolkit Extensions project provides many automated functions to add the various controls contained within the project commonly accessed via "***GameObject -> UI -> Extensions -> 'Control'***" from the editor menu.  This will add the UI object and all the necessary components to make that control work in the scene in a default state.

Some of the features are also available through the GameObject "Add Component" menu in the inspector.

For a full list of the controls and how they are used, please see the [online documentation](https://unity-ui-extensions.github.io/Controls.html) for the project.

## Control References

### Navigation & Layout

- **ScrollSnap** — Page-based snap scroller with manual/swipe modes, validation gating, and restricted-movement events.
- **QuadrantStepper** — Segmented sliding-overlay step selector for tab bars, mode switchers, and category filters.
- **CollapsibleSection** — Accordion panel with animated max-height expand/collapse, ideal for FAQs and settings groups.
- **PageDotIndicator** — Row of pagination dots; all dots up to and including the current page are highlighted.
- **StepProgressBar** — Horizontal gradient fill bar driven by step counts.
- **ScreenHeader** — Configurable top app-bar with title, notch spacer, and up to four edge action buttons.
- **ElasticListView** — Vertical list with iOS-style elastic overscroll and an optional swipe-up "load more" trigger.
- **DropDownMenuControl** — Anchored overlay action menu with large tappable rows and backdrop-tap dismiss.

### Inputs & Forms

- **PillInputField** — Mobile-aware labeled text input with password mode, multiline support, and validation events.
- **RoundedInputField** — Rounded input field with custom placeholder rendering.
- **PillSelector** — Read-only tap-to-open selector row with chevron icon.
- **PillButton** — Pill-shaped gradient CTA button with flash feedback animation.
- **IconLabelButton** — Row button with a 24 × 24 icon and label, ideal for menu items and list actions.
- **DropDownControl** — Custom dropdown selector with a scrollable list of selectable entries.
- **SocialLinkContainer** — Editable list of platform-labelled social-link fields with add/remove and a platform picker.

### Media & Images

- **CircularImageButton** — Circular tappable image with no-image overlay, ideal for avatars and profile photos.
- **GrayscaleImage** — Immediate-mode image renderer with a toggleable greyscale shader effect.
- **LoadingIcon** — Rotating spinner with configurable speed and optional interaction blocking.
- **ImageCropOverlayControl** — Interactive overlay for framing and cropping an image.

### Toggles & Selection

- **ToggleButton** — Binary image toggle that fires an event on every press.
- **ColorToggleButton** — Tint-colored toggle with ripple animation and selected overlay (extends `ToggleButton`).
- **ColorToggleGroup** — Single-selection group of `ColorToggleButton` items with tap and drag-to-select support.

### Feedback & Utility

- **ToastSwipeDismissManipulator** — Pointer manipulator that adds swipe-to-dismiss gesture handling to any element.
- **ComingSoonMessage** — Centered placeholder panel for in-progress features.
- **NotificationBadge** — Small rounded unread-count badge that auto-hides at zero and clamps to "99+".

### Utilities

- **UIToolkitExtensions** — Static helper for creating, parenting, and wiring VisualElements from code.
- **VisualElementShakeUtility** — Horizontal shake animation for validation and attention feedback.
- **ProceduralTextureUtility** — Generates procedural textures (e.g. rounded rects and gradients) for control styling.

## Examples

Ready-to-run sample scenes demonstrating controls in realistic combinations, included as the **UI Toolkit Extensions Samples** package sample:

- **ScrollSnap + PageDotIndicator** — Horizontal paging with dot indicator and a ComingSoonMessage page.
- **Registration Form** — Full form using PillInputField, RoundedInputField, PillButton, PillSelector, and shake validation.
- **Step Wizard** — Multi-step flow using QuadrantStepper and StepProgressBar.
- **Content Explorer** — LoadingIcon reveal with CollapsibleSection and IconLabelButton items.
- **Profile Editor** — CircularImageButton, GrayscaleImage, ToggleButton, and ColorToggleGroup.
- **Toast Notifications** — Swipe-to-dismiss toast stack using ToastSwipeDismissManipulator.
- **Dropdown Phone Entry** — Phone-number entry pairing a country-code DropDownControl with PillInputFields.
- **Image Crop Overlay** — Pan / pinch-zoom crop flow using ImageCropOverlayControl and CircularImageButton.
- **Notification List** — Elastic notification feed using ElasticListView, NotificationBadge, and PillButton.
- **Screen Header** — Top app-bar demo wiring ScreenHeader's title and action events.
- **Scroll Snap (Split Views)** — The same ScrollSnap built three ways — C#, UXML, and a split layout.
- **Social Links** — Editable social-links section using SocialLinkContainer and PillButton.

## Latest documentation

The lists above are a snapshot for offline reference. For the most accurate and up-to-date documentation — including full API references, usage guides, and code examples for every control and sample — always refer to the **[Unity UI Extensions website](https://unity-ui-extensions.github.io/)**:

- **[UI Toolkit Controls](https://unity-ui-extensions.github.io/uitoolkit/#controls)** — searchable control reference.
- **[Example scenes](https://unity-ui-extensions.github.io/uitoolkit/examples/)** — walkthroughs of every sample.

## Technical details

## Requirements

This version of the Unity UI Toolkit Extensions is compatible with the following versions of the Unity Editor:

- 6000 and above - the recommended path is to use the Unity Package Manager to get access to the package.  Full details for installing via UPM can be [found here](https://unity-ui-extensions.github.io/UPMInstallation.html).

> [!NOTE]
> The package comes with some default `svg` assets for use in the controls and demonstration scenes which requires the `com.unity.vectorgraphics` installed to make use of them
>
> Package Manager -> Install Package By Name -> `com.unity.vectorgraphics`
>
> However, this is completely optional, but highly recommended when working with images with the UI Toolkit.

## [Release Notes](#release-notes)

Coming soon.

## Document revision history

|Date|Details|
|-|-|
|June 16th, 2026|V1.0.0-preview.1 created, project creation|
|July 20th, 2026|V1.0.0-preview.2 created, controls overhaul|
