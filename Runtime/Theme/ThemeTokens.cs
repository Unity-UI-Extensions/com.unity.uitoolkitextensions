/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The ground a component is drawn on. It selects the surface tokens (<see cref="ThemeTokens.Surface"/>) that the component and its children read.
    /// </summary>
    public enum ThemeSurface
    {
        Inherit,

        /// <summary>
        /// Dark translucent panels over the camera (the default).
        /// </summary>
        Deep,

        /// <summary>
        /// White sheets and pages.
        /// </summary>
        Light,
    }

    /// <summary>
    /// The USS variable contract every UI Toolkit Extensions component reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default values live in <c>Runtime/Styles/ThemeTokens.uss</c>, declared on <c>:root</c>.
    /// An application restyles every component by redefining these variables on <c>:root</c> in its
    /// own sheet, imported after the token sheet in the panel theme:
    /// </para>
    /// <code>
    /// @import url("unity-theme://default");
    /// @import url("/Packages/com.unity.uitoolkitextensions/Runtime/Styles/ThemeTokens.uss");
    /// @import url("/Packages/com.unity.uitoolkitextensions/Runtime/Styles/UIToolkitExtensionsControlStyles.uss");
    /// @import url("/Assets/UI/ApplicationTheme.uss");
    /// </code>
    /// <para>
    /// Components never declare colours, radii, spacing, type sizes or durations of their own.
    /// They read these variables, mostly through the surface layer, so the same component
    /// reads correctly on deep and light grounds.
    /// </para>
    /// <para>
    /// Modifier classes go on any ancestor, normally the panel root. <c>reduce-motion</c> sets every motion token to 0 ms.
    /// <c>text-scale--115</c> and <c>text-scale--130</c> scale the type tokens by 1.15 and 1.3.
    /// </para>
    /// </remarks>
    public static class ThemeTokens
    {
        public const string StyleSheetAssetPath = "Packages/com.unity.uitoolkitextensions/Runtime/Styles/ThemeTokens.uss";

        public const string DeepSurfaceClass = "surface--deep";
        public const string LightSurfaceClass = "surface--light";
        public const string ReduceMotionClass = "reduce-motion";
        public const string HighContrastClass = "theme--high-contrast";
        public const string TextScale115Class = "text-scale--115";
        public const string TextScale130Class = "text-scale--130";

        /// <summary>
        /// Declared by the token sheet; lets code detect whether the tokens are loaded.
        /// </summary>
        public const string VersionToken = "--theme-tokens";

        /// <summary>
        /// Added by <see cref="UseThemeTokens"/> to every component that reads the tokens.
        /// </summary>
        public const string ThemedClass = "uitk-themed";

        private static readonly CustomStyleProperty<int> ThemedProperty = new("--uitk-themed");
        private static readonly CustomStyleProperty<int> TokensProperty = new("--uitk-tokens");
        private static bool hasWarnedMissingTokens;

        /// <summary>
        /// Core theme colours and the few semantic colours components need.
        /// </summary>
        public static class Colors
        {
            public const string Background = "--c-bg";
            public const string Background2 = "--c-bg-2";
            public const string Navy = "--c-navy";
            public const string Navy2 = "--c-navy-2";
            public const string Surface = "--c-surface";
            public const string Surface2 = "--c-surface-2";
            public const string Panel = "--c-panel";
            public const string PanelStrong = "--c-panel-strong";
            public const string Divider = "--c-divider";
            public const string DividerStrong = "--c-divider-strong";
            public const string Primary = "--c-primary";
            public const string Primary2 = "--c-primary-2";
            public const string Accent = "--c-accent";
            public const string Foreground = "--c-fg";
            public const string ForegroundSoft = "--c-fg-soft";
            public const string ForegroundMuted = "--c-fg-muted";
            public const string Warn = "--c-warn";
            public const string Success = "--c-success";
            public const string Danger = "--c-danger";
            public const string Page = "--c-page";
            public const string PageForeground = "--c-page-fg";
            public const string PageMuted = "--c-page-muted";
            public const string PageCard = "--c-page-card";
            public const string PageDivider = "--c-page-divider";
            public const string PageDividerStrong = "--c-page-divider-strong";
            public const string OnPrimary = "--c-on-primary";
            public const string OnDanger = "--c-on-danger";
            public const string OnWarn = "--c-on-warn";

            /// <summary>
            /// The recording red, for the REC chip and the recording shutter, and its foreground.
            /// </summary>
            public const string Record = "--c-record";
            public const string OnRecord = "--c-on-record";
            public const string Scrim = "--c-scrim";

            /// <summary>
            /// Start and end stops of <c>--grad-brand</c>; USS has no gradients, so components build the texture from these.
            /// </summary>
            public const string GradientBrandStart = "--grad-brand-start";
            public const string GradientBrandEnd = "--grad-brand-end";

            /// <summary>
            /// Start and end stops of <c>--grad-danger</c>, the filled destructive action.
            /// </summary>
            public const string GradientDangerStart = "--grad-danger-start";
            public const string GradientDangerEnd = "--grad-danger-end";
        }

        /// <summary>
        /// The ground-relative layer. Components read these rather than the core theme colours, so a
        /// <c>surface--light</c> ancestor turns a deep component into a light one.
        /// </summary>
        public static class Surface
        {
            public const string Background = "--surface-bg";
            public const string Raised = "--surface-raised";
            public const string Foreground = "--surface-fg";
            public const string ForegroundSoft = "--surface-fg-soft";
            public const string Muted = "--surface-muted";
            public const string Border = "--surface-border";
            public const string BorderStrong = "--surface-border-strong";
            public const string Accent = "--surface-accent";
            public const string Pressed = "--surface-pressed";
            public const string Skeleton = "--surface-skeleton";

            /// <summary>
            /// Dialogs and other cards that must read clearly over any page.
            /// </summary>
            public const string Overlay = "--surface-overlay";
            public const string Input = "--surface-input";
            public const string InputBorder = "--surface-input-border";

            /// <summary>
            /// Icon tiles, such as the leading icon of a settings row.
            /// </summary>
            public const string Tile = "--surface-tile";

            /// <summary>
            /// A tinted row that stands out from the panel, such as a match or an achievement.
            /// </summary>
            public const string Highlight = "--surface-highlight";

            /// <summary>
            /// The track of a switch that is off.
            /// </summary>
            public const string SwitchOff = "--surface-switch-off";

            /// <summary>
            /// A subtle ring round an image in a disc, such as badge art: 18% white over a tile, or the border on light.
            /// </summary>
            public const string Ring = "--surface-ring";
            public const string HighlightBorder = "--surface-highlight-border";
        }

        public static class Fonts
        {
            /// <summary>
            /// Titles. Not defined by the package; the application sets it to a FontAsset.
            /// </summary>
            public const string Display = "--font-display";

            /// <summary>
            /// Everything else. Not defined by the package; the application sets it to a FontAsset.
            /// </summary>
            public const string Body = "--font-body";

            /// <summary>
            /// Medium weight; defaults to <see cref="Body"/>.
            /// </summary>
            public const string BodyMedium = "--font-body-medium";

            /// <summary>
            /// SemiBold weight; defaults to <see cref="Body"/>.
            /// </summary>
            public const string BodySemibold = "--font-body-semibold";
        }

        public static class Text
        {
            public const string Scale = "--text-scale";
            /// <summary>
            /// Eyebrows and gesture labels, set in capitals with letter spacing.
            /// </summary>
            public const string Micro = "--text-micro";
            public const string Caption = "--text-caption";
            public const string LabelSmall = "--text-label-sm";
            public const string Label = "--text-label";
            public const string Body = "--text-body";
            public const string Subtitle = "--text-subtitle";
            public const string Title = "--text-title";
            public const string Display = "--text-display";
        }

        public static class Radius
        {
            public const string Small = "--radius-sm";
            public const string Input = "--radius-input";
            public const string Pill = "--radius-pill";
            public const string Card = "--radius-card";
            public const string Sheet = "--radius-sheet";
            public const string Dialog = "--radius-dialog";
        }

        public static class Space
        {
            public const string S1 = "--space-1";
            public const string S2 = "--space-2";
            public const string S3 = "--space-3";
            public const string S4 = "--space-4";
            public const string S5 = "--space-5";
            public const string S6 = "--space-6";
        }

        public static class Motion
        {
            public const string Fast = "--motion-fast";
            public const string Base = "--motion-base";
            public const string Slow = "--motion-slow";

            /// <summary>
            /// Default values in milliseconds, for code that cannot read USS.
            /// </summary>
            public const int FastMs = 120;
            public const int BaseMs = 240;
            public const int SlowMs = 400;
        }

        private static StyleSheet styleSheet;

        /// <summary>
        /// The default token sheet. Resolved via the AssetDatabase, so it is only available in the
        /// Editor (including Play Mode); player builds reference it from the panel theme.
        /// </summary>
        public static StyleSheet StyleSheet
        {
            get
            {
#if UNITY_EDITOR
                if (styleSheet == null)
                {
                    styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetAssetPath);
                }
#endif
                return styleSheet;
            }
        }

        /// <summary>
        /// Attaches the default token sheet to a root element. Use on a document root whose panel
        /// theme does not import the token sheet; never on a component, or its <c>:root</c>
        /// defaults would shadow the application's overrides for that component.
        /// </summary>
        public static void ApplyThemeTokens(this VisualElement root) => root.AttachOnce(StyleSheet);

        /// <summary>
        /// Marks a component as reading the token contract. In the Editor it logs one warning per
        /// session when the panel theme does not import the token sheet, because the component would
        /// then render unstyled in the Editor and in player builds alike.
        /// </summary>
        public static void UseThemeTokens(this VisualElement element)
        {
            element.AddToClassList(ThemedClass);
#if UNITY_EDITOR
            element.RegisterCallback<CustomStyleResolvedEvent>(CheckTokensPresent);
#endif
        }

        public static bool HasThemeTokens(ICustomStyle customStyle)
        {
            return customStyle.TryGetValue(TokensProperty, out _);
        }

        private static void CheckTokensPresent(CustomStyleResolvedEvent evt)
        {
            if (evt.currentTarget is not VisualElement element)
            {
                return;
            }

            element.UnregisterCallback<CustomStyleResolvedEvent>(CheckTokensPresent);

            if (hasWarnedMissingTokens || !evt.customStyle.TryGetValue(ThemedProperty, out _) || HasThemeTokens(evt.customStyle))
            {
                return;
            }

            hasWarnedMissingTokens = true;
            Debug.LogWarning($"[UI Toolkit Extensions] {element.GetType().Name} reads ThemeTokens, but the panel theme does not import " +
                             $"\"/{StyleSheetAssetPath}\". Add it to the theme (.tss) before the control styles, or call ApplyThemeTokens() on the document root.");
        }

        public static void SetSurface(this VisualElement element, ThemeSurface surface)
        {
            element.EnableInClassList(DeepSurfaceClass, surface == ThemeSurface.Deep);
            element.EnableInClassList(LightSurfaceClass, surface == ThemeSurface.Light);
        }

        /// <summary>
        /// Switches every motion token in the subtree to 0 ms.
        /// </summary>
        public static void SetReduceMotion(this VisualElement root, bool reduceMotion)
        {
            root.EnableInClassList(ReduceMotionClass, reduceMotion);
        }

        /// <summary>
        /// Applies a text scale to the subtree. USS cannot multiply, so the scale snaps to the
        /// nearest supported step: 1, 1.15 or 1.3.
        /// </summary>
        /// <returns>The step that was applied.</returns>
        public static float SetTextScale(this VisualElement root, float scale)
        {
            var step = scale >= 1.225f ? 1.3f : scale >= 1.075f ? 1.15f : 1f;
            root.EnableInClassList(TextScale115Class, Mathf.Approximately(step, 1.15f));
            root.EnableInClassList(TextScale130Class, Mathf.Approximately(step, 1.3f));
            return step;
        }

        public static void SetHighContrast(this VisualElement root, bool highContrast)
        {
            root.EnableInClassList(HighContrastClass, highContrast);
        }

        public static bool IsReduceMotion(this VisualElement element)
        {
            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.ClassListContains(ReduceMotionClass))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
