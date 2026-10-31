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
    /// The content of the save-and-publish sheet, as a panel (FS04_5): eyebrow, title and stats, a progress row per stage, a status
    /// line that says why a space is not live yet, an optional visibility choice, the visitor code, and Keep as draft
    /// and Publish. Host it in your own sheet; it has no sheet chrome.
    /// </summary>
    /// <remarks>
    /// <para>Props: <c>state</c> (a <see cref="PublishState"/>), <c>eyebrow</c> (shown in capitals), <c>title-text</c>,
    /// <c>detail</c> (stats, such as "Map 18 MB · 6 items"), <c>status-text</c>, <c>code-url</c> (empty hides the code
    /// card), <c>primary-text</c> (default "Publish"), <c>secondary-text</c> (default "Keep as draft"),
    /// <c>show-close</c>. Methods: <see cref="SetStages"/>, <see cref="SetStageProgress"/>,
    /// <see cref="SetVisibilityOptions"/>, <see cref="StageElement"/>. Events: <see cref="PublishRequested"/>,
    /// <see cref="KeepDraftRequested"/>, <see cref="CloseRequested"/>, <see cref="CopyRequested"/>,
    /// <see cref="ShareRequested"/>, <see cref="VisibilityChanged"/>.</para>
    /// <para>Publish is disabled while validating, uploading or generating. The stage rows show while uploading and
    /// after; in the draft state they hide unless one has progress.</para>
    /// <para>USS classes: <c>publishPanel</c> (root), <c>publishPanel__header</c>, <c>publishPanel__headings</c>,
    /// <c>publishPanel__eyebrow</c>, <c>publishPanel__title</c>, <c>publishPanel__detail</c>, <c>publishPanel__close</c>, <c>publishPanel__closeIcon</c>,
    /// <c>publishPanel__stages</c>, <c>publishPanel__stage</c>, <c>publishPanel__stageLabels</c>,
    /// <c>publishPanel__stageLabel</c>, <c>publishPanel__stagePercent</c>, <c>publishPanel__stageBar</c>,
    /// <c>publishPanel__status</c>, <c>publishPanel__visibility</c> (a <see cref="SegmentedControl"/>),
    /// <c>publishPanel__code</c> (a <see cref="QRCodeCard"/>), <c>publishPanel__actions</c>,
    /// <c>publishPanel__secondary</c>, <c>publishPanel__primary</c>. Modifiers: <c>publishPanel--draft</c>,
    /// <c>--validating</c>, <c>--uploading</c>, <c>--generating</c>, <c>--live</c>, <c>--failed</c>.</para>
    /// <para>Tokens read: <c>--surface-fg</c>, <c>--surface-muted</c>, <c>--surface-border-strong</c>,
    /// <c>--surface-pressed</c>, <c>--c-warn</c>, <c>--radius-pill</c>, <c>--space-1..6</c>, <c>--text-micro</c>,
    /// <c>--text-caption</c>, <c>--text-title</c>, <c>--font-display</c>, <c>--font-body-medium</c>.</para>
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

        /// <summary>
        /// Raised when the visibility choice changes: index and text.
        /// </summary>
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
                eyebrow.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
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
            set => SetText(detail, value);
        }

        /// <summary>
        /// One line on where the publish is, such as "The map is being prepared. It goes live in a few minutes."
        /// </summary>
        [UxmlAttribute("status-text")]
        public string StatusText
        {
            get => status.text;
            set => SetText(status, value);
        }

        [UxmlAttribute("code-url")]
        public string CodeUrl
        {
            get => code.Text;
            set
            {
                code.Text = value ?? string.Empty;
                code.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
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
                secondary.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        [UxmlAttribute("show-close")]
        public bool ShowClose
        {
            get => close.style.display != DisplayStyle.None;
            set => close.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
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
            close.RegisterCallback<ClickEvent>(_ => CloseRequested?.Invoke());
            close.RegisterCallback<NavigationSubmitEvent>(evt =>
            {
                CloseRequested?.Invoke();
                evt.StopPropagation();
            });

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

        /// <summary>
        /// Sets the progress rows, such as the map and the content.
        /// </summary>
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
            visibility.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void Refresh()
        {
            foreach (PublishState value in Enum.GetValues(typeof(PublishState)))
            {
                EnableInClassList(RootClass + "--" + value.ToString().ToLowerInvariant(), value == state);
            }

            var anyProgress = stages.Exists(s => s.Progress > 0f);
            stagesContainer.style.display = stages.Count > 0 && (state != PublishState.Draft || anyProgress) ? DisplayStyle.Flex : DisplayStyle.None;
            primary.SetEnabled(IsActionable);
            secondary.SetEnabled(state != PublishState.Validating && state != PublishState.Generating);
        }

        private static void SetText(Label label, string text)
        {
            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
