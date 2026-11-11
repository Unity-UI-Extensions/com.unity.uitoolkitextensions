# Change Log

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/).

## Unreleased

A theming contract and a set of app-shell, browsing and AR guidance components, built so any UI Toolkit app can use them.

### Changed

- ContentGrid places rows at one measured height (`FixedHeight` virtualisation) instead of `DynamicHeight`, so scrolling back towards the start no longer jumps as rows scroll in; tiles size their thumbnails when bound, and a change of items refreshes the list rather than rebuilding it.
- ContentGrid tiles keep the meta line while any item has a `Meta`, blank where an item has none, so all tiles are one height. When no item has one, every tile leaves the line out, as before.
- ModalDialog gains an optional third, link-style action under the main buttons: `DialogOptions.TertiaryText`, the `tertiary-text` UXML attribute, `TertiaryButton`, and `DialogAction.Tertiary` in the result. Null keeps the two-action layout.
- BottomSheet leaves header controls and nested scroll views in charge of their pointers, capturing plain-header and handle drags only after the movement threshold.
- ContentGrid suppresses recycled-row hover/selection backgrounds and ignores the click at the end of a scroll gesture; selection remains per tile.
- SearchField's clear control is focusable and supports keyboard submit, including when hosted in a draggable sheet header.
- Renamed `CaptionBar.Clear()` to `ClearCue()`, `SearchField.Clear()` to `ClearQuery()`, and `StatusChipHost.Clear()` to `ClearStatuses()`. Update custom callers to these names; inherited `VisualElement.Clear()` only removes children and does not perform control-specific cleanup.
- Renamed private backing fields in EntryGate, BottomSheet, SideDrawer and ToastHost to avoid hiding Unity properties. Public properties and UXML attributes are unchanged.

### Added

#### Theming

- **ThemeTokens** - one USS variable contract (`Runtime/Styles/ThemeTokens.uss`) that every new component reads: primary colours, a surface layer that remaps deep and light grounds, type sizes, radii, spacing and motion. An app restyles everything by redefining the variables on `:root` in its own sheet, imported after the token sheet in the panel theme. Root classes switch reduce motion, text scale (1, 1.15, 1.3), high contrast and the surface; `ThemeTokens` has C# helpers for each.
- **GradientFill** - a manipulator that paints a two-stop gradient background from tokens (primary or deep, horizontal or vertical), since USS has no gradients.

#### Components

