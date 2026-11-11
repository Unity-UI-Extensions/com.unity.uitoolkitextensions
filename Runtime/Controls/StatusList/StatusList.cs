/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum StepState
    {
        Pending,
        Active,
        Done,
        Failed,
    }

    /// <summary>
    /// One row of a <see cref="StatusList"/>. Strings are display text; localise before passing them.
    /// </summary>
    public sealed class StatusStep
    {
        public string Id;
        public string Title;
        public string Detail;
        public StepState State;

        public StatusStep() { }

        public StatusStep(string id, string title, string detail = null, StepState state = StepState.Pending)
        {
            Id = id;
            Title = title;
            Detail = detail;
            State = state;
        }
    }

    /// <summary>
    /// Structured loading progress: one row per step (pending, active, done or failed, with an optional
    /// detail) and an overall bar. Raises <see cref="AllDone"/> when every step is done.
    /// </summary>
    /// <remarks>
    /// <para>The classes <c>statusList--complete</c> and <c>statusList--failed</c> go on the root.</para>
    /// </remarks>
    [UxmlElement]
    public partial class StatusList : VisualElement
    {
        public const string RootClass = "statusList";
        public const string StepsClass = "statusList__steps";
        public const string StepClass = "statusList__step";
        public const string MarkerClass = "statusList__marker";
        public const string SpinnerClass = "statusList__spinner";
        public const string TextsClass = "statusList__texts";
        public const string TitleClass = "statusList__title";
        public const string DetailClass = "statusList__detail";
        public const string BarClass = "statusList__bar";
        public const string BarFillClass = "statusList__barFill";
        public const string CompleteClass = "statusList--complete";
        public const string FailedClass = "statusList--failed";

        private sealed class Row
        {
            public StatusStep Step;
            public VisualElement Element;
            public VisualElement Marker;
            public LoadingIcon Spinner;
            public Label Detail;
        }

        private readonly VisualElement stepsRoot;
        private readonly VisualElement bar;
        private readonly VisualElement barFill;
        private readonly List<Row> rows = new();
        private float progress = -1f;
        private bool raisedAllDone;

        public event Action<string, StepState> StepChanged;

        /// <summary>
        /// Raised once when every step is done.
        /// </summary>
        public event Action AllDone;

        public IReadOnlyList<StatusStep> Steps => rows.ConvertAll(r => r.Step);

        /// <summary>
        /// The bar's value, 0 to 1. Negative derives it from the share of done steps.
        /// </summary>
        [UxmlAttribute("progress")]
        public float Progress
        {
            get => progress >= 0f ? progress : DerivedProgress();
            set
            {
                progress = value < 0f ? -1f : Mathf.Clamp01(value);
                UpdateBar();
            }
        }

        [UxmlAttribute("show-bar")]
        public bool ShowBar
        {
            get => bar.style.display != DisplayStyle.None;
            set => bar.SetDisplayed(value);
        }

        public StatusList()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);

            stepsRoot = UIToolkitExtensions.CreateVisualElement(this, StepsClass);
            bar = UIToolkitExtensions.CreateVisualElement(this, BarClass).MakePill();
            barFill = UIToolkitExtensions.CreateVisualElement(bar, BarFillClass).MakePill().IgnorePicking();

            UpdateBar();
        }

        public void SetSteps(IReadOnlyList<StatusStep> steps)
        {
            foreach (var row in rows)
            {
                row.Spinner.StopLoading();
            }

            stepsRoot.Clear();
            rows.Clear();
            raisedAllDone = false;

            if (steps != null)
            {
                foreach (var step in steps)
                {
                    rows.Add(CreateRow(step));
                }
            }

            foreach (var row in rows)
            {
                ApplyState(row);
            }

            UpdateSummary();
        }

        /// <summary>
        /// Changes a step's state and, when given, its detail line.
        /// </summary>
        /// <returns>False when no step has this id.</returns>
        public bool SetState(string id, StepState state, string detail = null)
        {
            var row = rows.Find(r => r.Step.Id == id);
            if (row == null)
            {
                return false;
            }

            var changed = row.Step.State != state;
            row.Step.State = state;
            if (detail != null)
            {
                row.Step.Detail = detail;
            }

            ApplyState(row);
            UpdateSummary();

            if (changed)
            {
                StepChanged?.Invoke(id, state);
            }

            if (!raisedAllDone && rows.Count > 0 && rows.TrueForAll(r => r.Step.State == StepState.Done))
            {
                raisedAllDone = true;
                AllDone?.Invoke();
            }

            return true;
        }

        public VisualElement StepElement(string id) => rows.Find(r => r.Step.Id == id)?.Element;

        private Row CreateRow(StatusStep source)
        {
            var step = new StatusStep(source.Id, source.Title, source.Detail, source.State);
            var element = UIToolkitExtensions.CreateVisualElement(stepsRoot, StepClass);

            var marker = UIToolkitExtensions.CreateVisualElement(element, MarkerClass).IgnorePicking();

            var spinner = new LoadingIcon();
            spinner.AddToClassList(SpinnerClass);
            element.Add(spinner);

            var texts = UIToolkitExtensions.CreateVisualElement(element, TextsClass);
            var title = UIToolkitExtensions.CreateVisualElement<Label>(texts, TitleClass);
            title.text = step.Title ?? string.Empty;
            var detail = UIToolkitExtensions.CreateVisualElement<Label>(texts, DetailClass);

            return new Row { Step = step, Element = element, Marker = marker, Spinner = spinner, Detail = detail };
        }

        private static readonly string[] StateClasses =
        {
            StepClass + "--pending",
            StepClass + "--active",
            StepClass + "--done",
            StepClass + "--failed",
        };

        private static void ApplyState(Row row)
        {
            var state = row.Step.State;
            for (var i = 0; i < StateClasses.Length; i++)
            {
                row.Element.EnableInClassList(StateClasses[i], i == (int)state);
            }

            var active = state == StepState.Active;
            row.Marker.SetDisplayed(!active);
            row.Spinner.SetDisplayed(active);
            if (active)
            {
                row.Spinner.PlayLoading();
            }
            else
            {
                row.Spinner.StopLoading();
            }

            row.Detail.SetTextOrHide(row.Step.Detail);
        }

        private float DerivedProgress()
        {
            if (rows.Count == 0)
            {
                return 0f;
            }

            var done = 0;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].Step.State == StepState.Done)
                {
                    done++;
                }
            }

            return (float)done / rows.Count;
        }

        private void UpdateSummary()
        {
            EnableInClassList(CompleteClass, rows.Count > 0 && rows.TrueForAll(r => r.Step.State == StepState.Done));
            EnableInClassList(FailedClass, rows.Exists(r => r.Step.State == StepState.Failed));
            UpdateBar();
        }

        private void UpdateBar()
        {
            barFill.style.width = Length.Percent(Progress * 100f);
        }
    }
}
