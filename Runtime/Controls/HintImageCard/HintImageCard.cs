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
    /// <para>The tag text defaults to "Hint". The proximity labels default to "Far,Near,Found", shown in capitals.</para>
    /// <para>State machine: <c>state</c> (a <see cref="HintState"/>) moves the strip (Far, Near, Found are its three
    /// segments), shows a moving bar while Loading, and on Failed shows the tips (<see cref="SetTips"/>) and the
    /// actions (<c>retry-text</c>, default "Try again"; <c>skip-text</c>, empty hides it).</para>
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
            set => proximityStrip.SetDisplayed(value);
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

            var row = UIToolkitExtensions.CreateVisualElement(this, RowClass).IgnorePicking();

            var image = UIToolkitExtensions.CreateVisualElement(row, ImageClass).IgnorePicking();
            var placeholder = UIToolkitExtensions.CreateVisualElement(image, PlaceholderClass).IgnorePicking();
            placeholder.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
            picture = UIToolkitExtensions.CreateVisualElement(image, PictureClass).IgnorePicking();
            tagLabel = UIToolkitExtensions.CreateVisualElement<Label>(image, TagClass).MakePill().IgnorePicking();

            var texts = UIToolkitExtensions.CreateVisualElement(row, TextsClass).IgnorePicking();
            heading = UIToolkitExtensions.CreateVisualElement(texts, HeadingClass).IgnorePicking();
            locationLabel = UIToolkitExtensions.CreateVisualElement<Label>(heading, LocationClass);
            distanceLabel = UIToolkitExtensions.CreateVisualElement<Label>(heading, DistanceClass);
            textLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, TextClass).IgnorePicking();

            loading = new LinearProgress { Indeterminate = true };
            loading.AddToClassList(LoadingClass);
            Add(loading);

            proximityStrip = UIToolkitExtensions.CreateVisualElement(this, ProximityClass).IgnorePicking();

            tips = UIToolkitExtensions.CreateVisualElement(this, TipsClass).IgnorePicking();
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
            var has = picture.SetImageOrNone(image);
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

            loading.SetDisplayed(state == HintState.Loading);
            var failed = state == HintState.Failed;
            tips.SetDisplayed(failed && tips.childCount > 0);
            actions.SetDisplayed(failed);

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
            var hasLocation = !string.IsNullOrEmpty(locationLabel.text);
            var hasDistance = !string.IsNullOrEmpty(distanceLabel.text);
            locationLabel.SetDisplayed(hasLocation);
            distanceLabel.SetDisplayed(hasDistance);
            heading.SetDisplayed(hasLocation || hasDistance);
        }

        private static void SetButton(PillButton button, string text)
        {
            button.Text = text ?? string.Empty;
            button.SetDisplayed(!string.IsNullOrEmpty(text));
        }

        /// <summary>
        /// Replaces the proximity labels, nearest last. Keeps the current segment when it still exists.
        /// </summary>
        public void SetProximityLabels(params string[] labels)
        {
            proximityLabels = labels ?? Array.Empty<string>();
            proximityStrip.Clear();
            segments.Clear();
            foreach (var label in proximityLabels)
            {
                var segment = UIToolkitExtensions.CreateVisualElement(proximityStrip, SegmentClass).IgnorePicking();
                var fill = UIToolkitExtensions.CreateVisualElement(segment, SegmentFillClass).IgnorePicking();
                fill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));
                var text = UIToolkitExtensions.CreateVisualElement<Label>(segment, SegmentLabelClass).IgnorePicking();
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