- **SegmentedControl** - the extended `QuadrantStepper` under a general name, themed from tokens.
- **BottomSheet** - peek, half and full detents, drag with velocity, optional scrim, back-button rule, a docked right-hand column for wide layouts, and a footer pinned to the visible bottom edge. The content area ends at the visible edge, so scrolling content reaches its end at every detent.
- **SideDrawer** - left or right drawer with scrim, edge swipe, drag to close, titled sections and a permanent variant.
- **ModalDialog** - confirm, destructive and short-form dialogs with a header slot, optional text or choice field, focus trap and back handling; `Show(options)` returns a `Task<DialogResult>`.
- **ToastHost** - queued toasts with tones, one action (including undo), a detail line, an icon, a progress bar that updates in place, auto-dismiss or stay until dismissed, tap and swipe to dismiss, anchored top or bottom.
- **StatusChip** and **StatusChipHost** - persistent statuses (info, busy, ok, warn, fail) keyed by id, updated in place and resolved with a final state.
- **OrnamentBar** - a floating action bar that sizes to its items, bottom centre in portrait and on the right edge in landscape, with one primary item.
- **ScrollingTabs** - dynamic categories as tabs or chips, with counts, an optional search tab, an animated indicator and edge fades.
- **ContentGrid** - a virtualised tile grid with a thumbnail aspect, selection modes, a long-press drag-out source, and loading and empty states.
- **SectionList** - data-bound expandable sections with counts and rows, built on `CollapsibleSection` and `SettingsRow`.
- **SearchField** - a search box on `RoundedInputField` with a debounced query, submit, clear and a result count.
- **SettingsRow** - icon, title, subtitle and a trailing chevron, value, switch or custom slot.
- **Switch** - a labelled on/off `BaseField<bool>`.
- **EmptyState** - icon, title, body and one action for empty lists.
- **SkeletonTile** - loading placeholders in tile, row, text and circle shapes that hold still with reduce motion.
- **ProgressRing** - a determinate ring with a centre value and caption. It turns the success colour at 100% unless `ShowComplete` is off, as for a countdown that starts full.
- **StatusList** - structured loading steps (pending, active, done, failed) with an overall bar.
- **CoachingOverlay** - AR tracking cues (look around, find a surface, move slower, too dark) with plain-language text, an optional detail line, icon disc, one or two actions (ghost, then primary) and a progress line, centred or as a card at the top or bottom.
- **WorldAnchoredLabel** - a pill that follows a point in the world, through a camera or from panel coordinates, with tones, a distance, safe-area clamping and a range fade.
- **ToolRail** - icon tools in a pill, as an action toolbar or a mode rail with one active tool; disabled and warning tools; horizontal, vertical or chosen from the parent's shape.
- **GestureHint** - the first-use chip that teaches move, rotate, scale and raise, in touch or mouse wording, dismissed by a tap or the first gesture.
- **EntryGate** - the full-screen gate before an experience or the camera: a deep gradient backdrop, optional logos, a message or hero layout, the requirements Start unlocks, a content slot, primary and secondary actions, a cancel link and consent links.
- **AudioGuidePlayer** - controls for a guide or a video: play, pause and replay, a scrubber, 15 second skips, loop, rate and the transcript line. It holds no media; the owner reports the state and answers the requests. An optional mute button (`ShowMute`, `IsMuted`, `MuteToggled`; hidden by default) shows a speaker or a crossed-out speaker.
- **CaptionBar** - captions with a speaker column, clamped to a number of lines that follows the text scale, cross-fading between cues.
- **PromptChoices** - numbered prompt rows for talking to a character, with used prompts dimmed and number keys.
- **SpeechInputPanel** and **HoldToTalk** - the listening panel (status, level meter, partial and final transcript, match or no match, actions) and the hold-to-talk row that tells a hold from a tap.
- **AvatarPortrait** - a round portrait with an initials fallback at 40, 48 and 56 px.
- **LinearProgress** - a thin bar in the primary-to-accent gradient, or an indeterminate segment that sweeps back and forth.
- **AchievementRow** - a badge disc, a title and an optional progress bar.
- **ShutterButton** - the camera shutter: a white ring around a primary-colour disc.
- **ContextPill** - the pill at the top left of AR screens: a logo, a title and an eyebrow, with a dimmed state for when a dialog is open, and a logo-only state when there is no text.
- **ScanFrame** - corner brackets for the scanner in neutral, success and warning tones, pulsing while searching, centred or hugging a tracked marker.
- **QRCodeView** and **QrEncoder** - a QR code for a string with a quiet zone, and a texture copy to share or save. The encoder is self-contained (numeric, alphanumeric and UTF-8 byte modes, versions 1 to 40, all four error correction levels) and was checked against ZXing.Net.
- **HintImageCard** - a picture of what to face, a line of guidance and an optional proximity strip (FAR, NEAR, FOUND), as a card or embedded in another card.
- **WayfinderCard** - directions to the next stop: an arrow that turns with the bearing, the distance and a detail line, and a check on arrival.
- Icons: `Search`, `Close`, `Check`, `Play`, `Pause`, `Replay`, `Loop`, `Mic` and `ArrowUp`.

#### Tests and samples

- A PlayMode test assembly (`UnityUIToolkitExtensions.Tests`) that mounts each component in a `UIDocument`.
- A sample scene per component, on a shared token-themed sample page.

### Changed

- **QuadrantStepper** - an icon slot, equal or content sizing (the row scrolls when it is wider than the control), disabled segments, keyboard navigation and a settable `SelectedIndex`. The existing API and USS class names are unchanged.
- **CollapsibleSection** - leading and trailing header slots, a subtitle, `ShowChevron`, `Interactive`, `ToggleOnPointerDown` and a token-styled card variant. The default look and pointer-down toggle are unchanged.
- **PillButton** - token-styled `Primary`, `Ghost`, `Warn` and `Danger` (filled from the `--grad-danger` pair) variants, a `Compact` height, and `SetIcon` for a leading icon centred with the label; the default keeps the inner and outer colour gradient.
- **RoundedInputField** - an `Invalid` state with a warning border, and a token-styled `Themed` variant (50 px, 1.5 px border, 12 px radius) for deep and light grounds.
- **ScreenHeader** - a token-styled `Compact` variant (56 px bar, back chevron, optional eyebrow line above the title), `TopInset`, and `SetActionIcon`, which takes any `Background` and leaves the tint to USS.
- **ToggleButton** - `SetIcon(Background)`, for sprites and vector images, tinted by USS.
- The sample theme imports the sample styles with relative paths, so the samples keep their styles at any package version.

