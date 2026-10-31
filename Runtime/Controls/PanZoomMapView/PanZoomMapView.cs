/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// How a stop on a <see cref="PanZoomMapView"/> is drawn.
    /// </summary>
    public enum MapStopState
    {
        /// <summary>
        /// Not visited yet: a white disc with a muted ring.
        /// </summary>
        Pending,

        /// <summary>
        /// The next stop: a white disc with a strong ring.
        /// </summary>
        Next,

        /// <summary>
        /// Visited: a filled disc.
        /// </summary>
        Visited,
    }

    /// <summary>
    /// One numbered stop on a <see cref="PanZoomMapView"/>. Labels are display text.
    /// </summary>
    public sealed class MapStop
    {
        public string Id;

        /// <summary>
        /// Shown in the pin, such as "5".
        /// </summary>
        public string Label;

        /// <summary>
        /// Position on the map image, 0 to 1 from the top-left corner.
        /// </summary>
        public Vector2 Position;

        public MapStopState State;

        public MapStop() { }

        public MapStop(string id, string label, Vector2 position, MapStopState state = MapStopState.Pending)
        {
            Id = id;
            Label = label;
            Position = position;
            State = state;
        }
    }

    /// <summary>
    /// A map image that pans and zooms (drag, pinch, mouse wheel), with numbered stop pins, a dashed route and a
    /// you-are-here dot with an optional heading (FS09_1).
    /// </summary>
    /// <remarks>
    /// <para>The image is any <see cref="Background"/>: a floor plan, a venue photo or a rendered map. Positions are
    /// normalised to the image, 0 to 1 from its top-left corner, so they survive a change of image size. At zoom 1 the
    /// whole image fits the view. Pins and the route keep their size while the image scales.</para>
    /// <para>Methods: <see cref="SetImage"/>, <see cref="SetStops"/>, <see cref="SetStopState"/>, <see cref="SetRoute"/>
    /// (stop ids in order), <see cref="SetRoutePath"/> (free points), <see cref="SetYouAreHere"/>,
    /// <see cref="ClearYouAreHere"/>, <see cref="FocusOn"/>, <see cref="Select"/>, <see cref="Recentre"/>,
    /// <see cref="ZoomBy"/>, <see cref="NormalisedToLocal"/>, <see cref="LocalToNormalised"/>.
    /// Events: <see cref="StopTapped"/> (id), <see cref="MapTapped"/> (normalised point), <see cref="ViewChanged"/>.
    /// Props: <c>min-zoom</c> (1), <c>max-zoom</c> (4), <c>focus-zoom</c> (2), <c>show-controls</c>.</para>
    /// <para>A tap selects nothing by itself; the app calls <see cref="Select"/> or <see cref="FocusOn"/> from
    /// <see cref="StopTapped"/>, for example to show a stop card. Arrow keys pan and plus and minus zoom when focused.</para>
    /// <para>USS classes: <c>panZoomMapView</c> (root), <c>panZoomMapView__image</c>, <c>panZoomMapView__overlay</c>,
    /// <c>panZoomMapView__pins</c>, <c>panZoomMapView__pin</c>, <c>panZoomMapView__pinLabel</c>,
    /// <c>panZoomMapView__controls</c>, <c>panZoomMapView__control</c>, <c>panZoomMapView__recentre</c>. Pin modifiers:
    /// <c>panZoomMapView__pin--pending</c>, <c>--next</c>, <c>--visited</c>, <c>--selected</c>.</para>
    /// <para>Tokens read: <c>--surface-tile</c>, <c>--surface-raised</c>, <c>--surface-fg</c>, <c>--surface-muted</c>,
    /// <c>--surface-border</c>, <c>--surface-accent</c>, <c>--c-navy-2</c>, <c>--c-primary</c>, <c>--c-warn</c>,
    /// <c>--c-on-primary</c>, <c>--c-page-card</c>, <c>--radius-card</c>, <c>--radius-pill</c>, <c>--space-2..3</c>,
    /// <c>--text-micro</c>, <c>--text-subtitle</c>, <c>--font-display</c>, <c>--motion-fast</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class PanZoomMapView : VisualElement
    {
        public const string RootClass = "panZoomMapView";
        public const string ImageClass = "panZoomMapView__image";
        public const string OverlayClass = "panZoomMapView__overlay";
        public const string PinsClass = "panZoomMapView__pins";
        public const string PinClass = "panZoomMapView__pin";
        public const string PinLabelClass = "panZoomMapView__pinLabel";
        public const string PendingPinClass = "panZoomMapView__pin--pending";
        public const string NextPinClass = "panZoomMapView__pin--next";
        public const string VisitedPinClass = "panZoomMapView__pin--visited";
        public const string SelectedPinClass = "panZoomMapView__pin--selected";
        public const string ControlsClass = "panZoomMapView__controls";
        public const string ControlClass = "panZoomMapView__control";
        public const string RecentreClass = "panZoomMapView__recentre";

        /// <summary>
        /// Pin diameter (FS09_1: r 13).
        /// </summary>
        public const float PinSize = 26f;

        /// <summary>
        /// Travel in pixels below which a press counts as a tap.
        /// </summary>
        public const float TapSlop = 8f;

        private static readonly CustomStyleProperty<Color> RouteColorProperty = new("--map-route");
        private static readonly CustomStyleProperty<Color> YouColorProperty = new("--map-you");
        private static readonly CustomStyleProperty<Color> YouRingColorProperty = new("--map-you-ring");

        private readonly VisualElement image;
        private readonly VisualElement overlay;
        private readonly VisualElement pins;
        private readonly VisualElement controls;
        private readonly List<MapStop> stops = new();
        private readonly Dictionary<string, VisualElement> pinElements = new();
        private readonly List<string> routeIds = new();
        private readonly List<Vector2> routePath = new();
        private readonly Dictionary<int, Vector2> pointers = new();

        private Vector2 imageSize = new(1000f, 1000f);
        private Vector2 fitSize;
        private Vector2 offset;
        private float zoom = 1f;
        private float minZoom = 1f;
        private float maxZoom = 4f;
        private float focusZoom = 2f;
        private bool hasYou;
        private Vector2 youPosition;
        private float? youHeading;
        private string selectedId;
        private bool showControls;
        private bool layoutReady;
        private Vector2 pendingCentre = new(0.5f, 0.5f);
        private float pressTravel;
        private bool multiTouch;
        private Color routeColor = new(0.24f, 0.51f, 0.6f);
        private Color youColor = new(0.91f, 0.69f, 0.29f);
        private Color youRingColor = Color.white;

        /// <summary>
        /// Raised when a stop pin is tapped: its id.
        /// </summary>
        public event Action<string> StopTapped;

        /// <summary>
        /// Raised when the map is tapped away from any pin: the normalised point.
        /// </summary>
        public event Action<Vector2> MapTapped;

        /// <summary>
        /// Raised after a pan or a zoom.
        /// </summary>
        public event Action ViewChanged;

        public IReadOnlyList<MapStop> Stops => stops;

        public string SelectedId => selectedId;

        public float Zoom => zoom;

        /// <summary>
        /// The normalised image point at the centre of the view.
        /// </summary>
        public Vector2 Centre => layoutReady ? LocalToNormalised(ViewSize * 0.5f) : pendingCentre;

        public bool HasYouAreHere => hasYou;

        public Vector2 YouAreHere => youPosition;

        [UxmlAttribute("min-zoom")]
        public float MinZoom
        {
            get => minZoom;
            set
            {
                minZoom = Mathf.Max(0.25f, value);
                maxZoom = Mathf.Max(maxZoom, minZoom);
                SetZoom(zoom, ViewSize * 0.5f);
            }
        }

        [UxmlAttribute("max-zoom")]
        public float MaxZoom
        {
            get => maxZoom;
            set
            {
                maxZoom = Mathf.Max(minZoom, value);
                SetZoom(zoom, ViewSize * 0.5f);
            }
        }

        /// <summary>
        /// The zoom <see cref="FocusOn"/> moves to when the map is zoomed out further.
        /// </summary>
        [UxmlAttribute("focus-zoom")]
        public float FocusZoom
        {
            get => focusZoom;
            set => focusZoom = Mathf.Max(1f, value);
        }

        /// <summary>
        /// Shows zoom in, zoom out and recentre buttons in the bottom-right corner.
        /// </summary>
        [UxmlAttribute("show-controls")]
        public bool ShowControls
        {
            get => showControls;
            set
            {
                showControls = value;
                controls.style.display = showControls ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public PanZoomMapView()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            focusable = true;
            tabIndex = 0;

            image = UIToolkitExtensions.CreateVisualElement(this, ImageClass);
            image.pickingMode = PickingMode.Ignore;
            image.style.transformOrigin = new TransformOrigin(0, 0);

            overlay = UIToolkitExtensions.CreateVisualElement(this, OverlayClass);
            overlay.pickingMode = PickingMode.Ignore;
            overlay.generateVisualContent += DrawOverlay;

            pins = UIToolkitExtensions.CreateVisualElement(this, PinsClass);
            pins.pickingMode = PickingMode.Ignore;

            controls = UIToolkitExtensions.CreateVisualElement(this, ControlsClass);
            CreateControl("+", "Zoom in", () => ZoomBy(1.5f));
            CreateControl("−", "Zoom out", () => ZoomBy(1f / 1.5f));
            var recentre = CreateControl(string.Empty, "Recentre", Recentre);
            recentre.AddToClassList(RecentreClass);
            recentre.generateVisualContent += DrawRecentreGlyph;

            RegisterCallback<GeometryChangedEvent>(_ => OnGeometryChanged());
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(evt => EndPointer(evt.pointerId));
            RegisterCallback<PointerCaptureOutEvent>(evt => pointers.Remove(evt.pointerId));
            RegisterCallback<WheelEvent>(OnWheel);
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            ShowControls = false;
        }

        private Vector2 ViewSize => new(float.IsNaN(layout.width) ? 0f : layout.width, float.IsNaN(layout.height) ? 0f : layout.height);

        private Vector2 DisplaySize => fitSize * zoom;

        /// <summary>
        /// Sets the map image. <paramref name="size"/> is its size in pixels, needed for vector images and sprites
        /// whose size the background cannot give; it sets the aspect ratio. Keeps the stops, the route and the view centre.
        /// </summary>
        public void SetImage(Background background, Vector2 size = default)
        {
            image.style.backgroundImage = new StyleBackground(background);
            if (size.x > 0f && size.y > 0f)
            {
                imageSize = size;
            }
            else if (background.texture != null)
            {
                imageSize = new Vector2(background.texture.width, background.texture.height);
            }
            else if (background.sprite != null)
            {
                imageSize = background.sprite.rect.size;
            }
            else if (background.renderTexture != null)
            {
                imageSize = new Vector2(background.renderTexture.width, background.renderTexture.height);
            }

            var centre = Centre;
            UpdateFit();
            CentreOn(centre);
        }

        public void SetStops(IReadOnlyList<MapStop> newStops)
        {
            stops.Clear();
            pinElements.Clear();
            pins.Clear();

            if (newStops != null)
            {
                foreach (var stop in newStops)
                {
                    stops.Add(stop);
                    var pin = UIToolkitExtensions.CreateVisualElement(pins, PinClass);
                    pin.pickingMode = PickingMode.Ignore;
                    var label = UIToolkitExtensions.CreateVisualElement<Label>(pin, PinLabelClass);
                    label.text = stop.Label ?? string.Empty;
                    label.pickingMode = PickingMode.Ignore;
                    pinElements[stop.Id] = pin;
                    ApplyPinState(stop);
                }
            }

            if (selectedId != null && !pinElements.ContainsKey(selectedId))
            {
                selectedId = null;
            }

            ApplySelection();
            UpdatePins();
            overlay.MarkDirtyRepaint();
        }

        public void SetStopState(string id, MapStopState state)
        {
            var stop = stops.Find(s => s.Id == id);
            if (stop != null)
            {
                stop.State = state;
                ApplyPinState(stop);
            }
        }

        /// <summary>
        /// The pin element for a stop, or null.
        /// </summary>
        public VisualElement PinElement(string id) => id != null && pinElements.TryGetValue(id, out var pin) ? pin : null;

        /// <summary>
        /// Draws a dashed route through these stops, in order. Null or empty clears it.
        /// </summary>
        public void SetRoute(IReadOnlyList<string> ids)
        {
            routeIds.Clear();
            routePath.Clear();
            if (ids != null)
            {
                routeIds.AddRange(ids);
            }

            overlay.MarkDirtyRepaint();
        }

        /// <summary>
        /// Draws a dashed route through free normalised points, such as a walking path. Null or empty clears it.
        /// </summary>
        public void SetRoutePath(IReadOnlyList<Vector2> points)
        {
            routeIds.Clear();
            routePath.Clear();
            if (points != null)
            {
                routePath.AddRange(points);
            }

            overlay.MarkDirtyRepaint();
        }

        /// <summary>
        /// Shows the you-are-here dot at a normalised point, with a heading cone when a heading is given (degrees clockwise from up).
        /// </summary>
        public void SetYouAreHere(Vector2 position, float? headingDegrees = null)
        {
            hasYou = true;
            youPosition = position;
            youHeading = headingDegrees;
            overlay.MarkDirtyRepaint();
        }

        public void ClearYouAreHere()
        {
            hasYou = false;
            overlay.MarkDirtyRepaint();
        }

        /// <summary>
        /// Marks a stop as selected (a larger pin with an accent ring); null clears it.
        /// </summary>
        public void Select(string id)
        {
            selectedId = id != null && pinElements.ContainsKey(id) ? id : null;
            ApplySelection();
        }

        /// <summary>
        /// Centres a stop, zooms in to at least <see cref="FocusZoom"/> and selects it.
        /// </summary>
        public void FocusOn(string id)
        {
            var stop = stops.Find(s => s.Id == id);
            if (stop == null)
            {
                return;
            }

            Select(id);
            SetZoomOnly(Mathf.Max(zoom, focusZoom));
            CentreOn(stop.Position);
        }

        /// <summary>
        /// Centres the you-are-here dot, or shows the whole map when there is none.
        /// </summary>
        public void Recentre()
        {
            if (hasYou)
            {
                CentreOn(youPosition);
            }
            else
            {
                SetZoomOnly(minZoom);
                CentreOn(new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>
        /// Multiplies the zoom about the centre of the view.
        /// </summary>
        public void ZoomBy(float factor) => SetZoom(zoom * factor, ViewSize * 0.5f);

        /// <summary>
        /// Centres a normalised point, as far as the image edges allow.
        /// </summary>
        public void CentreOn(Vector2 normalised)
        {
            if (!layoutReady)
            {
                pendingCentre = normalised;
                return;
            }

            offset = (ViewSize * 0.5f) - Vector2.Scale(normalised, DisplaySize);
            ApplyView();
        }

        /// <summary>
        /// Where a normalised image point is in the view's local space.
        /// </summary>
        public Vector2 NormalisedToLocal(Vector2 normalised) => offset + Vector2.Scale(normalised, DisplaySize);

        /// <summary>
        /// The normalised image point under a local position.
        /// </summary>
        public Vector2 LocalToNormalised(Vector2 local)
        {
            var display = DisplaySize;
            return new Vector2(display.x > 0f ? (local.x - offset.x) / display.x : 0.5f, display.y > 0f ? (local.y - offset.y) / display.y : 0.5f);
        }

        private Button CreateControl(string text, string tooltipText, Action action)
        {
            var button = UIToolkitExtensions.CreateButton(controls, text, action, className: ControlClass);
            button.tooltip = tooltipText;
            return button;
        }

        private void OnGeometryChanged()
        {
            var centre = Centre;
            UpdateFit();
            layoutReady = ViewSize.x > 0f && ViewSize.y > 0f;
            if (layoutReady)
            {
                CentreOn(centre);
            }
        }

        private void UpdateFit()
        {
            var view = ViewSize;
            if (view.x <= 0f || view.y <= 0f || imageSize.x <= 0f || imageSize.y <= 0f)
            {
                return;
            }

            var scale = Mathf.Min(view.x / imageSize.x, view.y / imageSize.y);
            fitSize = imageSize * scale;
            image.style.width = fitSize.x;
            image.style.height = fitSize.y;
        }

        private void SetZoomOnly(float value)
        {
            zoom = Mathf.Clamp(value, minZoom, maxZoom);
        }

        private void SetZoom(float value, Vector2 aboutLocal)
        {
            var anchor = LocalToNormalised(aboutLocal);
            SetZoomOnly(value);
            if (!layoutReady)
            {
                return;
            }

            offset = aboutLocal - Vector2.Scale(anchor, DisplaySize);
            ApplyView();
        }

        private void ApplyView()
        {
            var view = ViewSize;
            var display = DisplaySize;
            offset.x = display.x <= view.x ? (view.x - display.x) * 0.5f : Mathf.Clamp(offset.x, view.x - display.x, 0f);
            offset.y = display.y <= view.y ? (view.y - display.y) * 0.5f : Mathf.Clamp(offset.y, view.y - display.y, 0f);

            image.style.translate = new Translate(offset.x, offset.y);
            image.style.scale = new Scale(new Vector3(zoom, zoom, 1f));
            UpdatePins();
            overlay.MarkDirtyRepaint();
            ViewChanged?.Invoke();
        }

        private void UpdatePins()
        {
            foreach (var stop in stops)
            {
                if (pinElements.TryGetValue(stop.Id, out var pin))
                {
                    var local = NormalisedToLocal(stop.Position);
                    pin.style.translate = new Translate(local.x - (PinSize * 0.5f), local.y - (PinSize * 0.5f));
                }
            }
        }

        private void ApplyPinState(MapStop stop)
        {
            if (!pinElements.TryGetValue(stop.Id, out var pin))
            {
                return;
            }

            pin.EnableInClassList(PendingPinClass, stop.State == MapStopState.Pending);
            pin.EnableInClassList(NextPinClass, stop.State == MapStopState.Next);
            pin.EnableInClassList(VisitedPinClass, stop.State == MapStopState.Visited);
        }

        private void ApplySelection()
        {
            foreach (var pair in pinElements)
            {
                pair.Value.EnableInClassList(SelectedPinClass, pair.Key == selectedId);
                if (pair.Key == selectedId)
                {
                    pair.Value.BringToFront();
                }
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.target is VisualElement target && controls.Contains(target))
            {
                return;
            }

            if (pointers.Count == 0)
            {
                pressTravel = 0f;
                multiTouch = false;
            }

            pointers[evt.pointerId] = evt.localPosition;
            multiTouch |= pointers.Count > 1;
            this.CapturePointer(evt.pointerId);
            Focus();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!pointers.TryGetValue(evt.pointerId, out var previous))
            {
                return;
            }

            var current = (Vector2)evt.localPosition;
            if (pointers.Count == 1)
            {
                var delta = current - previous;
                pressTravel += delta.magnitude;
                pointers[evt.pointerId] = current;
                if (pressTravel > TapSlop)
                {
                    offset += delta;
                    ApplyView();
                }

                return;
            }

            // Pinch: zoom by the change in spread about the midpoint, and pan with the midpoint.
            var other = Vector2.zero;
            foreach (var pair in pointers)
            {
                if (pair.Key != evt.pointerId)
                {
                    other = pair.Value;
                    break;
                }
            }

            var oldMid = (previous + other) * 0.5f;
            var newMid = (current + other) * 0.5f;
            var oldSpread = Vector2.Distance(previous, other);
            var newSpread = Vector2.Distance(current, other);
            pointers[evt.pointerId] = current;
            if (oldSpread > 1f)
            {
                SetZoom(zoom * (newSpread / oldSpread), oldMid);
            }

            offset += newMid - oldMid;
            ApplyView();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!pointers.ContainsKey(evt.pointerId))
            {
                return;
            }

            var wasTap = pointers.Count == 1 && !multiTouch && pressTravel <= TapSlop;
            EndPointer(evt.pointerId);
            if (wasTap)
            {
                HandleTap(evt.localPosition);
            }
        }

        private void EndPointer(int pointerId)
        {
            pointers.Remove(pointerId);
            if (this.HasPointerCapture(pointerId))
            {
                this.ReleasePointer(pointerId);
            }
        }

        private void HandleTap(Vector2 local)
        {
            // The nearest pin within a 44 px touch target wins; the selected pin is checked first.
            string hit = null;
            var best = 22f * 22f;
            foreach (var stop in stops)
            {
                var distance = (NormalisedToLocal(stop.Position) - local).sqrMagnitude;
                if (distance < best || (distance <= 22f * 22f && stop.Id == selectedId))
                {
                    best = distance;
                    hit = stop.Id;
                    if (stop.Id == selectedId)
                    {
                        break;
                    }
                }
            }

            if (hit != null)
            {
                StopTapped?.Invoke(hit);
            }
            else
            {
                MapTapped?.Invoke(LocalToNormalised(local));
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            var factor = evt.delta.y > 0f ? 1f / 1.15f : 1.15f;
            SetZoom(zoom * factor, evt.localMousePosition);
            evt.StopPropagation();
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            const float step = 48f;
            var delta = evt.direction switch
            {
                NavigationMoveEvent.Direction.Left => new Vector2(step, 0f),
                NavigationMoveEvent.Direction.Right => new Vector2(-step, 0f),
                NavigationMoveEvent.Direction.Up => new Vector2(0f, step),
                NavigationMoveEvent.Direction.Down => new Vector2(0f, -step),
                _ => Vector2.zero,
            };

            if (delta != Vector2.zero)
            {
                offset += delta;
                ApplyView();
                evt.StopPropagation();
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode is KeyCode.Plus or KeyCode.Equals or KeyCode.KeypadPlus)
            {
                ZoomBy(1.5f);
                evt.StopPropagation();
            }
            else if (evt.keyCode is KeyCode.Minus or KeyCode.KeypadMinus)
            {
                ZoomBy(1f / 1.5f);
                evt.StopPropagation();
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(RouteColorProperty, out var route))
            {
                routeColor = route;
            }

            if (evt.customStyle.TryGetValue(YouColorProperty, out var you))
            {
                youColor = you;
            }

            if (evt.customStyle.TryGetValue(YouRingColorProperty, out var ring))
            {
                youRingColor = ring;
            }

            overlay.MarkDirtyRepaint();
        }

        private void DrawOverlay(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            DrawRoute(painter);
            if (hasYou)
            {
                DrawYou(painter);
            }
        }

        private void DrawRoute(Painter2D painter)
        {
            var points = new List<Vector2>();
            if (routeIds.Count > 0)
            {
                foreach (var id in routeIds)
                {
                    var stop = stops.Find(s => s.Id == id);
                    if (stop != null)
                    {
                        points.Add(NormalisedToLocal(stop.Position));
                    }
                }
            }
            else
            {
                foreach (var point in routePath)
                {
                    points.Add(NormalisedToLocal(point));
                }
            }

            if (points.Count < 2)
            {
                return;
            }

            // FS09_1: 3 px, dashes of 8 with gaps of 6, carried across corners.
            const float dash = 8f;
            const float gap = 6f;
            painter.lineWidth = 3f;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = routeColor;
            painter.BeginPath();
            var phase = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                var length = Vector2.Distance(a, b);
                if (length <= 0f)
                {
                    continue;
                }

                var direction = (b - a) / length;
                var travelled = 0f;
                while (travelled < length)
                {
                    var inDash = phase < dash;
                    var step = Mathf.Min(inDash ? dash - phase : dash + gap - phase, length - travelled);
                    if (inDash)
                    {
                        painter.MoveTo(a + (direction * travelled));
                        painter.LineTo(a + (direction * (travelled + step)));
                    }

                    travelled += step;
                    phase = (phase + step) % (dash + gap);
                }
            }

            painter.Stroke();
        }

        private void DrawYou(Painter2D painter)
        {
            var centre = NormalisedToLocal(youPosition);

            if (youHeading.HasValue)
            {
                // A soft cone pointing along the heading, under the dot.
                var angle = youHeading.Value - 90f;
                var cone = youColor;
                cone.a = 0.35f;
                painter.fillColor = cone;
                painter.BeginPath();
                painter.MoveTo(centre);
                painter.Arc(centre, 30f, angle - 28f, angle + 28f);
                painter.ClosePath();
                painter.Fill();
            }

            // FS09_1: a halo of r 18 at half strength, then an amber dot of r 9 with a 3 px white ring.
            var halo = youColor;
            halo.a = 0.5f;
            painter.lineWidth = 2f;
            painter.strokeColor = halo;
            painter.BeginPath();
            painter.Arc(centre, 18f, 0f, 360f);
            painter.Stroke();

            painter.fillColor = youRingColor;
            painter.BeginPath();
            painter.Arc(centre, 10.5f, 0f, 360f);
            painter.Fill();

            painter.fillColor = youColor;
            painter.BeginPath();
            painter.Arc(centre, 7.5f, 0f, 360f);
            painter.Fill();
        }

        private static void DrawRecentreGlyph(MeshGenerationContext context)
        {
            var element = context.visualElement;
            var centre = element.contentRect.center;
            var painter = context.painter2D;
            painter.strokeColor = element.resolvedStyle.color;
            painter.fillColor = element.resolvedStyle.color;
            painter.lineWidth = 2f;
            painter.BeginPath();
            painter.Arc(centre, 6f, 0f, 360f);
            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(centre + new Vector2(0f, -10f));
            painter.LineTo(centre + new Vector2(0f, -6f));
            painter.MoveTo(centre + new Vector2(0f, 6f));
            painter.LineTo(centre + new Vector2(0f, 10f));
            painter.MoveTo(centre + new Vector2(-10f, 0f));
            painter.LineTo(centre + new Vector2(-6f, 0f));
            painter.MoveTo(centre + new Vector2(6f, 0f));
            painter.LineTo(centre + new Vector2(10f, 0f));
            painter.Stroke();
            painter.BeginPath();
            painter.Arc(centre, 2f, 0f, 360f);
            painter.Fill();
        }
    }
}
