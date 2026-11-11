/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The card for one stop under a map: a number disc, the stop name, a meta line such as
    /// "24 m · 3 min · not visited", and one action pill such as "Go".
    /// </summary>
    /// <remarks>
    /// <para>An empty <c>action-text</c> hides the pill. The pill is a primary compact <see cref="PillButton"/>. The
    /// <c>state</c> is a <see cref="MapStopState"/>: the disc is filled once visited, ringed otherwise.
    /// <see cref="Tapped"/> is a tap on the card outside the pill. Strings are display text.</para>
    /// </remarks>
    [UxmlElement]
    public partial class MapStopCard : VisualElement
    {
        public const string RootClass = "mapStopCard";
        public const string NumberClass = "mapStopCard__number";
        public const string NumberLabelClass = "mapStopCard__numberLabel";
        public const string TextsClass = "mapStopCard__texts";
        public const string TitleClass = "mapStopCard__title";
        public const string MetaClass = "mapStopCard__meta";
        public const string ActionClass = "mapStopCard__action";
        public const string PendingClass = "mapStopCard--pending";
        public const string NextClass = "mapStopCard--next";
        public const string VisitedClass = "mapStopCard--visited";

        private readonly Label numberLabel;
        private readonly Label title;
        private readonly Label meta;
        private readonly PillButton action;
        private MapStopState state = MapStopState.Next;

        public event Action ActionClicked;

        /// <summary>
        /// Raised when the card is tapped outside the action, for example to open the stop's details.
        /// </summary>
        public event Action Tapped;

        public PillButton ActionButton => action;

        [UxmlAttribute("number")]
        public string Number
        {
            get => numberLabel.text;
            set => numberLabel.text = value ?? string.Empty;
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("meta")]
        public string Meta
        {
            get => meta.text;
            set
            {
                meta.SetTextOrHide(value);
            }
        }

        [UxmlAttribute("action-text")]
        public string ActionText
        {
            get => action.Text;
            set
            {
                action.Text = value ?? string.Empty;
                action.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("state")]
        public MapStopState State
        {
            get => state;
            set
            {
                state = value;
                EnableInClassList(PendingClass, state == MapStopState.Pending);
                EnableInClassList(NextClass, state == MapStopState.Next);
                EnableInClassList(VisitedClass, state == MapStopState.Visited);
            }
        }

        public MapStopCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var number = UIToolkitExtensions.CreateVisualElement(this, NumberClass).IgnorePicking();
            numberLabel = UIToolkitExtensions.CreateVisualElement<Label>(number, NumberLabelClass).IgnorePicking();

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass).IgnorePicking();
            title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            meta = UIToolkitExtensions.CreateVisualElement<Label>(texts, MetaClass);

            action = new PillButton { Variant = PillButtonVariant.Primary, Compact = true };
            action.AddToClassList(ActionClass);
            action.Clicked += () => ActionClicked?.Invoke();
            Add(action);

            RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is VisualElement target && action.Contains(target))
                {
                    return;
                }

                Tapped?.Invoke();
            });

            State = state;
            Meta = null;
            ActionText = "Go";
        }

        public MapStopCard(string number, string titleText, string metaText, MapStopState stopState = MapStopState.Next)
            : this()
        {
            Number = number;
            TitleText = titleText;
            Meta = metaText;
            State = stopState;
        }
    }
}