### Fixed

- `DropDownControl.meta` and `ImageCropOverlayControl.meta` contained their content twice, which Unity reported on import.
- The Editor assembly is now Editor-only, so player builds no longer try to compile it.
- A custom-coloured `PillButton` lost its gradient after being removed from and re-added to a panel.
- ContentGrid no longer rebuilds its rows every frame when its tiles measure different heights. A padding cell in a short last row, or an item with no `Meta` (common while details load), measured shorter than its neighbours. Each height in turn rebuilt the list, so the rows jumped and no tile received a tap. Every tile now has the same lines, blank where an item has no text, and padding cells never set the row height. The row height comes from the tallest tile, in whole device pixels, and existing rows are resized in place instead of rebuilt. If the height changes a few times without settling, it then only grows, so no feedback between tiles and rows can loop. `TileFor(null)` no longer returns a padding cell.

## Release 1.0.0 - Launch - 2026/09

The first release of **Unity UI Toolkit Extensions** - a brand-new companion to the long-running uGUI Extensions library, built from the ground up for Unity 6 and the UI Toolkit (UITK) system.

> **Two packages. One ecosystem.** These notes cover the **UI Toolkit** package (`com.unity.uitoolkitextensions`). It launches alongside the V3 relaunch of its sibling under the shared 3.0 banner: [UI Extensions (uGUI)](https://github.com/Unity-UI-Extensions/com.unity.uiextensions).

### Highlights

- **Built for Unity 6 + UI Toolkit** - a native UITK control library targeting Unity `6000.0` and above, distributed via the Unity Package Manager / OpenUPM.
- **25 controls out of the box** - covering navigation, layout, forms, media, toggles, and feedback, all using the `Unity.UI.Extensions` namespace.
- **Editor menu and starter templates** - add any control to a scene or document from the `GameObject/UI Toolkit/Extensions` and `Assets/Create/UI Toolkit/Extensions` menus, with demo content and styling already wired up.
- **Reusable utilities** - helpers for code-first VisualElement construction, attention animations, and procedural textures.
- **12 ready-to-run sample scenes** - included as a UPM sample, demonstrating the controls in realistic combinations.

### Added

The initial release contains 25 controls and a handful of extensions plus utilities to help making UI Toolkit UX far easier.

#### Editor integration

- **Editor menu integration** - a new `GameObject/UI Toolkit/Extensions` menu adds any of the 22 UXML-placeable controls to the scene in basic form with demo content, automatically creating a `UIDocument` and `PanelSettings` (with the default runtime theme) if the scene has none. The UI Toolkit counterpart of the uGUI Extensions `GameObject/UI/Extensions` menu.
- **Starter templates** - per-control starter UXML templates with demo content, also available from `Assets/Create/UI Toolkit/Extensions` for dropping into existing documents. Instantiated copies are placed under `Assets/UI Toolkit Extensions` so they remain freely editable.
- **Self-applying control styles** - while authoring (UI Builder) and in Play Mode, controls attach the shared extensions stylesheet automatically on construction, so they render correctly without manually referencing the stylesheet. Overlay controls (`DropDownControl`, `DropDownMenuControl`, `ImageCropOverlayControl`) also style their root-attached panels directly. In player builds, styling comes from the explicit stylesheet references in the starter templates and from the theme generated by the editor menu (which imports the shared stylesheet panel-wide).

#### Controls

##### Navigation & Layout

- **ScrollSnap** - Page-based snap scroller with manual/swipe modes, validation gating, and restricted-movement events.
- **QuadrantStepper** - Segmented sliding-overlay step selector for tab bars, mode switchers, and category filters.
- **CollapsibleSection** - Accordion panel with animated max-height expand/collapse, ideal for FAQs and settings groups.
- **PageDotIndicator** - Row of pagination dots; all dots up to and including the current page are highlighted.
- **StepProgressBar** - Horizontal gradient fill bar driven by step counts.
- **ScreenHeader** - Configurable top app-bar with title, notch spacer, and up to four edge action buttons (events only, no app state).
- **ElasticListView** - Vertical list with iOS-style elastic overscroll and an optional swipe-up "load more" trigger. Items can be declared as child elements in UXML or added with plain `Add()`, as well as from code with `AddItem()`/`SetItems()`.
- **DropDownMenuControl** - Anchored overlay action menu with large tappable rows and backdrop-tap dismiss (distinct from the value-picker `DropDownControl`).

##### Inputs & Forms

- **PillInputField** - Mobile-aware labeled text input with password mode, multiline support, and validation events.
- **RoundedInputField** - Rounded input field with custom placeholder rendering.
- **PillSelector** - Read-only tap-to-open selector row with chevron icon.
- **PillButton** - Pill-shaped gradient CTA button with flash feedback animation.
- **IconLabelButton** - Row button with a 24 × 24 icon and label, ideal for menu items and list actions.
- **DropDownControl** - Custom dropdown selector with a scrollable list of selectable entries.
- **SocialLinkContainer** - Editable list of platform-labelled social-link fields with add/remove and a platform picker.

##### Media & Images

- **CircularImageButton** - Circular tappable image with no-image overlay, ideal for avatars and profile photos.
- **GrayscaleImage** - Immediate-mode image renderer with a toggleable greyscale shader effect.
- **LoadingIcon** - Rotating spinner with configurable speed and optional interaction blocking.
- **ImageCropOverlayControl** - Interactive overlay for framing and cropping an image.

##### Toggles & Selection

- **ToggleButton** - Binary image toggle that fires an event on every press.
- **ColorToggleButton** - Tint-colored toggle with ripple animation and selected overlay (extends `ToggleButton`).
- **ColorToggleGroup** - Single-selection group of `ColorToggleButton` items with tap and drag-to-select support.

##### Feedback & Utility

- **ToastSwipeDismissManipulator** - Pointer manipulator that adds swipe-to-dismiss gesture handling to any element.
- **ComingSoonMessage** - Centered placeholder panel for in-progress features.
- **NotificationBadge** - Small rounded unread-count badge that auto-hides at zero and clamps to "99+".

#### Utilities

- **UIToolkitExtensions** - Static helper for creating, parenting, and wiring VisualElements from code.
- **VisualElementShakeUtility** - Horizontal shake animation for validation and attention feedback.
- **ProceduralTextureUtility** - Generates procedural textures (e.g. rounded rects and gradients) for control styling.

#### Examples

Included as the **UI Toolkit Extensions Samples** package sample:

- **ScrollSnap + PageDotIndicator** - Horizontal paging with dot indicator and a ComingSoonMessage page.
- **Registration Form** - Full form using PillInputField, RoundedInputField, PillButton, PillSelector, and shake validation.
- **Step Wizard** - Multi-step flow using QuadrantStepper and StepProgressBar.
- **Content Explorer** - LoadingIcon reveal with CollapsibleSection and IconLabelButton items.
- **Profile Editor** - CircularImageButton, GrayscaleImage, ToggleButton, and ColorToggleGroup.
- **Toast Notifications** - Swipe-to-dismiss toast stack using ToastSwipeDismissManipulator.
- **Dropdown Phone Entry** - Phone-number entry pairing a country-code DropDownControl with PillInputFields.
- **Image Crop Overlay** - Pan / pinch-zoom crop flow using ImageCropOverlayControl and CircularImageButton.
- **Notification List** - Elastic notification feed using ElasticListView, NotificationBadge, and PillButton.
- **Screen Header** - Top app-bar demo wiring ScreenHeader's title and action events.
- **Scroll Snap (Split Views)** - The same ScrollSnap built three ways - C#, UXML, and a split layout.
- **Social Links** - Editable social-links section using SocialLinkContainer and PillButton.

### Changed

Relative to the `1.0-preview.1` pre-release:

- Package dependency corrected from the deprecated `com.unity.ui` preview package to the built-in `com.unity.modules.uielements` module.
- Removed leftover uGUI assembly references from the Runtime and Editor assembly definitions, and the placeholder `DumbScript` from the Editor assembly.

### Contributors

Huge thanks to everyone who helped bring the inaugural UI Toolkit Extensions release together:
[@SimonDarksideJ](https://github.com/SimonDarksideJ), and the wider Unity UI Extensions community.
