/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum StatusState
    {
        /// <summary>
        /// Neutral information, such as "Offline".
        /// </summary>
        Info,

        /// <summary>
        /// Work in progress; shows a spinner.
        /// </summary>
        Busy,

        Ok,

        /// <summary>
        /// Needs attention but not failed, such as "Offline" or "Permission denied".
        /// </summary>
        Warn,

        Fail,

        /// <summary>
        /// A recording in progress, such as "REC 00:12": red fill, white dot and text.
        /// </summary>
        Recording,
    }

    /// <summary>
    /// A compact pill that shows one persistent status (info, busy, ok, warn or fail) until it is resolved.
    /// Use <see cref="StatusChipHost"/> to manage several by id.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="StatusState.Recording"/> state is always filled: red, with a white dot
    /// and bold text.</para>
    /// </remarks>
    [UxmlElement]
    public partial class StatusChip : VisualElement
    {
        public const string RootClass = "statusChip";
        public const string IndicatorClass = "statusChip__indicator";
        public const string SpinnerClass = "statusChip__spinner";
        public const string LabelClass = "statusChip__label";
        public const string InfoClass = "statusChip--info";
        public const string BusyClass = "statusChip--busy";
        public const string OkClass = "statusChip--ok";
        public const string WarnClass = "statusChip--warn";
        public const string FailClass = "statusChip--fail";
        public const string RecordingClass = "statusChip--recording";
        public const string FilledClass = "statusChip--filled";
        public const string HasIconClass = "statusChip--hasIcon";
        public const string IconClass = "statusChip__icon";

        private readonly VisualElement indicator;
        private readonly VisualElement icon;
        private bool filled;
        private readonly LoadingIcon spinner;
        private readonly Label label;
        private StatusState state;

        public event Action Clicked;

        [UxmlAttribute("text")]
        public string Text
        {
            get => label.text;
            set => label.text = value ?? string.Empty;
        }

        [UxmlAttribute("state")]
        public StatusState State
        {
            get => state;
            set => SetState(value);
        }

        /// <summary>
        /// Fills the chip with its state's colour. A recording chip is always filled.
        /// </summary>
        [UxmlAttribute("filled")]
        public bool Filled
        {
            get => filled;
            set
            {
                filled = value;
                EnableInClassList(FilledClass, filled);
            }
        }

        /// <summary>
        /// Shows an icon in place of the state dot, tinted to match the text; a default value restores the dot.
        /// </summary>
        public void SetIcon(Background image)
        {
            var hasIcon = icon.SetImageOrNone(image);
            EnableInClassList(HasIconClass, hasIcon);
            indicator.style.display = state == StatusState.Busy || hasIcon ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public StatusChip()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.MakePill();

            indicator = UIToolkitExtensions.CreateVisualElement(this, IndicatorClass).IgnorePicking();
            icon = UIToolkitExtensions.CreateVisualElement(this, IconClass).IgnorePicking();

            spinner = new LoadingIcon();
            spinner.AddToClassList(SpinnerClass);
            Add(spinner);

            label = UIToolkitExtensions.CreateVisualElement<Label>(this, LabelClass).IgnorePicking();

            RegisterCallback<ClickEvent>(_ => Clicked?.Invoke());
            RegisterCallback<DetachFromPanelEvent>(_ => spinner.StopLoading());
            RegisterCallback<AttachToPanelEvent>(_ => SetState(state, force: true));

            SetState(StatusState.Info, force: true);
        }

        public StatusChip(string text, StatusState state)
            : this()
        {
            Text = text;
            State = state;
        }

        private void SetState(StatusState value, bool force = false)
        {
            if (state == value && !force)
            {
                return;
            }

            state = value;
            EnableInClassList(InfoClass, state == StatusState.Info);
            EnableInClassList(BusyClass, state == StatusState.Busy);
            EnableInClassList(OkClass, state == StatusState.Ok);
            EnableInClassList(WarnClass, state == StatusState.Warn);
            EnableInClassList(FailClass, state == StatusState.Fail);
            EnableInClassList(RecordingClass, state == StatusState.Recording);

            var busy = state == StatusState.Busy;
            indicator.style.display = busy || ClassListContains(HasIconClass) ? DisplayStyle.None : DisplayStyle.Flex;
            spinner.SetDisplayed(busy);
            if (busy && panel != null)
            {
                spinner.PlayLoading();
            }
            else
            {
                spinner.StopLoading();
            }
        }
    }

    /// <summary>
    /// A band of <see cref="StatusChip"/>s keyed by id: <see cref="ShowStatus"/> adds or updates a chip,
    /// <see cref="Resolve"/> removes it, optionally after showing a final state for a moment.
    /// </summary>
    /// <remarks>
    /// <para>The <c>linger-ms</c> value sets how long a resolved chip stays.</para>
    /// </remarks>
    [UxmlElement]
    public partial class StatusChipHost : VisualElement
    {
        public const string RootClass = "statusChipHost";
        public const string ChipClass = "statusChipHost__chip";
        public const string ChipShownClass = "statusChipHost__chip--shown";

        private readonly Dictionary<string, StatusChip> chips = new();
        private readonly Dictionary<string, IVisualElementScheduledItem> pendingRemovals = new();
        private int lingerMs = 1500;

        public event Action<string> StatusResolved;

        public event Action<string> ChipClicked;

        [UxmlAttribute("linger-ms")]
        public int LingerMs
        {
            get => lingerMs;
            set => lingerMs = Mathf.Max(0, value);
        }

        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => resolvedStyle.paddingTop;
            set => style.paddingTop = Mathf.Max(0f, value);
        }

        public IReadOnlyCollection<string> Ids => chips.Keys;

        public StatusChipHost()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;
        }

        public bool TryGetChip(string id, out StatusChip chip) => chips.TryGetValue(id, out chip);

        public StatusChip ShowStatus(string id, string text, StatusState state)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A status needs an id.", nameof(id));
            }

            CancelRemoval(id);

            if (!chips.TryGetValue(id, out var chip))
            {
                chip = new StatusChip();
                chip.AddToClassList(ChipClass);
                chip.Clicked += () => ChipClicked?.Invoke(id);
                chips[id] = chip;
                Add(chip);
                chip.schedule.Execute(() => chip.AddToClassList(ChipShownClass));
            }
            else
            {
                chip.AddToClassList(ChipShownClass);
            }

            chip.Text = text;
            chip.State = state;
            return chip;
        }

        /// <summary>
        /// Removes the chip with this id. With a final state, the chip shows it for <see cref="LingerMs"/> first.
        /// </summary>
        /// <returns>False when no chip has this id.</returns>
        public bool Resolve(string id, StatusState? finalState = null, string finalText = null)
        {
            if (string.IsNullOrEmpty(id) || !chips.TryGetValue(id, out var chip))
            {
                return false;
            }

            if (finalState.HasValue)
            {
                chip.State = finalState.Value;
                if (finalText != null)
                {
                    chip.Text = finalText;
                }
            }

            CancelRemoval(id);
            var delay = finalState.HasValue ? lingerMs : 0;
            if (delay <= 0)
            {
                Remove(id);
            }
            else
            {
                pendingRemovals[id] = schedule.Execute(() => Remove(id)).StartingIn(delay);
            }

            return true;
        }

        public void ClearStatuses()
        {
            foreach (var id in new List<string>(chips.Keys))
            {
                Remove(id);
            }
        }

        private void CancelRemoval(string id)
        {
            if (pendingRemovals.TryGetValue(id, out var item))
            {
                item.Pause();
                pendingRemovals.Remove(id);
            }
        }

        private void Remove(string id)
        {
            CancelRemoval(id);
            if (!chips.TryGetValue(id, out var chip))
            {
                return;
            }

            chips.Remove(id);
            chip.RemoveFromClassList(ChipShownClass);
            chip.pickingMode = PickingMode.Ignore;

            var delayMs = chip.LongestTransitionMs();

            if (delayMs <= 0f || chip.panel == null)
            {
                chip.RemoveFromHierarchy();
            }
            else
            {
                chip.schedule.Execute(chip.RemoveFromHierarchy).StartingIn(Mathf.CeilToInt(delayMs));
            }

            StatusResolved?.Invoke(id);
        }
    }
}
