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
    /// <para>Off is a grey track with a white thumb. On is the primary gradient with a white thumb.</para>
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
        /// Raised when the value changes through the UI or <see cref="BaseField{T}.value"/>. SetValueWithoutNotify does not raise it.
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

            var fill = UIToolkitExtensions.CreateVisualElement(track, FillClass).MakePill().IgnorePicking();
            fill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));

            thumb = UIToolkitExtensions.CreateVisualElement(track, ThumbClass).IgnorePicking();

            RegisterCallback<ClickEvent>(_ => Toggle());
            RegisterCallback<NavigationSubmitEvent>(OnSubmit);
            RegisterCallback<ChangeEvent<bool>>(evt => ValueChanged?.Invoke(evt.newValue));

            EnableInClassList(OnClass, value);
        }

        public void Toggle()
        {
            value = !value;
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            EnableInClassList(OnClass, newValue);
        }

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            Toggle();
            evt.StopPropagation();
        }
    }
}
