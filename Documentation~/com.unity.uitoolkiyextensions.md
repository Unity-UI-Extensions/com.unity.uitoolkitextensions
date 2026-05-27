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

- [ScrollSnap](ScrollSnap.md) — Page-based snap scroller with manual/swipe modes, validation gating, and restricted-movement events.
- [QuadrantStepper](Controls/QuadrantStepper.md) — Segmented sliding-overlay step selector. Ideal for tab bars, mode switchers, and category filters.
- [CollapsibleSection](Controls/CollapsibleSection.md) — Accordion panel with animated max-height expand/collapse. Ideal for FAQs and settings groups.
- [PageDotIndicator](Controls/PageDotIndicator.md) — Row of pagination dots; all dots up to and including the current page are highlighted.
- [StepProgressBar](Controls/StepProgressBar.md) — Horizontal gradient fill bar driven by step counts.

### Inputs & Forms

- [PillInputField](Controls/PillInputField.md) — Mobile-aware labeled text input with password mode, multiline support, and validation events.
- [RoundedInputField](Controls/RoundedInputField.md) — Rounded input field with custom placeholder rendering.
- [PillSelector](Controls/PillSelector.md) — Read-only tap-to-open selector row with chevron icon.
- [PillButton](Controls/PillButton.md) — Pill-shaped gradient CTA button with flash feedback animation.
- [IconLabelButton](Controls/IconLabelButton.md) — Row button with a 24 × 24 icon and label. Ideal for menu items and list actions.

### Media & Images

- [CircularImageButton](Controls/CircularImageButton.md) — Circular tappable image with no-image overlay. Ideal for avatars and profile photos.
- [GrayscaleImage](Controls/GrayscaleImage.md) — Immediate-mode image renderer with a toggleable greyscale shader effect.
- [LoadingIcon](Controls/LoadingIcon.md) — Rotating spinner with configurable speed and optional interaction blocking.

### Toggles & Selection

- [ToggleButton](Controls/ToggleButton.md) — Binary image toggle that fires an event on every press.
- [ColorToggleButton](Controls/ColorToggleButton.md) — Tint-colored toggle with ripple animation and selected overlay. Extends `ToggleButton`.
- [ColorToggleGroup](Controls/ColorToggleGroup.md) — Single-selection group of `ColorToggleButton` items with tap and drag-to-select support.

### Feedback & Utility

- [ToastSwipeDismissManipulator](Controls/ToastSwipeDismissManipulator.md) — Pointer manipulator that adds swipe-to-dismiss gesture handling to any element.
- [ComingSoonMessage](Controls/ComingSoonMessage.md) — Centered placeholder panel for in-progress features.

### Utilities

- [VisualElementShakeUtility](VisualElementShakeUtility.md) — Horizontal shake animation for validation and attention feedback.
- [UIToolkitExtensions](Utilities/UIToolkitExtensions.md) — Static helper for creating, parenting, and wiring VisualElements from code.

## Examples

Ready-to-run sample scenes demonstrating controls in realistic combinations:

- [ScrollSnap + PageDotIndicator](Examples/ScrollSnapAndDots.md) — Horizontal paging with dot indicator and ComingSoonMessage page.
- [Registration Form](Examples/RegistrationForm.md) — Full form with PillInputField, RoundedInputField, PillButton, PillSelector, and shake validation.
- [Step Wizard](Examples/StepWizard.md) — Multi-step flow using QuadrantStepper and StepProgressBar.
- [Content Explorer](Examples/ContentExplorer.md) — LoadingIcon reveal with CollapsibleSection and IconLabelButton items.
- [Profile Editor](Examples/ProfileEditor.md) — CircularImageButton, GrayscaleImage, ToggleButton, and ColorToggleGroup.
- [Toast Notifications](Examples/ToastNotifications.md) — Swipe-to-dismiss toast stack using ToastSwipeDismissManipulator.

## Technical details

## Requirements

This version of the Unity UI Toolkit Extensions is compatible with the following versions of the Unity Editor:

- 6000 and above - the recommended path is to use the Unity Package Manager to get access to the package.  Full details for installing via UPM can be [found here](https://unity-ui-extensions.github.io/UPMInstallation.html).

## [Release Notes](#release-notes)

Coming soon.

## Document revision history

|Date|Details|
|-|-|
|December 25th, 2025|V1.0.0 created, project creation|
