# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/).

## Release 1.0.0 - Launch - 2026/06

The first release of **Unity UI Toolkit Extensions** — a brand-new companion to the long-running uGUI Extensions library, built from the ground up for Unity 6 and the UI Toolkit (UITK) system.

> **Two packages. One ecosystem.** These notes cover the **UI Toolkit** package (`com.unity.uitoolkitextensions`). It launches alongside the V3 relaunch of its sibling under the shared 3.0 banner: [UI Extensions (uGUI)](https://github.com/Unity-UI-Extensions/com.unity.uiextensions).

### Highlights

- **Built for Unity 6 + UI Toolkit** — a native UITK control library targeting Unity `6000.0` and above, distributed via the Unity Package Manager / OpenUPM.
- **25 controls out of the box** — covering navigation, layout, forms, media, toggles, and feedback, all using the `Unity.UI.Extensions` namespace.
- **Reusable utilities** — helpers for code-first VisualElement construction, attention animations, and procedural textures.
- **12 ready-to-run sample scenes** — included as a UPM sample, demonstrating the controls in realistic combinations.

### Added

The initial release contains 25 controls and a handful of extensions plus utilities to help making UI Toolkit UX far easier.

#### Controls

##### Navigation & Layout

- **ScrollSnap** — Page-based snap scroller with manual/swipe modes, validation gating, and restricted-movement events.
- **QuadrantStepper** — Segmented sliding-overlay step selector for tab bars, mode switchers, and category filters.
- **CollapsibleSection** — Accordion panel with animated max-height expand/collapse, ideal for FAQs and settings groups.
- **PageDotIndicator** — Row of pagination dots; all dots up to and including the current page are highlighted.
- **StepProgressBar** — Horizontal gradient fill bar driven by step counts.
- **ScreenHeader** — Configurable top app-bar with title, notch spacer, and up to four edge action buttons (events only, no app state).
- **ElasticListView** — Vertical list with iOS-style elastic overscroll and an optional swipe-up "load more" trigger.
- **DropDownMenuControl** — Anchored overlay action menu with large tappable rows and backdrop-tap dismiss (distinct from the value-picker `DropDownControl`).

##### Inputs & Forms

- **PillInputField** — Mobile-aware labeled text input with password mode, multiline support, and validation events.
- **RoundedInputField** — Rounded input field with custom placeholder rendering.
- **PillSelector** — Read-only tap-to-open selector row with chevron icon.
- **PillButton** — Pill-shaped gradient CTA button with flash feedback animation.
- **IconLabelButton** — Row button with a 24 × 24 icon and label, ideal for menu items and list actions.
- **DropDownControl** — Custom dropdown selector with a scrollable list of selectable entries.
- **SocialLinkContainer** — Editable list of platform-labelled social-link fields with add/remove and a platform picker.

##### Media & Images

- **CircularImageButton** — Circular tappable image with no-image overlay, ideal for avatars and profile photos.
- **GrayscaleImage** — Immediate-mode image renderer with a toggleable greyscale shader effect.
- **LoadingIcon** — Rotating spinner with configurable speed and optional interaction blocking.
- **ImageCropOverlayControl** — Interactive overlay for framing and cropping an image.

##### Toggles & Selection

- **ToggleButton** — Binary image toggle that fires an event on every press.
- **ColorToggleButton** — Tint-colored toggle with ripple animation and selected overlay (extends `ToggleButton`).
- **ColorToggleGroup** — Single-selection group of `ColorToggleButton` items with tap and drag-to-select support.

##### Feedback & Utility

- **ToastSwipeDismissManipulator** — Pointer manipulator that adds swipe-to-dismiss gesture handling to any element.
- **ComingSoonMessage** — Centered placeholder panel for in-progress features.
- **NotificationBadge** — Small rounded unread-count badge that auto-hides at zero and clamps to "99+".

#### Utilities

- **UIToolkitExtensions** — Static helper for creating, parenting, and wiring VisualElements from code.
- **VisualElementShakeUtility** — Horizontal shake animation for validation and attention feedback.
- **ProceduralTextureUtility** — Generates procedural textures (e.g. rounded rects and gradients) for control styling.

#### Examples

Included as the **UI Toolkit Extensions Samples** package sample:

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

### Contributors

Huge thanks to everyone who helped bring the inaugural UI Toolkit Extensions release together:
[@SimonDarksideJ](https://github.com/SimonDarksideJ), and the wider Unity UI Extensions community.
