/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum ToastTone
    {
        Info,
        Success,
        Warning,
        Error,
    }

    public enum ToastAnchor
    {
        Top,
        Bottom,
    }

    /// <summary>
    /// Why a toast left the screen.
    /// </summary>
    public enum ToastDismissReason
    {
        Timeout,
        Swiped,
        Tapped,
        Action,
        Programmatic,
    }

    /// <summary>
    /// What a <see cref="ToastHost"/> shows. Strings are display text; localise before passing them.
    /// </summary>
    public sealed class ToastOptions
    {
        public string Message;

        /// <summary>
        /// One optional action, such as "Undo" or "View". Null for none.
        /// </summary>
        public string ActionText;
        public Action OnAction;

        /// <summary>
        /// Time on screen in milliseconds, paused while the toast is touched. Negative (the default) uses the host
        /// default; 0 keeps the toast until it is dismissed.
        /// </summary>
        public int DurationMs = -1;

        /// <summary>
        /// A second line under the message, such as "1.6 of 4.2 MB". Null for none.
        /// </summary>
        public string Detail;

        /// <summary>
        /// A progress bar under the text, 0 to 1. Null for none.
        /// </summary>
        public float? Progress;

        /// <summary>
        /// An icon shown in an accent disc before the text. A default value for none.
        /// </summary>
        public Background Icon;

        /// <summary>
        /// A picture shown before the text in place of the icon disc, such as the photo just saved (FS08_3): a 56 px
        /// rounded square. A default value for none.
        /// </summary>
        public Background Thumbnail;

        /// <summary>
        /// Draws the action as an icon button, such as Share (FS08_3), instead of a text pill. <see cref="ActionText"/>
        /// is still required and becomes the button's tooltip.
        /// </summary>
        public Background ActionIcon;

        public ToastTone Tone = ToastTone.Info;

        /// <summary>
        /// Marks the action as an undo (<c>toastHost__toast--undo</c>).
        /// </summary>
        public bool IsUndo;
    }

    /// <summary>
    /// A shown or queued toast. Dismiss it early, or listen for when it leaves.
    /// </summary>
    public sealed class ToastHandle
    {
        private readonly ToastHost host;

        internal ToastHandle(ToastHost host, ToastOptions options)
        {
            this.host = host;
            Options = options;
        }

        public ToastOptions Options { get; }

        /// <summary>
        /// The toast's root element, or null while it is queued.
        /// </summary>
        public VisualElement Element { get; internal set; }

        internal Label MessageLabel;
        internal Label DetailLabel;
        internal VisualElement ProgressTrack;
        internal VisualElement ProgressFill;

        /// <summary>
        /// Updates the progress bar, showing it if needed; null hides it. Works while queued too.
        /// </summary>
        public void SetProgress(float? progress)
        {
            Options.Progress = progress.HasValue ? Mathf.Clamp01(progress.Value) : null;
            ToastHost.ApplyProgress(this);
        }

        /// <summary>
        /// Replaces the message and, when given, the detail line. Works while queued too.
        /// </summary>
        public void SetMessage(string message, string detail = null)
        {
            Options.Message = message;
            if (detail != null)
            {
                Options.Detail = detail;
            }

            ToastHost.ApplyTexts(this);
        }

        public bool IsVisible => Element != null && !IsDismissed;
        public bool IsDismissed { get; internal set; }

        /// <summary>
        /// Raised once when the toast is dismissed, including from the queue.
        /// </summary>
        public event Action<ToastDismissReason> Dismissed;

        public void Dismiss() => host.Dismiss(this, ToastDismissReason.Programmatic);

        internal void RaiseDismissed(ToastDismissReason reason) => Dismissed?.Invoke(reason);
    }

    /// <summary>
    /// Shows short messages in a band at the top or bottom of the screen: a queue, a stack of up to
    /// <see cref="MaxVisible"/>, one optional action (an undo variant included), auto-dismiss, tap and
    /// swipe to dismiss through <see cref="ToastSwipeDismissManipulator"/>.
    /// </summary>
    /// <remarks>
    /// <para>The host fills its parent as an overlay layer; only the toasts take input.</para>
    /// <para>Props: <c>max-visible</c>, <c>anchor</c>, <c>default-duration-ms</c>, <c>top-inset</c>,
    /// <c>bottom-inset</c> (safe area). Methods: <see cref="Show(ToastOptions)"/>,
    /// <see cref="Show(string, string, Action, int)"/>, <see cref="ShowUndo"/>, <see cref="DismissAll"/>.
    /// Events: <see cref="ToastShown"/>, <see cref="ToastDismissed"/>, <see cref="ActionInvoked"/>.</para>
    /// <para>A toast can carry a detail line, an icon in an accent disc and a progress bar, and its handle updates
    /// them in place (<see cref="ToastHandle.SetMessage"/>, <see cref="ToastHandle.SetProgress"/>), for example for a
    /// download. <see cref="ToastOptions.DurationMs"/> 0 keeps a toast until it is dismissed.</para>
    /// <para>USS classes: <c>toastHost</c> (root), <c>toastHost__toast</c>, <c>toastHost__card</c>, <c>toastHost__icon</c>,
    /// <c>toastHost__iconImage</c>, <c>toastHost__texts</c>, <c>toastHost__message</c>, <c>toastHost__detail</c>,
    /// <c>toastHost__progress</c>, <c>toastHost__progressFill</c>, <c>toastHost__action</c>, <c>toastHost__thumbnail</c>,
    /// <c>toastHost__actionIcon</c>.</para>
    /// <para>Modifier classes: <c>toastHost--top</c>, <c>toastHost--bottom</c>; on a toast
    /// <c>toastHost__toast--shown</c>, <c>toastHost__toast--action</c>, <c>toastHost__toast--undo</c>,
    /// <c>toastHost__toast--success</c>, <c>toastHost__toast--warning</c>, <c>toastHost__toast--error</c>,
    /// <c>toastHost__toast--progress</c>, <c>toastHost__toast--hasIcon</c>, <c>toastHost__toast--hasThumbnail</c>,
    /// <c>toastHost__toast--iconAction</c>.</para>
    /// <para>Tokens read: <c>--surface-bg</c>, <c>--surface-fg</c>, <c>--surface-border</c>,
    /// <c>--surface-accent</c>, <c>--c-success</c>, <c>--c-warn</c>, <c>--c-danger</c>, <c>--radius-card</c>,
    /// <c>--space-2..4</c>, <c>--text-label</c>, <c>--font-body</c>, <c>--font-body-semibold</c>,
    /// <c>--motion-base</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class ToastHost : VisualElement
    {
        public const string RootClass = "toastHost";
        public const string TopClass = "toastHost--top";
        public const string BottomClass = "toastHost--bottom";
        public const string ToastClass = "toastHost__toast";
        public const string CardClass = "toastHost__card";
        public const string IconClass = "toastHost__icon";
        public const string IconImageClass = "toastHost__iconImage";
        public const string TextsClass = "toastHost__texts";
        public const string MessageClass = "toastHost__message";
        public const string DetailClass = "toastHost__detail";
        public const string ProgressClass = "toastHost__progress";
        public const string ProgressFillClass = "toastHost__progressFill";
        public const string ProgressModifierClass = "toastHost__toast--progress";
        public const string HasIconClass = "toastHost__toast--hasIcon";
        public const string ActionClass = "toastHost__action";
        public const string ShownClass = "toastHost__toast--shown";
        public const string HasActionClass = "toastHost__toast--action";
        public const string UndoClass = "toastHost__toast--undo";
        public const string SuccessClass = "toastHost__toast--success";
        public const string WarningClass = "toastHost__toast--warning";
        public const string ErrorClass = "toastHost__toast--error";
        public const string ThumbnailClass = "toastHost__thumbnail";
        public const string HasThumbnailClass = "toastHost__toast--hasThumbnail";
        public const string ActionIconClass = "toastHost__actionIcon";
        public const string IconActionClass = "toastHost__toast--iconAction";

        /// <summary>
        /// Undo toasts stay longer than plain ones so the action can be reached.
        /// </summary>
        public const int UndoDurationMs = 6000;

        private readonly List<ToastHandle> visibleToasts = new();
        private readonly Queue<ToastHandle> queued = new();
        private readonly Dictionary<ToastHandle, IVisualElementScheduledItem> timers = new();

        private int maxVisible = 3;
        private ToastAnchor anchor = ToastAnchor.Top;
        private int defaultDurationMs = 4000;

        public event Action<ToastHandle> ToastShown;
        public event Action<ToastHandle, ToastDismissReason> ToastDismissed;
        public event Action<ToastHandle> ActionInvoked;

        /// <summary>
        /// Toasts on screen, oldest first.
        /// </summary>
        public IReadOnlyList<ToastHandle> Visible => visibleToasts;

        public int QueuedCount => queued.Count;

        [UxmlAttribute("max-visible")]
        public int MaxVisible
        {
            get => maxVisible;
            set
            {
                maxVisible = Mathf.Max(1, value);
                PumpQueue();
            }
        }

        [UxmlAttribute("anchor")]
        public ToastAnchor Anchor
        {
            get => anchor;
            set
            {
                anchor = value;
                EnableInClassList(TopClass, anchor == ToastAnchor.Top);
                EnableInClassList(BottomClass, anchor == ToastAnchor.Bottom);
            }
        }

        [UxmlAttribute("default-duration-ms")]
        public int DefaultDurationMs
        {
            get => defaultDurationMs;
            set => defaultDurationMs = Mathf.Max(500, value);
        }

        /// <summary>
        /// Top safe-area inset, added above the stack.
        /// </summary>
        [UxmlAttribute("top-inset")]
        public float TopInset
        {
            get => resolvedStyle.paddingTop;
            set => style.paddingTop = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Bottom safe-area inset, added below the stack.
        /// </summary>
        [UxmlAttribute("bottom-inset")]
        public float BottomInset
        {
            get => resolvedStyle.paddingBottom;
            set => style.paddingBottom = Mathf.Max(0f, value);
        }

        public ToastHost()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;
            Anchor = ToastAnchor.Top;

            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                if (evt.target == this)
                {
                    DismissAll();
                }
            });
        }

        public ToastHandle Show(string message, string actionText = null, Action onAction = null, int durationMs = -1)
        {
            return Show(new ToastOptions { Message = message, ActionText = actionText, OnAction = onAction, DurationMs = durationMs });
        }

        /// <summary>
        /// Shows a toast with an undo action that stays for <see cref="UndoDurationMs"/>.
        /// </summary>
        public ToastHandle ShowUndo(string message, Action onUndo, string undoText = "Undo")
        {
            return Show(new ToastOptions { Message = message, ActionText = undoText, OnAction = onUndo, DurationMs = UndoDurationMs, IsUndo = true });
        }

        /// <summary>
        /// Shows a toast now, or queues it while <see cref="MaxVisible"/> toasts are on screen.
        /// </summary>
        public ToastHandle Show(ToastOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var handle = new ToastHandle(this, options);
            if (visibleToasts.Count >= maxVisible)
            {
                queued.Enqueue(handle);
            }
            else
            {
                Present(handle);
            }

            return handle;
        }

        public void DismissAll()
        {
            while (queued.Count > 0)
            {
                var handle = queued.Dequeue();
                handle.IsDismissed = true;
                handle.RaiseDismissed(ToastDismissReason.Programmatic);
                ToastDismissed?.Invoke(handle, ToastDismissReason.Programmatic);
            }

            foreach (var handle in visibleToasts.ToArray())
            {
                Dismiss(handle, ToastDismissReason.Programmatic);
            }
        }

        internal void Dismiss(ToastHandle handle, ToastDismissReason reason)
        {
            if (handle == null || handle.IsDismissed)
            {
                return;
            }

            handle.IsDismissed = true;
            StopTimer(handle);

            if (!visibleToasts.Remove(handle))
            {
                // Still queued: it never appeared.
                var remaining = new Queue<ToastHandle>();
                foreach (var item in queued)
                {
                    if (item != handle)
                    {
                        remaining.Enqueue(item);
                    }
                }

                queued.Clear();
                foreach (var item in remaining)
                {
                    queued.Enqueue(item);
                }
            }
            else
            {
                Retire(handle.Element, swiped: reason == ToastDismissReason.Swiped);
            }

            handle.RaiseDismissed(reason);
            ToastDismissed?.Invoke(handle, reason);
            PumpQueue();
        }

        private void PumpQueue()
        {
            while (visibleToasts.Count < maxVisible && queued.Count > 0)
            {
                Present(queued.Dequeue());
            }
        }

        private void Present(ToastHandle handle)
        {
            var options = handle.Options;

            var toast = UIToolkitExtensions.CreateVisualElement(ToastClass);
            toast.EnableInClassList(SuccessClass, options.Tone == ToastTone.Success);
            toast.EnableInClassList(WarningClass, options.Tone == ToastTone.Warning);
            toast.EnableInClassList(ErrorClass, options.Tone == ToastTone.Error);
            toast.EnableInClassList(UndoClass, options.IsUndo);

            var card = UIToolkitExtensions.CreateVisualElement(toast, CardClass);

            var hasThumbnail = HasImage(options.Thumbnail);
            var hasIcon = !hasThumbnail && HasImage(options.Icon);
            toast.EnableInClassList(HasIconClass, hasIcon);
            toast.EnableInClassList(HasThumbnailClass, hasThumbnail);
            if (hasThumbnail)
            {
                var thumbnail = UIToolkitExtensions.CreateVisualElement(card, ThumbnailClass);
                thumbnail.pickingMode = PickingMode.Ignore;
                thumbnail.style.backgroundImage = new StyleBackground(options.Thumbnail);
            }
            else if (hasIcon)
            {
                var disc = UIToolkitExtensions.CreateVisualElement(card, IconClass);
                disc.pickingMode = PickingMode.Ignore;
                var image = UIToolkitExtensions.CreateVisualElement(disc, IconImageClass);
                image.pickingMode = PickingMode.Ignore;
                image.style.backgroundImage = new StyleBackground(options.Icon);
            }

            var texts = UIToolkitExtensions.CreateVisualElement(card, TextsClass);
            texts.pickingMode = PickingMode.Ignore;
            handle.MessageLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, MessageClass);
            handle.MessageLabel.pickingMode = PickingMode.Ignore;
            handle.DetailLabel = UIToolkitExtensions.CreateVisualElement<Label>(texts, DetailClass);
            handle.DetailLabel.pickingMode = PickingMode.Ignore;
            handle.ProgressTrack = UIToolkitExtensions.CreateVisualElement(texts, ProgressClass);
            handle.ProgressTrack.pickingMode = PickingMode.Ignore;
            handle.ProgressFill = UIToolkitExtensions.CreateVisualElement(handle.ProgressTrack, ProgressFillClass);
            handle.ProgressFill.AddManipulator(new GradientFill(GradientPreset.Brand, GradientDirection.Horizontal));

            if (!string.IsNullOrEmpty(options.ActionText))
            {
                toast.AddToClassList(HasActionClass);
                var iconAction = HasImage(options.ActionIcon);
                toast.EnableInClassList(IconActionClass, iconAction);
                var action = UIToolkitExtensions.CreateButton(card, iconAction ? string.Empty : options.ActionText, () => OnAction(handle), className: ActionClass);
                action.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation(), TrickleDown.TrickleDown);
                action.MakePill();
                if (iconAction)
                {
                    action.tooltip = options.ActionText;
                    var glyph = UIToolkitExtensions.CreateVisualElement(action, ActionIconClass);
                    glyph.pickingMode = PickingMode.Ignore;
                    glyph.style.backgroundImage = new StyleBackground(options.ActionIcon);
                }
            }

            card.AddManipulator(new ToastSwipeDismissManipulator(
                canInteract: () => !handle.IsDismissed,
                canStartAtPosition: position => !IsOverAction(card, position),
                getHorizontalDismissTravelDistance: () => card.resolvedStyle.width > 0 ? card.resolvedStyle.width + 32f : 400f,
                getVerticalDismissTravelDistance: () => card.resolvedStyle.height > 0 ? card.resolvedStyle.height + 16f : 80f,
                getOffsetTarget: () => card,
                onInteractionStarted: () => StopTimer(handle),
                onInteractionAborted: () => StartTimer(handle),
                onTapped: () => Dismiss(handle, ToastDismissReason.Tapped),
                onDismissed: () => Dismiss(handle, ToastDismissReason.Swiped)));

            handle.Element = toast;
            ApplyTexts(handle);
            ApplyProgress(handle);
            visibleToasts.Add(handle);

            if (anchor == ToastAnchor.Top)
            {
                Insert(0, toast);
            }
            else
            {
                Add(toast);
            }

            // A frame later, so the entrance transition starts from the hidden values.
            toast.schedule.Execute(() =>
            {
                if (!handle.IsDismissed)
                {
                    toast.AddToClassList(ShownClass);
                }
            });

            StartTimer(handle);
            ToastShown?.Invoke(handle);
        }

        internal static void ApplyTexts(ToastHandle handle)
        {
            if (handle.MessageLabel == null)
            {
                return;
            }

            handle.MessageLabel.text = handle.Options.Message ?? string.Empty;
            handle.DetailLabel.text = handle.Options.Detail ?? string.Empty;
            handle.DetailLabel.style.display = string.IsNullOrEmpty(handle.Options.Detail) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        internal static void ApplyProgress(ToastHandle handle)
        {
            if (handle.ProgressTrack == null)
            {
                return;
            }

            var progress = handle.Options.Progress;
            handle.ProgressTrack.style.display = progress.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            handle.Element.EnableInClassList(ProgressModifierClass, progress.HasValue);
            handle.ProgressFill.style.width = Length.Percent((progress ?? 0f) * 100f);
        }

        private static bool HasImage(Background image) =>
            image.texture != null || image.sprite != null || image.vectorImage != null || image.renderTexture != null;

        private static bool IsOverAction(VisualElement card, Vector2 position)
        {
            var action = card.Q(className: ActionClass);
            return action != null && action.worldBound.Contains(position);
        }

        private void OnAction(ToastHandle handle)
        {
            if (handle.IsDismissed)
            {
                return;
            }

            handle.Options.OnAction?.Invoke();
            ActionInvoked?.Invoke(handle);
            Dismiss(handle, ToastDismissReason.Action);
        }

        private void StartTimer(ToastHandle handle)
        {
            StopTimer(handle);
            if (handle.IsDismissed || handle.Element == null)
            {
                return;
            }

            var duration = handle.Options.DurationMs < 0 ? defaultDurationMs : handle.Options.DurationMs;
            if (duration == 0)
            {
                // Stays until dismissed.
                return;
            }

            timers[handle] = handle.Element.schedule.Execute(() => Dismiss(handle, ToastDismissReason.Timeout)).StartingIn(duration);
        }

        private void StopTimer(ToastHandle handle)
        {
            if (timers.TryGetValue(handle, out var timer))
            {
                timer.Pause();
                timers.Remove(handle);
            }
        }

        /// <summary>
        /// Fades the toast out (a swiped toast has already left) and removes it after the transition.
        /// </summary>
        private static void Retire(VisualElement toast, bool swiped)
        {
            if (toast == null)
            {
                return;
            }

            toast.pickingMode = PickingMode.Ignore;
            toast.RemoveFromClassList(ShownClass);

            var delayMs = 0f;
            if (!swiped)
            {
                foreach (var duration in toast.resolvedStyle.transitionDuration)
                {
                    delayMs = Mathf.Max(delayMs, duration.unit == TimeUnit.Second ? duration.value * 1000f : duration.value);
                }
            }

            if (delayMs <= 0f || toast.panel == null)
            {
                toast.RemoveFromHierarchy();
            }
            else
            {
                toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(Mathf.CeilToInt(delayMs));
            }
        }
    }
}
