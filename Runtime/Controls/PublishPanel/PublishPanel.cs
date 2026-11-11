/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Where a publish is up to, for a <see cref="PublishPanel"/>.
    /// </summary>
    public enum PublishState
    {
        /// <summary>
        /// Not published yet; both actions available.
        /// </summary>
        Draft,

        /// <summary>
        /// Checking the space can be published.
        /// </summary>
        Validating,

        /// <summary>
        /// Uploading the map and content; the stage rows move.
        /// </summary>
        Uploading,

        /// <summary>
        /// The server is building the published map; the space is not live yet.
        /// </summary>
        Generating,

        /// <summary>
        /// Published; the visitor code works.
        /// </summary>
        Live,

        /// <summary>
        /// Something failed; the status line says what and Publish retries.
        /// </summary>
        Failed,
    }

    /// <summary>
    /// One progress row of a <see cref="PublishPanel"/>, such as the map or the content. Labels are display text.
    /// </summary>
    public sealed class PublishStage
    {
        public string Id;

        /// <summary>
        /// Such as "Map uploaded · 18 MB".
        /// </summary>
        public string Label;

        /// <summary>
        /// 0 to 1.
        /// </summary>
        public float Progress;

        public PublishStage() { }

        public PublishStage(string id, string label, float progress = 0f)
        {
            Id = id;
            Label = label;
            Progress = progress;
        }
    }

    /// <summary>
    /// The content of the save-and-publish sheet, as a panel. It has an eyebrow, a title and stats, a progress row per stage, a status
    /// line that says why a space is not live yet, an optional visibility choice, the visitor code, and Keep as draft
    /// and Publish buttons. Host it in your own sheet. It has no sheet chrome.
    /// </summary>
    /// <remarks>
    /// <para>Publish is disabled while validating, uploading or generating. The stage rows show while uploading and
    /// after. In the draft state they hide unless one has progress. An empty <c>code-url</c> hides the code card.</para>
    /// </remarks>
    [UxmlElement]
    public partial class PublishPanel : VisualElement
    {
        public const string RootClass = "publishPanel";
        public const string HeaderClass = "publishPanel__header";
        public const string HeadingsClass = "publishPanel__headings";
        public const string EyebrowClass = "publishPanel__eyebrow";
        public const string TitleClass = "publishPanel__title";
        public const string DetailClass = "publishPanel__detail";
        public const string CloseClass = "publishPanel__close";
        public const string CloseIconClass = "publishPanel__closeIcon";
        public const string StagesClass = "publishPanel__stages";
        public const string StageClass = "publishPanel__stage";
        public const string StageLabelsClass = "publishPanel__stageLabels";
        public const string StageLabelClass = "publishPanel__stageLabel";
        public const string StagePercentClass = "publishPanel__stagePercent";
        public const string StageBarClass = "publishPanel__stageBar";
        public const string StatusClass = "publishPanel__status";
        public const string VisibilityClass = "publishPanel__visibility";
        public const string CodeClass = "publishPanel__code";
        public const string ActionsClass = "publishPanel__actions";
        public const string SecondaryClass = "publishPanel__secondary";
        public const string PrimaryClass = "publishPanel__primary";

        private readonly Label eyebrow;
        private readonly Label title;
        private readonly Label detail;
        private readonly VisualElement close;
        private readonly VisualElement stagesContainer;
        private readonly Label status;
        private readonly SegmentedControl visibility;
        private readonly QRCodeCard code;
        private readonly PillButton secondary;
        private readonly PillButton primary;
        private readonly List<PublishStage> stages = new();
        private readonly Dictionary<string, VisualElement> stageElements = new();
        private PublishState state = PublishState.Draft;
        private string eyebrowText;

        public event Action PublishRequested;

        public event Action KeepDraftRequested;

        public event Action CloseRequested;

        /// <summary>
        /// Raised by Copy link with the code's URL.
        /// </summary>
        public event Action<string> CopyRequested;

        /// <summary>
        /// Raised by Share with the code's URL.
        /// </summary>
        public event Action<string> ShareRequested;

        public event Action<int, string> VisibilityChanged;

        public PillButton PrimaryButton => primary;

        public PillButton SecondaryButton => secondary;

        public QRCodeCard CodeCard => code;

        public SegmentedControl Visibility => visibility;

        [UxmlAttribute("state")]
        public PublishState State
        {
            get => state;
            set
            {
                state = value;
                Refresh();
            }
        }

        [UxmlAttribute("eyebrow")]
        public string Eyebrow
        {
            get => eyebrowText;
            set
            {
                eyebrowText = value ?? string.Empty;
                eyebrow.text = eyebrowText.ToUpperInvariant();
                eyebrow.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("title-text")]
        public string TitleText
        {
            get => title.text;
            set => title.text = value ?? string.Empty;
        }

        [UxmlAttribute("detail")]
        public string Detail
        {
            get => detail.text;
            set => detail.SetTextOrHide(value);
        }

        /// <summary>
        /// One line on where the publish is, such as "The map is being prepared. It goes live in a few minutes."
        /// </summary>
        [UxmlAttribute("status-text")]
        public string StatusText
        {
            get => status.text;
            set => status.SetTextOrHide(value);
        }

        [UxmlAttribute("code-url")]
        public string CodeUrl
        {
            get => code.Text;
            set
            {
                code.Text = value ?? string.Empty;
                code.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("primary-text")]
        public string PrimaryText
        {
            get => primary.Text;
            set => primary.Text = value ?? string.Empty;
        }

        [UxmlAttribute("secondary-text")]
        public string SecondaryText
        {
            get => secondary.Text;
            set
            {
                secondary.Text = value ?? string.Empty;
                secondary.SetDisplayed(!string.IsNullOrEmpty(value));
            }
        }

        [UxmlAttribute("show-close")]
        public bool ShowClose
        {
            get => close.style.display != DisplayStyle.None;
            set => close.SetDisplayed(value);
        }

        public PublishPanel()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            var header = UIToolkitExtensions.CreateVisualElement(this, HeaderClass);
            var headings = UIToolkitExtensions.CreateVisualElement(header, HeadingsClass);
            eyebrow = UIToolkitExtensions.CreateVisualElement<Label>(headings, EyebrowClass);
            title = UIToolkitExtensions.CreateVisualElement<Label>(headings, TitleClass);
            detail = UIToolkitExtensions.CreateVisualElement<Label>(headings, DetailClass);
            close = UIToolkitExtensions.CreateVisualElement(header, CloseClass);
            close.focusable = true;
            close.tabIndex = 0;
            close.tooltip = "Close";
            UIToolkitExtensions.CreateVisualElement(close, CloseIconClass).pickingMode = PickingMode.Ignore;
            close.RegisterActivate(() => CloseRequested?.Invoke());

            stagesContainer = UIToolkitExtensions.CreateVisualElement(this, StagesClass);
            status = UIToolkitExtensions.CreateVisualElement<Label>(this, StatusClass);

            visibility = new SegmentedControl();
            visibility.AddToClassList(VisibilityClass);
            visibility.SelectionChanged += (index, text) => VisibilityChanged?.Invoke(index, text);
            Add(visibility);

            code = new QRCodeCard();
            code.AddToClassList(CodeClass);
            code.CopyRequested += url => CopyRequested?.Invoke(url);
            code.ShareRequested += url => ShareRequested?.Invoke(url);
            Add(code);

            var actions = UIToolkitExtensions.CreateVisualElement(this, ActionsClass);
            secondary = new PillButton { Variant = PillButtonVariant.Ghost };
            secondary.AddToClassList(SecondaryClass);
            secondary.Clicked += () => KeepDraftRequested?.Invoke();
            actions.Add(secondary);
            primary = new PillButton { Variant = PillButtonVariant.Primary };
            primary.AddToClassList(PrimaryClass);
            primary.Clicked += () =>
            {
                if (IsActionable)
                {
                    PublishRequested?.Invoke();
                }
            };
            actions.Add(primary);

            Eyebrow = "Space";
            TitleText = "Save and publish";
            Detail = null;
            StatusText = null;
            CodeUrl = null;
            PrimaryText = "Publish";
            SecondaryText = "Keep as draft";
            SetVisibilityOptions(null);
            Refresh();
        }

        private bool IsActionable => state is PublishState.Draft or PublishState.Failed or PublishState.Live;

        public void SetStages(IReadOnlyList<PublishStage> newStages)
        {
            stages.Clear();
            stageElements.Clear();
            stagesContainer.Clear();
            if (newStages != null)
            {
                foreach (var stage in newStages)
                {
                    stages.Add(stage);
                    var row = UIToolkitExtensions.CreateVisualElement(stagesContainer, StageClass);
                    var labels = UIToolkitExtensions.CreateVisualElement(row, StageLabelsClass);
                    UIToolkitExtensions.CreateVisualElement<Label>(labels, StageLabelClass);
                    UIToolkitExtensions.CreateVisualElement<Label>(labels, StagePercentClass);
                    var bar = new LinearProgress();
                    bar.AddToClassList(StageBarClass);
                    row.Add(bar);
                    stageElements[stage.Id] = row;
                    ApplyStage(stage);
                }
            }

            Refresh();
        }

        /// <summary>
        /// Moves one stage's bar and, when given, replaces its label.
        /// </summary>
        public void SetStageProgress(string id, float progress, string label = null)
        {
            var stage = stages.Find(s => s.Id == id);
            if (stage == null)
            {
                return;
            }

            stage.Progress = Mathf.Clamp01(progress);
            if (label != null)
            {
                stage.Label = label;
            }

            ApplyStage(stage);
            Refresh();
        }

        /// <summary>
        /// The row for a stage, or null.
        /// </summary>
        public VisualElement StageElement(string id) => id != null && stageElements.TryGetValue(id, out var row) ? row : null;

        /// <summary>
        /// Shows a visibility choice, such as "Public" and "Unlisted"; null or fewer than two hides it.
        /// </summary>
        public void SetVisibilityOptions(IReadOnlyList<string> options, int selectedIndex = 0)
        {
            var show = options != null && options.Count > 1;
            visibility.SetDisplayed(show);
            if (show)
            {
                visibility.SetOptions(options, selectedIndex);
            }
        }

        private void ApplyStage(PublishStage stage)
        {
            var row = stageElements[stage.Id];
            row.Q<Label>(className: StageLabelClass).text = stage.Label ?? string.Empty;
            row.Q<Label>(className: StagePercentClass).text = $"{Mathf.RoundToInt(stage.Progress * 100f)}%";
            row.Q<LinearProgress>().Value = stage.Progress;
        }

        private static readonly string[] StateClasses =
        {
            RootClass + "--draft",
            RootClass + "--validating",
            RootClass + "--uploading",
            RootClass + "--generating",
            RootClass + "--live",
            RootClass + "--failed",
        };

        private void Refresh()
        {
            for (var i = 0; i < StateClasses.Length; i++)
            {
                EnableInClassList(StateClasses[i], i == (int)state);
            }

            var anyProgress = stages.Exists(s => s.Progress > 0f);
            stagesContainer.SetDisplayed(stages.Count > 0 && (state != PublishState.Draft || anyProgress));
            primary.SetEnabled(IsActionable);
            secondary.SetEnabled(state != PublishState.Validating && state != PublishState.Generating);
        }

    }
}
