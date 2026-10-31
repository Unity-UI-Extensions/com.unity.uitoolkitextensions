/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The card for one stop under a map (FS09_1): a number disc, the stop name, a meta line such as
    /// "24 m · 3 min · not visited", and one action pill such as "Go".
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>number</c>, <c>title-text</c>, <c>meta</c>, <c>action-text</c> (empty hides the pill),
    /// <c>state</c> (a <see cref="MapStopState"/>: the disc is filled once visited, ringed otherwise).
    /// Events: <see cref="ActionClicked"/>, <see cref="Tapped"/> (the card outside the pill). Strings are display text.</para>
    /// <para>USS classes: <c>mapStopCard</c> (root), <c>mapStopCard__number</c>, <c>mapStopCard__numberLabel</c>,
    /// <c>mapStopCard__texts</c>, <c>mapStopCard__title</c>, <c>mapStopCard__meta</c>, <c>mapStopCard__action</c>
    /// (a primary compact <see cref="PillButton"/>). Modifiers: <c>mapStopCard--pending</c>, <c>--next</c>,
    /// <c>--visited</c>.</para>
    /// <para>Tokens read: <c>--surface-raised</c>, <c>--surface-border</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--c-navy-2</c>, <c>--c-on-primary</c>, <c>--c-page-card</c>, <c>--radius-pill</c>, <c>--space-1..4</c>,
    /// <c>--text-caption</c>, <c>--text-label</c>, <c>--font-display</c>, <c>--font-body</c>.</para>
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
                meta.text = value ?? string.Empty;
                meta.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("action-text")]
        public string ActionText
        {
            get => action.Text;
            set
            {
                action.Text = value ?? string.Empty;
                action.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
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

            var number = UIToolkitExtensions.CreateVisualElement(this, NumberClass);
            number.pickingMode = PickingMode.Ignore;
            numberLabel = UIToolkitExtensions.CreateVisualElement<Label>(number, NumberLabelClass);
            numberLabel.pickingMode = PickingMode.Ignore;

            var texts = UIToolkitExtensions.CreateVisualElement(this, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
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
