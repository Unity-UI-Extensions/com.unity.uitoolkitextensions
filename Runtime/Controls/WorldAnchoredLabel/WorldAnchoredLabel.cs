/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// The colour of a <see cref="WorldAnchoredLabel"/>'s dot.
    /// </summary>
    public enum AnchoredLabelTone
    {
        /// <summary>
        /// Content in the world (<c>--c-primary</c>).
        /// </summary>
        Default,

        /// <summary>
        /// Something to do, such as "Tap to place" (<c>--c-accent</c>).
        /// </summary>
        Action,

        /// <summary>
        /// Done, such as "placed" (<c>--c-success</c>).
        /// </summary>
        Success,

        /// <summary>
        /// Needs attention (<c>--c-warn</c>).
        /// </summary>
        Warn,
    }

    /// <summary>
    /// A screen-space label that follows a point in the world: a dark pill with a coloured dot, a title and an
    /// optional detail such as a distance. It clamps to the safe area or hides when its point leaves the screen,
    /// fades with distance, and is a tap target.
    /// </summary>
    /// <remarks>
    /// <para>Put labels in a full-screen layer. Drive a label either by passing panel coordinates each frame
    /// (<see cref="SetPanelPosition"/>), or by giving it a camera and a transform to follow (<see cref="Track"/>),
    /// in which case it updates itself every frame and can write the distance into the detail.</para>
    /// <para>The class <c>worldAnchoredLabel--offscreen</c> marks a label clamped to the edge. The class <c>worldAnchoredLabel--hidden</c>
    /// marks a label that is off screen without clamping, behind the camera or beyond <c>max-distance</c>.</para>
    /// <para>The label always uses the deep palette because it sits over the camera.</para>
    /// </remarks>
    [UxmlElement]
    public partial class WorldAnchoredLabel : VisualElement
    {
        public const string RootClass = "worldAnchoredLabel";
        public const string DotClass = "worldAnchoredLabel__dot";
        public const string TextClass = "worldAnchoredLabel__text";
        public const string SeparatorClass = "worldAnchoredLabel__separator";
        public const string DetailClass = "worldAnchoredLabel__detail";
        public const string OffscreenClass = "worldAnchoredLabel--offscreen";
        public const string HiddenClass = "worldAnchoredLabel--hidden";

        /// <summary>
        /// Gap kept between a clamped label and the safe-area edge.
        /// </summary>
        public const float EdgeMargin = 8f;

        private readonly Label textLabel;
        private readonly Label separator;
        private readonly Label detailLabel;

        private AnchoredLabelTone tone;
        private bool clampToSafeArea = true;
        private float fadeStartDistance = 8f;
        private float maxDistance = 15f;
        private bool showDistance;
        private Vector2 pivot = new(0f, 0.5f);
        private Vector2 offset = new(12f, 0f);
        private Vector4 safeArea;

        private Camera trackCamera;
        private Transform trackTarget;
        private Vector3 trackWorldOffset;
        private DisplayedTimer trackItem;

        public event Action Tapped;

        public Vector2 AnchorPosition { get; private set; }

        public bool IsOffscreen => ClassListContains(OffscreenClass);
        public bool IsHidden => ClassListContains(HiddenClass);

        [UxmlAttribute("text")]
        public string Text
        {
            get => textLabel.text;
            set => textLabel.text = value ?? string.Empty;
        }

        /// <summary>
        /// A second part after a dot separator, such as "2 m" or "placed"; empty hides it.
        /// </summary>
        [UxmlAttribute("detail")]
        public string Detail
        {
            get => detailLabel.text;
            set
            {
                detailLabel.text = value ?? string.Empty;
                var has = !string.IsNullOrEmpty(value);
                detailLabel.SetDisplayed(has);
                separator.SetDisplayed(has);
            }
        }

        [UxmlAttribute("tone")]
        public AnchoredLabelTone Tone
        {
            get => tone;
            set
            {
                tone = value;
                EnableInClassList($"{RootClass}--action", tone == AnchoredLabelTone.Action);
                EnableInClassList($"{RootClass}--success", tone == AnchoredLabelTone.Success);
                EnableInClassList($"{RootClass}--warn", tone == AnchoredLabelTone.Warn);
            }
        }

        /// <summary>
        /// Keeps the label inside the safe area when its point leaves the screen. False hides it instead.
        /// </summary>
        [UxmlAttribute("clamp-to-safe-area")]
        public bool ClampToSafeArea
        {
            get => clampToSafeArea;
            set => clampToSafeArea = value;
        }

        /// <summary>
        /// Distance, in metres, at which the label starts to fade.
        /// </summary>
        [UxmlAttribute("fade-start-distance")]
        public float FadeStartDistance
        {
            get => fadeStartDistance;
            set => fadeStartDistance = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Distance, in metres, beyond which the label hides. 0 or less turns the range fade off.
        /// </summary>
        [UxmlAttribute("max-distance")]
        public float MaxDistance
        {
            get => maxDistance;
            set => maxDistance = value;
        }

        /// <summary>
        /// While tracking, write the distance into <see cref="Detail"/> ("2 m", "450 m", "1.2 km").
        /// </summary>
        [UxmlAttribute("show-distance")]
        public bool ShowDistance
        {
            get => showDistance;
            set => showDistance = value;
        }

        /// <summary>
        /// Which point of the label sits on the anchor, as a fraction of its size. (0, 0.5) is the left middle.
        /// </summary>
        [UxmlAttribute("pivot")]
        public Vector2 Pivot
        {
            get => pivot;
            set => pivot = value;
        }

        /// <summary>
        /// Panel-space offset from the anchor point to the pivot.
        /// </summary>
        [UxmlAttribute("offset")]
        public Vector2 Offset
        {
            get => offset;
            set => offset = value;
        }

        public WorldAnchoredLabel()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            this.MakePill();

            var dot = UIToolkitExtensions.CreateVisualElement(this, DotClass).IgnorePicking();
            textLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, TextClass).IgnorePicking();
            separator = UIToolkitExtensions.CreateVisualElement<Label>(this, SeparatorClass);
            separator.text = "·";
            separator.pickingMode = PickingMode.Ignore;
            detailLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, DetailClass).IgnorePicking();

            RegisterCallback<ClickEvent>(evt =>
            {
                if (!IsHidden)
                {
                    evt.StopPropagation();
                    Tapped?.Invoke();
                }
            });
            RegisterCallback<DetachFromPanelEvent>(_ => trackItem?.Stop());
            RegisterCallback<AttachToPanelEvent>(_ => { if (trackTarget != null) StartTrackItem(); });

            Detail = null;
            Tone = AnchoredLabelTone.Default;
        }

        /// <summary>
        /// Safe-area insets (left, top, right, bottom) in panel units, for clamping.
        /// </summary>
        public void SetSafeArea(float left, float top, float right, float bottom)
        {
            safeArea = new Vector4(Mathf.Max(0f, left), Mathf.Max(0f, top), Mathf.Max(0f, right), Mathf.Max(0f, bottom));
        }

        /// <summary>
        /// Places the label at a panel position. <paramref name="distance"/> in metres drives the range fade; negative skips it.
        /// <paramref name="behindCamera"/> hides the label (or clamps it, mirrored, to the nearest edge).
        /// </summary>
        public void SetPanelPosition(Vector2 panelPosition, float distance = -1f, bool behindCamera = false)
        {
            AnchorPosition = panelPosition;

            var host = parent != null ? parent.contentRect : new Rect(0f, 0f, float.MaxValue, float.MaxValue);
            var safe = new Rect(
                host.xMin + safeArea.x + EdgeMargin,
                host.yMin + safeArea.y + EdgeMargin,
                host.width - safeArea.x - safeArea.z - (2f * EdgeMargin),
                host.height - safeArea.y - safeArea.w - (2f * EdgeMargin));

            var onScreen = !behindCamera && safe.Contains(panelPosition);
            var outOfRange = maxDistance > 0f && distance >= 0f && distance > maxDistance;

            var hidden = outOfRange || (!onScreen && !clampToSafeArea);
            EnableInClassList(HiddenClass, hidden);
            EnableInClassList(OffscreenClass, !hidden && !onScreen);
            pickingMode = hidden ? PickingMode.Ignore : PickingMode.Position;

            style.opacity = hidden ? 0f : RangeOpacity(distance);
            if (hidden)
            {
                return;
            }

            var size = layout.size;
            if (float.IsNaN(size.x) || float.IsNaN(size.y))
            {
                size = Vector2.zero;
            }

            var topLeft = panelPosition + offset - Vector2.Scale(pivot, size);
            if (!onScreen)
            {
                if (behindCamera)
                {
                    // Behind the camera the projection is mirrored; push it to the edge it points towards.
                    topLeft = new Vector2(host.width - topLeft.x - size.x, host.height - topLeft.y - size.y);
                }

                topLeft.x = Mathf.Clamp(topLeft.x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - size.x));
                topLeft.y = Mathf.Clamp(topLeft.y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - size.y));
            }

            style.left = topLeft.x;
            style.top = topLeft.y;
        }

        /// <summary>
        /// Follows a transform through a camera, updating every frame until <see cref="StopTracking"/>.
        /// </summary>
        public void Track(Camera camera, Transform target, Vector3 worldOffset = default)
        {
            trackCamera = camera;
            trackTarget = target;
            trackWorldOffset = worldOffset;
            if (panel != null)
            {
                StartTrackItem();
                UpdateTracking();
            }
        }

        public void StopTracking()
        {
            trackItem?.Stop();
            trackItem = null;
            trackCamera = null;
            trackTarget = null;
        }

        /// <summary>
        /// Formats a distance the way the label shows it: "2 m", "450 m", "1.2 km".
        /// </summary>
        public static string FormatDistance(float metres)
        {
            if (metres < 1000f)
            {
                return $"{Mathf.Max(0, Mathf.RoundToInt(metres))} m";
            }

            return $"{metres / 1000f:0.0} km";
        }

        private float RangeOpacity(float distance)
        {
            if (distance < 0f || maxDistance <= 0f || distance <= fadeStartDistance)
            {
                return 1f;
            }

            var span = Mathf.Max(0.001f, maxDistance - fadeStartDistance);
            return Mathf.Clamp01(1f - ((distance - fadeStartDistance) / span));
        }

        private void StartTrackItem()
        {
            trackItem?.Stop();
            trackItem = DisplayedTimer.Start(this, UpdateTracking, 0);
        }

        internal void UpdateTracking()
        {
            if (trackCamera == null || trackTarget == null || panel == null)
            {
                return;
            }

            var world = trackTarget.position + trackWorldOffset;
            var toTarget = world - trackCamera.transform.position;
            var behind = Vector3.Dot(trackCamera.transform.forward, toTarget) <= 0f;
            var distance = toTarget.magnitude;
            if (showDistance)
            {
                Detail = FormatDistance(distance);
            }

            // Flip by the height of what the camera draws into (its texture or the screen), not always the screen:
            // CameraTransformWorldToPanel assumes the screen, which is wrong for a camera rendering to a texture.
            var screen = trackCamera.WorldToScreenPoint(world);
            var targetHeight = trackCamera.targetTexture != null ? trackCamera.targetTexture.height : Screen.height;
            var position = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, targetHeight - screen.y));
            if (parent != null)
            {
                position = parent.WorldToLocal(position);
            }

            SetPanelPosition(position, distance, behind);
        }
    }
}
