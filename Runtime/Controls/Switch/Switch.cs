/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A labelled on/off switch: a track with a sliding thumb. A <see cref="BaseField{T}"/> of bool, so it has
    /// the field label, value, <see cref="ChangeEvent{T}"/> and binding behaviour of the built-in controls.
    /// </summary>
    /// <remarks>
    /// <para>Tap anywhere on the field, or press submit (Enter, Space, gamepad A) while it has focus, to toggle.</para>
    /// <para>Props: <c>label</c>, <c>value</c>. Events: <see cref="ValueChanged"/> and <see cref="ChangeEvent{T}"/>.</para>
    /// <para>USS classes: <c>switch</c> (root, alongside the <c>unity-base-field</c> classes), <c>switch__track</c>,
    /// <c>switch__fill</c> (the primary gradient shown when on), <c>switch__thumb</c>. Modifier: <c>switch--on</c>.</para>
    /// <para>Off is a grey track with a white thumb, on is the primary gradient with a white thumb (FS09_5, FS10_2).</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-switch-off</c>, <c>--surface-accent</c>, <c>--c-primary</c>,
    /// <c>--c-on-primary</c>, <c>--grad-brand-start/end</c> (through <see cref="GradientFill"/>), <c>--space-3</c>,
    /// <c>--text-body</c>, <c>--font-body</c>, <c>--motion-fast</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class Switch : BaseField<bool>
    {
        public const string RootClass = "switch";
        public const string TrackClass = "switch__track";
        public const string ThumbClass = "switch__thumb";
        public const string FillClass = "switch__fill";
        public const string OnClass = "switch--on";

        private readonly VisualElement track;
        private readonly VisualElement thumb;

        /// <summary>
        /// Raised when the value changes through the UI or <see cref="BaseField{T}.value"/>; not by SetValueWithoutNotify.
        /// </summary>
        public event Action<bool> ValueChanged;

        public Switch()
            : this(null)
        { }

        public Switch(string label)
            : this(label, new VisualElement())
        { }

        private Switch(string label, VisualElement input)
            : base(label, input)
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            track = input;
            track.AddToClassList(TrackClass);
            track.MakePill();
            track.pickingMode = PickingMode.Ignore;

            var fill = UIToolkitExtensions.CreateVisualElement(track, FillClass).MakePill();
            fill.pickingMode = PickingMode.Ignore;
            fill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));

            thumb = UIToolkitExtensions.CreateVisualElement(track, ThumbClass);
            thumb.pickingMode = PickingMode.Ignore;

            RegisterCallback<ClickEvent>(_ => Toggle());
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            RegisterCallback<ChangeEvent<bool>>(evt => ValueChanged?.Invoke(evt.newValue));

            UpdateState(value);
        }

        /// <summary>
        /// Flips the value, raising the change events.
        /// </summary>
        public void Toggle()
        {
            value = !value;
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateState(newValue);
        }

        private void UpdateState(bool on)
        {
            EnableInClassList(OnClass, on);
        }

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            Toggle();
            evt.StopPropagation();
        }
    }
}
