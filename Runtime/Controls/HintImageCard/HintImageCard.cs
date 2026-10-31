/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Where finding the start point is up to, for <see cref="HintImageCard.State"/>.
    /// </summary>
    public enum HintState
    {
        /// <summary>
        /// No state: the app drives the proximity strip itself.
        /// </summary>
        None,

        /// <summary>
        /// Waiting for coverage data or the first fix: a moving bar.
        /// </summary>
        Loading,
        Far,
        Near,
        Found,

        /// <summary>
        /// Not found: the tips and the Retry and Skip actions show.
        /// </summary>
        Failed,
    }

    /// <summary>
    /// A hint for finding a start point: a picture of what to face, with a small "HINT" tag, and a line of guidance
    /// beside it. An optional proximity strip below shows how close the user is, such as FAR, NEAR, FOUND.
    /// </summary>
    /// <remarks>
    /// <para>Without a picture, a gradient placeholder fills the image. <see cref="Embedded"/> drops the card's own
    /// background and uses a smaller image, for use inside another card.</para>
    /// <para>Props: <c>text</c>, <c>tag-text</c> (default "Hint", shown in capitals; empty hides the tag),
    /// <c>embedded</c>, <c>show-proximity</c>, <c>proximity-labels</c> (comma separated, default "Far,Near,Found",
    /// shown in capitals), <c>proximity</c> (the current segment, -1 for none).
    /// Methods: <see cref="SetImage"/>, <see cref="SetProximity"/>, <see cref="SetProximityLabels"/>.</para>
    /// <para>State machine: <c>state</c> (a <see cref="HintState"/>) moves the strip (Far, Near, Found are its three
    /// segments), shows a moving bar while Loading, and on Failed shows the tips (<see cref="SetTips"/>) and the
    /// actions (<c>retry-text</c>, default "Try again"; <c>skip-text</c>, empty hides it). Also <c>location-name</c>
    /// and <c>distance</c> (metres, negative hides it) above the guidance. Events: <see cref="RetryRequested"/>,
    /// <see cref="SkipRequested"/>, <see cref="StateChanged"/>.</para>
    /// <para>USS classes: <c>hintImageCard</c> (root), <c>hintImageCard__row</c>, <c>hintImageCard__image</c>,
    /// <c>hintImageCard__placeholder</c>, <c>hintImageCard__picture</c>, <c>hintImageCard__tag</c>,
    /// <c>hintImageCard__text</c>, <c>hintImageCard__proximity</c>, <c>hintImageCard__segment</c>,
    /// <c>hintImageCard__segmentFill</c>, <c>hintImageCard__segmentLabel</c>, <c>hintImageCard__texts</c>,
    /// <c>hintImageCard__heading</c>, <c>hintImageCard__location</c>, <c>hintImageCard__distance</c>,
    /// <c>hintImageCard__loading</c> (a <see cref="LinearProgress"/>), <c>hintImageCard__tips</c>,
    /// <c>hintImageCard__tip</c>, <c>hintImageCard__actions</c>, <c>hintImageCard__retry</c>,
    /// <c>hintImageCard__skip</c>.</para>
    /// <para>Modifier classes: <c>hintImageCard--embedded</c>, <c>hintImageCard--hasImage</c>,
    /// <c>hintImageCard__segment--current</c>, <c>hintImageCard--loading</c>, <c>--far</c>, <c>--near</c>,
    /// <c>--found</c>, <c>--failed</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-border</c>, <c>--surface-border-strong</c>,
    /// <c>--surface-fg</c>, <c>--surface-fg-soft</c>, <c>--surface-muted</c>, <c>--c-bg</c>, <c>--c-fg-muted</c>,
    /// <c>--c-primary</c>, <c>--c-on-primary</c>, <c>--radius-card</c>, <c>--radius-input</c>, <c>--radius-sm</c>,
    /// <c>--radius-pill</c>, <c>--space-3</c>, <c>--text-label</c>, <c>--text-micro</c>,
    /// <c>--font-body</c>, <c>--font-body-medium</c>, <c>--font-body-semibold</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class HintImageCard : VisualElement
    {
        public const string RootClass = "hintImageCard";
        public const string RowClass = "hintImageCard__row";
        public const string ImageClass = "hintImageCard__image";
        public const string PlaceholderClass = "hintImageCard__placeholder";
        public const string PictureClass = "hintImageCard__picture";
        public const string TagClass = "hintImageCard__tag";
        public const string TextClass = "hintImageCard__text";
        public const string ProximityClass = "hintImageCard__proximity";
        public const string SegmentClass = "hintImageCard__segment";
        public const string SegmentFillClass = "hintImageCard__segmentFill";
        public const string SegmentLabelClass = "hintImageCard__segmentLabel";
        public const string CurrentSegmentClass = "hintImageCard__segment--current";
        public const string EmbeddedClass = "hintImageCard--embedded";
        public const string HasImageClass = "hintImageCard--hasImage";
        public const string TextsClass = "hintImageCard__texts";
        public const string HeadingClass = "hintImageCard__heading";
        public const string LocationClass = "hintImageCard__location";
        public const string DistanceClass = "hintImageCard__distance";
        public const string LoadingClass = "hintImageCard__loading";
        public const string TipsClass = "hintImageCard__tips";
        public const string TipClass = "hintImageCard__tip";
        public const string ActionsClass = "hintImageCard__actions";
        public const string RetryClass = "hintImageCard__retry";
        public const string SkipClass = "hintImageCard__skip";

        private readonly VisualElement picture;
        private readonly Label tagLabel;
        private readonly Label textLabel;
        private readonly VisualElement heading;
        private readonly Label locationLabel;
        private readonly Label distanceLabel;
        private readonly LinearProgress loading;
        private readonly VisualElement tips;
        private readonly VisualElement actions;
        private readonly PillButton retry;
        private readonly PillButton skip;
        private HintState state = HintState.None;
        private float distance = -1f;
        private readonly VisualElement proximityStrip;
        private readonly List<VisualElement> segments = new List<VisualElement>();
        private string tagText = string.Empty;
        private string[] proximityLabels = { "Far", "Near", "Found" };
        private int proximity = -1;

        /// <summary>
        /// The guidance beside the picture.
        /// </summary>
        [UxmlAttribute("text")]
        public string Text
        {
            get => textLabel.text;
            set => textLabel.text = value ?? string.Empty;
        }

        /// <summary>
        /// The tag on the picture, shown in capitals; empty hides it.
        /// </summary>
        [UxmlAttribute("tag-text")]
        public string TagText
        {
            get => tagText;
            set
            {
                tagText = value ?? string.Empty;
                tagLabel.text = tagText.ToUpperInvariant();
                tagLabel.style.display = tagText.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// No card background and a smaller image, for use inside another card.
        /// </summary>
        [UxmlAttribute("embedded")]
        public bool Embedded
        {
            get => ClassListContains(EmbeddedClass);
            set => EnableInClassList(EmbeddedClass, value);
        }

        [UxmlAttribute("show-proximity")]
        public bool ShowProximity
        {
            get => proximityStrip.style.display != DisplayStyle.None;
            set => proximityStrip.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// The proximity labels, comma separated, nearest last.
        /// </summary>
        [UxmlAttribute("proximity-labels")]
        public string ProximityLabels
        {
            get => string.Join(",", proximityLabels);
            set => SetProximityLabels((value ?? string.Empty).Split(','));
        }

        /// <summary>
        /// The current proximity segment, or -1 for none.
        /// </summary>
        [UxmlAttribute("proximity")]
        public int Proximity
        {
            get => proximity;
            set => SetProximity(value);
        }

        public VisualElement ProximityStrip => proximityStrip;

        public PillButton RetryButton => retry;

        public PillButton SkipButton => skip;

        public event Action RetryRequested;

        public event Action SkipRequested;

        public event Action<HintState> StateChanged;

        [UxmlAttribute("state")]
        public HintState State
        {
            get => state;
            set
            {
                if (state == value)
                {
                    return;
                }

                state = value;
                ApplyState();
                StateChanged?.Invoke(state);
            }
        }

        /// <summary>
        /// The place to find, such as "Main entrance"; empty hides it.
        /// </summary>
        [UxmlAttribute("location-name")]
        public string LocationName
        {
            get => locationLabel.text;
            set
            {
                locationLabel.text = value ?? string.Empty;
                UpdateHeading();
            }
        }

        /// <summary>
        /// Metres to the place; negative hides it.
        /// </summary>
        [UxmlAttribute("distance")]
        public float Distance
        {
            get => distance;
            set
            {
                distance = value;
                distanceLabel.text = value < 0f ? string.Empty : WorldAnchoredLabel.FormatDistance(value);
                UpdateHeading();
            }
        }

        [UxmlAttribute("retry-text")]
        public string RetryText
        {
            get => retry.Text;
            set => SetButton(retry, value);
        }

        [UxmlAttribute("skip-text")]
        public string SkipText
        {
            get => skip.Text;
            set => SetButton(skip, value);
        }

        public IReadOnlyList<VisualElement> ProximitySegments => segments;

        public HintImageCard()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var row = UIToolkitExtensions.CreateVisualElement(this, RowClass);
            row.pickingMode = PickingMode.Ignore;

            var image = UIToolkitExtensions.CreateVisualElement(row, ImageClass);
            image.pickingMode = PickingMode.Ignore;
            var placeholder = UIToolkitExtensions.CreateVisualElement(image, PlaceholderClass);
            placeholder.pickingMode = PickingMode.Ignore;
            placeholder.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            picture = UIToolkitExtensions.CreateVisualElement(image, PictureClass);
            picture.pickingMode = PickingMode.Ignore;
            tagLabel = UIToolkitExtensions.CreateVisualElement<Label>(image, TagClass).MakePill();
            tagLabel.pickingMode = PickingMode.Ignore;

            var texts = UIToolkitExtensions.CreateVisualElement(row, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            heading = UIToolkitExtensions.CreateVisualElement(texts, HeadingClass);
            heading.pickingMode = PickingMode.Ignore;
            locationLabel = UIToolkitExtensions.CreateVisualElement<Label>(heading, LocationClass);
            distanceLabel = UIToolkitExtensions.CreateVisualElement<Label>(heading, DistanceClass);
            textLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TextClass);
            textLabel.pickingMode = PickingMode.Ignore;

            loading = new LinearProgress { Indeterminate = true };
            loading.AddToClassList(LoadingClass);
            Add(loading);

            proximityStrip = UIToolkitExtensions.CreateVisualElement(this, ProximityClass);
            proximityStrip.pickingMode = PickingMode.Ignore;

            tips = UIToolkitExtensions.CreateVisualElement(this, TipsClass);
            tips.pickingMode = PickingMode.Ignore;
            actions = UIToolkitExtensions.CreateVisualElement(this, ActionsClass);
            skip = new PillButton { Variant = PillButtonVariant.Ghost, Compact = true };
            skip.AddToClassList(SkipClass);
            skip.Clicked += () => SkipRequested?.Invoke();
            actions.Add(skip);
            retry = new PillButton { Variant = PillButtonVariant.Primary, Compact = true };
            retry.AddToClassList(RetryClass);
            retry.Clicked += () => RetryRequested?.Invoke();
            actions.Add(retry);

            TagText = "Hint";
            RetryText = "Try again";
            SkipText = null;
            UpdateHeading();
            ApplyState();
            ShowProximity = false;
            SetProximityLabels(proximityLabels);
            SetImage(default);
        }

        public HintImageCard(string text)
            : this()
        {
            Text = text;
        }

        /// <summary>
        /// Sets the picture from a texture, sprite or vector image; a default value shows the placeholder.
        /// </summary>
        public void SetImage(Background image)
        {
            var has = image.texture != null || image.sprite != null || image.vectorImage != null || image.renderTexture != null;
            picture.style.backgroundImage = has ? new StyleBackground(image) : new StyleBackground(StyleKeyword.None);
            EnableInClassList(HasImageClass, has);
        }

        /// <summary>
        /// Fills one proximity segment; -1 or out of range clears them all. Shows the strip.
        /// </summary>
        public void SetProximity(int index)
        {
            proximity = index >= 0 && index < segments.Count ? index : -1;
            for (var i = 0; i < segments.Count; i++)
            {
                segments[i].EnableInClassList(CurrentSegmentClass, i == proximity);
            }

            if (proximity >= 0)
            {
                ShowProximity = true;
            }
        }

        /// <summary>
        /// Recovery tips shown while <see cref="State"/> is Failed, such as "Face the doors from the steps".
        /// </summary>
        public void SetTips(IReadOnlyList<string> tipTexts)
        {
            tips.Clear();
            if (tipTexts != null)
            {
                foreach (var tip in tipTexts)
                {
                    var label = UIToolkitExtensions.CreateVisualElement<Label>(tips, TipClass);
                    label.text = "\u2022 " + tip;
                }
            }

            ApplyState();
        }

        private void ApplyState()
        {
            EnableInClassList(RootClass + "--loading", state == HintState.Loading);
            EnableInClassList(RootClass + "--far", state == HintState.Far);
            EnableInClassList(RootClass + "--near", state == HintState.Near);
            EnableInClassList(RootClass + "--found", state == HintState.Found);
            EnableInClassList(RootClass + "--failed", state == HintState.Failed);

            loading.style.display = state == HintState.Loading ? DisplayStyle.Flex : DisplayStyle.None;
            var failed = state == HintState.Failed;
            tips.style.display = failed && tips.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            actions.style.display = failed ? DisplayStyle.Flex : DisplayStyle.None;

            switch (state)
            {
                case HintState.Far:
                    SetProximity(0);
                    break;
                case HintState.Near:
                    SetProximity(Mathf.Min(1, segments.Count - 1));
                    break;
                case HintState.Found:
                    SetProximity(segments.Count - 1);
                    break;
                case HintState.Loading:
                case HintState.Failed:
                    SetProximity(-1);
                    break;
            }
        }

        private void UpdateHeading()
        {
            locationLabel.style.display = string.IsNullOrEmpty(locationLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
            distanceLabel.style.display = string.IsNullOrEmpty(distanceLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
            heading.style.display = string.IsNullOrEmpty(locationLabel.text) && string.IsNullOrEmpty(distanceLabel.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static void SetButton(PillButton button, string text)
        {
            button.Text = text ?? string.Empty;
            button.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Replaces the proximity labels, nearest last. Keeps the current segment when it still exists.
        /// </summary>
        public void SetProximityLabels(params string[] labels)
        {
            proximityLabels = labels ?? new string[0];
            proximityStrip.Clear();
            segments.Clear();
            foreach (var label in proximityLabels)
            {
                var segment = UIToolkitExtensions.CreateVisualElement(proximityStrip, SegmentClass);
                segment.pickingMode = PickingMode.Ignore;
                var fill = UIToolkitExtensions.CreateVisualElement(segment, SegmentFillClass);
                fill.pickingMode = PickingMode.Ignore;
                fill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
                var text = UIToolkitExtensions.CreateVisualElement<Label>(segment, SegmentLabelClass);
                text.pickingMode = PickingMode.Ignore;
                text.text = (label ?? string.Empty).Trim().ToUpperInvariant();
                segments.Add(segment);
            }

            var keep = proximity;
            proximity = -1;
            if (keep >= 0 && keep < segments.Count)
            {
                SetProximity(keep);
            }
        }
    }
}
