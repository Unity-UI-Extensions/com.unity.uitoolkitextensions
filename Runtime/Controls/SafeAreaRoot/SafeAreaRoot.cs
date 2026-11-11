/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The edges a <see cref="SafeAreaRoot"/> pads.
    /// </summary>
    [Flags]
    public enum SafeAreaEdges
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Sides = Left | Right,
        All = Top | Bottom | Left | Right,
    }

    /// <summary>
    /// Safe-area insets in panel pixels.
    /// </summary>
    public readonly struct SafeInsets : IEquatable<SafeInsets>
    {
        public readonly float Top;
        public readonly float Right;
        public readonly float Bottom;
        public readonly float Left;

        public SafeInsets(float top, float right, float bottom, float left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        public bool Equals(SafeInsets other) =>
            Mathf.Approximately(Top, other.Top) && Mathf.Approximately(Right, other.Right) &&
            Mathf.Approximately(Bottom, other.Bottom) && Mathf.Approximately(Left, other.Left);

        public override bool Equals(object obj) => obj is SafeInsets other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Top, Right, Bottom, Left);

        public override string ToString() => $"(top {Top}, right {Right}, bottom {Bottom}, left {Left})";
    }

    /// <summary>
    /// A container that pads itself by the device's safe area (notch, home indicator, rounded corners, side bars in
    /// landscape), so its children stay clear. Components that float, such as <see cref="OrnamentBar"/> or
    /// <see cref="ToastHost"/>, can take <see cref="Insets"/> for their own inset properties instead.
    /// </summary>
    /// <remarks>
    /// <para>The insets come from <c>Screen.safeArea</c>, converted to panel pixels, so the panel's scale mode counts.
    /// The control reads them on attach, on resize, and every <c>poll-ms</c>, because rotation does not always resize the
    /// panel at once. The default <c>poll-ms</c> is 500. Use 0 to stop polling. The default <c>edges</c> is All.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SafeAreaRoot : VisualElement
    {
        public const string RootClass = "safeAreaRoot";

        private SafeAreaEdges edges = SafeAreaEdges.All;
        private int pollMs = 500;
        private SafeInsets insets;
        private SafeInsets? overrideInsets;
        private DisplayedTimer poll;

        public event Action<SafeInsets> InsetsChanged;

        /// <summary>
        /// The insets in panel pixels, whichever edges <see cref="Edges"/> pads.
        /// </summary>
        public SafeInsets Insets => insets;

        [UxmlAttribute("edges")]
        public SafeAreaEdges Edges
        {
            get => edges;
            set
            {
                edges = value;
                ApplyPadding();
            }
        }

        [UxmlAttribute("poll-ms")]
        public int PollMs
        {
            get => pollMs;
            set
            {
                pollMs = Mathf.Max(0, value);
                StartPolling();
            }
        }

        public SafeAreaRoot()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Refresh();
                StartPolling();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => poll?.Stop());
            RegisterCallback<GeometryChangedEvent>(_ => Refresh());
        }

        /// <summary>
        /// Uses fixed insets instead of the screen's; null returns to the screen.
        /// </summary>
        public void OverrideInsets(SafeInsets? fixedInsets)
        {
            overrideInsets = fixedInsets;
            Refresh();
        }

        /// <summary>
        /// The safe-area insets of a runtime panel, in its pixels. Zero for editor panels.
        /// </summary>
        public static SafeInsets ComputeInsets(IPanel panel)
        {
            if (panel == null || panel.contextType != ContextType.Player)
            {
                return default;
            }

            var safe = Screen.safeArea;
            var width = Screen.width;
            var height = Screen.height;
            if (width <= 0 || height <= 0)
            {
                return default;
            }

            // Panel positions take a screen point with y down, so the safe rect is flipped first.
            var topLeft = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMin, height - safe.yMax));
            var bottomRight = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(safe.xMax, height - safe.yMin));
            var screen = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(width, height));
            return new SafeInsets(
                Mathf.Max(0f, topLeft.y),
                Mathf.Max(0f, screen.x - bottomRight.x),
                Mathf.Max(0f, screen.y - bottomRight.y),
                Mathf.Max(0f, topLeft.x));
        }

        private void StartPolling()
        {
            poll?.Stop();
            poll = null;
            if (pollMs > 0 && panel != null)
            {
                poll = DisplayedTimer.Start(this, Refresh, pollMs);
            }
        }

        private void Refresh()
        {
            var next = overrideInsets ?? ComputeInsets(panel);
            if (next.Equals(insets))
            {
                ApplyPadding();
                return;
            }

            insets = next;
            ApplyPadding();
            InsetsChanged?.Invoke(insets);
        }

        private void ApplyPadding()
        {
            style.paddingTop = (edges & SafeAreaEdges.Top) != 0 ? insets.Top : 0f;
            style.paddingBottom = (edges & SafeAreaEdges.Bottom) != 0 ? insets.Bottom : 0f;
            style.paddingLeft = (edges & SafeAreaEdges.Left) != 0 ? insets.Left : 0f;
            style.paddingRight = (edges & SafeAreaEdges.Right) != 0 ? insets.Right : 0f;
        }
    }
}
