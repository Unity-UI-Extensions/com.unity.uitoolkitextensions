/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A manipulation gesture a <see cref="GestureHint"/> teaches.
    /// </summary>
    public enum Gesture
    {
        Move,
        Rotate,
        Scale,
        Raise,
    }

    public enum GestureHintPlatform
    {
        /// <summary>
        /// Finger counts: "1 finger move", "2 rotate", "pinch scale", "3 raise".
        /// </summary>
        Touch,

        /// <summary>
        /// Mouse wording: "drag move", "right drag rotate", "scroll scale", "shift drag raise".
        /// </summary>
        Mouse,
    }

    /// <summary>
    /// A transient chip that teaches the manipulation gestures the first time something is selected. It dismisses on
    /// a tap or on the first successful gesture the owner reports, and can offer the inspector as the non-gesture route.
    /// </summary>
    /// <remarks>
    /// <para>Manipulation itself is gesture-only; this chip only teaches it. Report gestures with
    /// <see cref="NotifyGesture"/>. With <see cref="ShowOnce"/>, <see cref="Show"/> does nothing after the first
    /// dismissal; persist <see cref="HasBeenDismissed"/> yourself to carry that across sessions.</para>
    /// <para>Props: <c>platform</c>, <c>show-once</c>, <c>inspector-text</c> (empty hides the inspector link).
    /// Methods: <see cref="Show"/>, <see cref="Dismiss"/>, <see cref="NotifyGesture"/>, <see cref="SetHintText"/>.
    /// Events: <see cref="Dismissed"/>, <see cref="OpenInspector"/>.</para>
    /// <para>USS classes: <c>gestureHint</c> (root), <c>gestureHint__item</c>, <c>gestureHint__prefix</c>,
    /// <c>gestureHint__verb</c>, <c>gestureHint__inspector</c>. Modifiers: <c>gestureHint--shown</c>,
    /// <c>gestureHint--mouse</c>.</para>
    /// <para>Tokens read: <c>--c-panel</c>, <c>--c-divider</c>, <c>--c-fg</c>, <c>--c-fg-muted</c>, <c>--c-accent</c>,
    /// <c>--radius-pill</c>, <c>--space-2..4</c>, <c>--text-micro</c>, <c>--text-caption</c>, <c>--font-body-medium</c>, <c>--motion-base</c>.
    /// The chip sits over the camera, so it uses the deep palette directly.</para>
    /// </remarks>
    [UxmlElement]
    public partial class GestureHint : VisualElement
    {
        public const string RootClass = "gestureHint";
        public const string ItemClass = "gestureHint__item";
        public const string PrefixClass = "gestureHint__prefix";
        public const string VerbClass = "gestureHint__verb";
        public const string InspectorClass = "gestureHint__inspector";
        public const string ShownClass = "gestureHint--shown";
        public const string MouseClass = "gestureHint--mouse";

        private static readonly Dictionary<Gesture, (string prefix, string verb)> TouchDefaults = new()
        {
            [Gesture.Move] = ("1 finger", "move"),
            [Gesture.Rotate] = ("2", "rotate"),
            [Gesture.Scale] = ("pinch", "scale"),
            [Gesture.Raise] = ("3", "raise"),
        };

        private static readonly Dictionary<Gesture, (string prefix, string verb)> MouseDefaults = new()
        {
            [Gesture.Move] = ("drag", "move"),
            [Gesture.Rotate] = ("right drag", "rotate"),
            [Gesture.Scale] = ("scroll", "scale"),
            [Gesture.Raise] = ("shift drag", "raise"),
        };

        private readonly Dictionary<Gesture, (Label prefix, Label verb)> items = new();
        private readonly Dictionary<Gesture, (string prefix, string verb)> overrides = new();
        private readonly Label inspector;
        private GestureHintPlatform platform;
        private bool showOnce = true;
        private bool isShown;
        private bool hasBeenDismissed;

        /// <summary>
        /// Raised when the chip hides after a tap or a gesture (not after <see cref="Dismiss"/> from code).
        /// </summary>
        public event Action Dismissed;

        /// <summary>
        /// Raised when the inspector link is tapped.
        /// </summary>
        public event Action OpenInspector;

        public bool IsShown => isShown;
        public bool HasBeenDismissed => hasBeenDismissed;

        [UxmlAttribute("platform")]
        public GestureHintPlatform Platform
        {
            get => platform;
            set
            {
                platform = value;
                EnableInClassList(MouseClass, platform == GestureHintPlatform.Mouse);
                ApplyTexts();
            }
        }

        [UxmlAttribute("show-once")]
        public bool ShowOnce
        {
            get => showOnce;
            set => showOnce = value;
        }

        [UxmlAttribute("inspector-text")]
        public string InspectorText
        {
            get => inspector.text;
            set
            {
                inspector.text = value ?? string.Empty;
                inspector.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        public GestureHint()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.MakePill();

            foreach (Gesture gesture in Enum.GetValues(typeof(Gesture)))
            {
                var item = UIToolkitExtensions.CreateVisualElement(this, ItemClass);
                item.pickingMode = PickingMode.Ignore;
                var prefix = UIToolkitExtensions.CreateVisualElement<Label>(item, PrefixClass);
                prefix.pickingMode = PickingMode.Ignore;
                var verb = UIToolkitExtensions.CreateVisualElement<Label>(item, VerbClass);
                verb.pickingMode = PickingMode.Ignore;
                items[gesture] = (prefix, verb);
            }

            inspector = UIToolkitExtensions.CreateVisualElement<Label>(this, InspectorClass);
            inspector.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                OpenInspector?.Invoke();
            });

            RegisterCallback<ClickEvent>(_ => DismissByUser());

            InspectorText = null;
            Platform = GestureHintPlatform.Touch;
            pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Replaces the wording for one gesture, for localisation. Null restores the default.
        /// </summary>
        public void SetHintText(Gesture gesture, string prefix, string verb)
        {
            if (prefix == null && verb == null)
            {
                overrides.Remove(gesture);
            }
            else
            {
                overrides[gesture] = (prefix ?? string.Empty, verb ?? string.Empty);
            }

            ApplyTexts();
        }

        /// <summary>
        /// Shows the chip, unless <see cref="ShowOnce"/> is on and it has been dismissed before.
        /// </summary>
        /// <returns>Whether it is now shown.</returns>
        public bool Show()
        {
            if (showOnce && hasBeenDismissed)
            {
                return false;
            }

            isShown = true;
            AddToClassList(ShownClass);
            pickingMode = PickingMode.Position;
            return true;
        }

        /// <summary>
        /// Hides the chip from code without counting as the user's dismissal.
        /// </summary>
        public void Dismiss()
        {
            isShown = false;
            RemoveFromClassList(ShownClass);
            pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Reports a successful manipulation; the first one dismisses the chip.
        /// </summary>
        public void NotifyGesture(Gesture gesture)
        {
            if (isShown)
            {
                DismissByUser();
            }
        }

        private void DismissByUser()
        {
            if (!isShown)
            {
                return;
            }

            hasBeenDismissed = true;
            Dismiss();
            Dismissed?.Invoke();
        }

        private void ApplyTexts()
        {
            var defaults = platform == GestureHintPlatform.Mouse ? MouseDefaults : TouchDefaults;
            foreach (var pair in items)
            {
                var text = overrides.TryGetValue(pair.Key, out var custom) ? custom : defaults[pair.Key];
                pair.Value.prefix.text = text.prefix.ToUpperInvariant();
                pair.Value.verb.text = text.verb.ToUpperInvariant();
            }
        }
    }
}
