/// Credit SimonDarksideJ  

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityUIToolkit.Extensions
{
	/// <summary>
	/// A paging (snap) scroller that hosts page-sized children and snaps to a page on release.
	///
	/// Requirements:
	/// - Orientation can be Horizontal (default) or Vertical.
	/// - PageSize defaults to the control's resolved width/height (based on orientation).
	/// - Child count determines scroll content length.
	/// - Supports paging with swipe gesture and always rests on a page.
	/// - Allows nested ScrollViews to handle the perpendicular axis.
	/// - Provides programmatic navigation (Next/Previous/GoToPage).
	/// - Manual movement can be enabled/disabled (children still receive events).
	/// - Exposes CurrentPageIndex.
	/// - Smooth movement with easing configurable via USS custom property:
	///   --scrollsnap-easing (default: Linear)
	///   Example:
	///     .scrollSnap { --scrollsnap-easing: OutCubic; }
	/// </summary>
	[UxmlElement]
	public sealed partial class ScrollSnap : VisualElement
	{
		public enum ScrollSnapOrientation
		{
			Horizontal,
			Vertical,
		}

		public const string RootClass = "scrollSnap";
		public const string ScrollViewClass = "scrollSnap__scroll";
		public const string ContentClass = "scrollSnap__content";
		public const string PageClass = "scrollSnap__page";

		private static readonly CustomStyleProperty<string> EasingStyleProperty = new("--scrollsnap-easing");
		private static readonly CustomStyleProperty<string> PagePaddingLeftStyleProperty = new("--scrollsnap-page-padding-left");
		private static readonly CustomStyleProperty<string> PagePaddingRightStyleProperty = new("--scrollsnap-page-padding-right");
		private static readonly CustomStyleProperty<string> PagePaddingTopStyleProperty = new("--scrollsnap-page-padding-top");
		private static readonly CustomStyleProperty<string> PagePaddingBottomStyleProperty = new("--scrollsnap-page-padding-bottom");

		private readonly ScrollView scrollView;
		private readonly VisualElement pages;

		private ScrollSnapOrientation orientation = ScrollSnapOrientation.Horizontal;
		private float explicitPageSize = 0f;
		private bool manualMovementEnabled = true;
		private float pagePaddingLeft;
		private float pagePaddingRight;
		private float pagePaddingTop;
		private float pagePaddingBottom;

		private bool isPointerDown;
		private bool isDragging;
		private int activePointerId;
		private Vector3 pointerStart;
		private float scrollOffsetStart;
		private int startPageIndex;

		private ValueAnimation<float> snapAnimation;
		private Func<float, float> easingCurve = Easing.Linear;
		private IVisualElementScheduledItem pendingSnap;
		private const long ScrollEndSnapDelayMs = 100;

		public event Action<int> PageChanged;

		public ScrollSnap()
		{
			AddToClassList(RootClass);

			scrollView = new ScrollView(ScrollViewMode.Horizontal);
			scrollView.AddToClassList(ScrollViewClass);
			scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

			scrollView.style.flexGrow = 1;
			scrollView.style.flexShrink = 1;
			scrollView.style.minWidth = 0;
			scrollView.style.minHeight = 0;

			hierarchy.Add(scrollView);

			pages = scrollView.contentContainer;
			pages.AddToClassList(ContentClass);
			pages.RegisterCallback<GeometryChangedEvent>(_ => RefreshPageSizing());

			UpdateOrientation(orientation);

			RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			RegisterCallback<GeometryChangedEvent>(_ => RefreshPageSizing());

			RegisterCallback<PointerDownEvent>(OnPointerDown);
			RegisterCallback<PointerMoveEvent>(OnPointerMove);
			RegisterCallback<PointerUpEvent>(OnPointerUp);
			RegisterCallback<PointerCancelEvent>(OnPointerCancel);

			// ScrollView can consume drag/wheel events. Hook into its events so we can snap
			// after the user scrolls it via trackpad/wheel/touch.
			scrollView.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);
			scrollView.RegisterCallback<PointerUpEvent>(OnScrollViewPointerUp, TrickleDown.TrickleDown);
			scrollView.RegisterCallback<PointerCancelEvent>(OnScrollViewPointerCancel, TrickleDown.TrickleDown);

		}

		public override VisualElement contentContainer => pages;

		[UxmlAttribute("orientation")]
		public ScrollSnapOrientation Orientation
		{
			get => orientation;
			set
			{
				if (orientation == value)
				{
					return;
				}

				orientation = value;
				UpdateOrientation(orientation);
				RefreshPageSizing();
				GoToPage(CurrentPageIndex, animate: false);
			}
		}

		/// <summary>
		/// Explicit page size in pixels (width for Horizontal, height for Vertical).
		/// Set to &lt;= 0 to use the control's resolved width/height.
		/// </summary>
		[UxmlAttribute("page-size")]
		public float PageSize
		{
			get => explicitPageSize;
			set
			{
				explicitPageSize = value;
				RefreshPageSizing();
				GoToPage(CurrentPageIndex, animate: false);
			}
		}

		[UxmlAttribute("manual-movement-enabled")]
		public bool ManualMovementEnabled
		{
			get => manualMovementEnabled;
			set => manualMovementEnabled = value;
		}

		/// <summary>
		/// Padding/gap around each page (in pixels). This is applied as margins on the page,
		/// while the page's size is reduced so the overall snap stride stays equal to PageSize.
		///
		/// USS custom properties (numbers, pixels):
		/// - --scrollsnap-page-padding-left
		/// - --scrollsnap-page-padding-right
		/// - --scrollsnap-page-padding-top
		/// - --scrollsnap-page-padding-bottom
		/// </summary>
		[UxmlAttribute("page-padding-left")]
		public float PagePaddingLeft
		{
			get => pagePaddingLeft;
			set
			{
				pagePaddingLeft = Mathf.Max(0f, value);
				RefreshPageSizing();
			}
		}

		[UxmlAttribute("page-padding-right")]
		public float PagePaddingRight
		{
			get => pagePaddingRight;
			set
			{
				pagePaddingRight = Mathf.Max(0f, value);
				RefreshPageSizing();
			}
		}

		[UxmlAttribute("page-padding-top")]
		public float PagePaddingTop
		{
			get => pagePaddingTop;
			set
			{
				pagePaddingTop = Mathf.Max(0f, value);
				RefreshPageSizing();
			}
		}

		[UxmlAttribute("page-padding-bottom")]
		public float PagePaddingBottom
		{
			get => pagePaddingBottom;
			set
			{
				pagePaddingBottom = Mathf.Max(0f, value);
				RefreshPageSizing();
			}
		}

		public int PageCount => pages?.childCount ?? 0;

		public int CurrentPageIndex { get; private set; }

		public void MoveNext(bool animate = true) => GoToPage(CurrentPageIndex + 1, animate);

		public void MovePrevious(bool animate = true) => GoToPage(CurrentPageIndex - 1, animate);

		public void GoToPage(int index, bool animate = true)
		{
			if (PageCount <= 0)
			{
				CurrentPageIndex = 0;
				SetScrollOffset(0f);
				return;
			}

			var clamped = Mathf.Clamp(index, 0, PageCount - 1);
			var pageSize = GetResolvedPageSize();
			if (pageSize <= 0f)
			{
				CurrentPageIndex = clamped;
				return;
			}

			StopSnapAnimation();
			CancelPendingSnap();

			var targetOffset = clamped * pageSize;
			if (!animate)
			{
				SetScrollOffset(targetOffset);
				SetCurrentPage(clamped);
				return;
			}

			var start = GetScrollOffset();

			// Duration can be driven by USS later if needed; keeping minimal/default smooth behavior.
			snapAnimation = this.experimental.animation.Start(start, targetOffset, 320, (_, v) => SetScrollOffset(v));
			snapAnimation.easingCurve = easingCurve ?? Easing.Linear;
			snapAnimation.KeepAlive();

			snapAnimation.onAnimationCompleted += () =>
			{
				StopSnapAnimation();
				SetScrollOffset(targetOffset);
				SetCurrentPage(clamped);
			};
		}

		private void SnapToNearestPage(bool animate)
		{
			var pageSize = GetResolvedPageSize();
			if (PageCount <= 0 || pageSize <= 0f)
			{
				return;
			}

			var raw = GetScrollOffset() / pageSize;
			var target = Mathf.RoundToInt(raw);
			GoToPage(target, animate);
		}

		private void ScheduleSnapToNearestPage(bool animate)
		{
			CancelPendingSnap();
			pendingSnap = schedule.Execute(() =>
			{
				pendingSnap = null;
				SnapToNearestPage(animate);
			}).StartingIn(ScrollEndSnapDelayMs);
		}

		private void CancelPendingSnap()
		{
			try
			{
				pendingSnap?.Pause();
			}
			catch { }
			finally
			{
				pendingSnap = null;
			}
		}

		private void SetCurrentPage(int index)
		{
			if (CurrentPageIndex == index)
			{
				return;
			}

			CurrentPageIndex = index;
			PageChanged?.Invoke(CurrentPageIndex);
		}

		private void StopSnapAnimation()
		{
			try
			{
				snapAnimation?.Stop();
			}
			catch { }
			finally
			{
				snapAnimation = null;
			}
		}

		private void UpdateOrientation(ScrollSnapOrientation newOrientation)
		{
			if (newOrientation == ScrollSnapOrientation.Horizontal)
			{
				scrollView.mode = ScrollViewMode.Horizontal;
				pages.style.flexDirection = FlexDirection.Row;
			}
			else
			{
				scrollView.mode = ScrollViewMode.Vertical;
				pages.style.flexDirection = FlexDirection.Column;
			}

			pages.style.flexWrap = Wrap.NoWrap;
		}

		private float GetResolvedPageSize()
		{
			if (explicitPageSize > 0f)
			{
				return explicitPageSize;
			}

			var size = orientation == ScrollSnapOrientation.Horizontal ? resolvedStyle.width : resolvedStyle.height;
			if (float.IsNaN(size) || size <= 0f)
			{
				return 0f;
			}

			return size;
		}

		private float GetScrollOffset()
		{
			var v = scrollView.scrollOffset;
			return orientation == ScrollSnapOrientation.Horizontal ? v.x : v.y;
		}

		private void SetScrollOffset(float primary)
		{
			primary = Mathf.Max(0f, primary);

			var current = scrollView.scrollOffset;
			if (orientation == ScrollSnapOrientation.Horizontal)
			{
				scrollView.scrollOffset = new Vector2(primary, current.y);
			}
			else
			{
				scrollView.scrollOffset = new Vector2(current.x, primary);
			}
		}

		private float GetMaxScrollOffset()
		{
			var pageSize = GetResolvedPageSize();
			if (pageSize <= 0f || PageCount <= 0)
			{
				return 0f;
			}

			return (PageCount - 1) * pageSize;
		}

		private void RefreshPageSizing()
		{
			if (pages == null)
			{
				return;
			}

			var pageSize = GetResolvedPageSize();
			var hasPageSize = pageSize > 0f;

			var padLeft = Mathf.Max(0f, pagePaddingLeft);
			var padRight = Mathf.Max(0f, pagePaddingRight);
			var padTop = Mathf.Max(0f, pagePaddingTop);
			var padBottom = Mathf.Max(0f, pagePaddingBottom);
			var padX = padLeft + padRight;
			var padY = padTop + padBottom;

			// When we apply margins to create gaps, we reduce the page size so that
			// (width/height + margins) still equals the snap stride (pageSize).
			var innerPrimary = hasPageSize
				? Mathf.Max(0f, pageSize - (orientation == ScrollSnapOrientation.Horizontal ? padX : padY))
				: 0f;
			var viewportCross = orientation == ScrollSnapOrientation.Horizontal ? resolvedStyle.height : resolvedStyle.width;
			var innerCross = viewportCross > 0f && !float.IsNaN(viewportCross)
				? Mathf.Max(0f, viewportCross - (orientation == ScrollSnapOrientation.Horizontal ? padY : padX))
				: 0f;

			foreach (var child in pages.Children())
			{
				child.AddToClassList(PageClass);
				// Pages are page-sized; prevent primary-axis flex growth from overriding width/height.
				child.style.flexGrow = 0;
				child.style.flexShrink = 0;

				child.style.marginLeft = padLeft;
				child.style.marginRight = padRight;
				child.style.marginTop = padTop;
				child.style.marginBottom = padBottom;

				if (orientation == ScrollSnapOrientation.Horizontal)
				{
					// If layout hasn't resolved yet, use percentage sizing so each page still occupies the viewport.
					child.style.width = hasPageSize ? innerPrimary : new Length(100, LengthUnit.Percent);
					// Use resolved height when available so margins don't cause overflow.
					child.style.height = innerCross > 0f ? innerCross : new Length(100, LengthUnit.Percent);
				}
				else
				{
					child.style.height = hasPageSize ? innerPrimary : new Length(100, LengthUnit.Percent);
					child.style.width = innerCross > 0f ? innerCross : new Length(100, LengthUnit.Percent);
				}
			}
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			if (evt.customStyle.TryGetValue(EasingStyleProperty, out var easingName))
			{
				easingCurve = ResolveEasing(easingName);
			}
			else
			{
				easingCurve = Ease.Linear;
			}

			// Page padding (gap) via USS custom properties.
			if (evt.customStyle.TryGetValue(PagePaddingLeftStyleProperty, out var left))
			{
				pagePaddingLeft = Mathf.Max(0f, ParsePixels(left));
			}
			if (evt.customStyle.TryGetValue(PagePaddingRightStyleProperty, out var right))
			{
				pagePaddingRight = Mathf.Max(0f, ParsePixels(right));
			}
			if (evt.customStyle.TryGetValue(PagePaddingTopStyleProperty, out var top))
			{
				pagePaddingTop = Mathf.Max(0f, ParsePixels(top));
			}
			if (evt.customStyle.TryGetValue(PagePaddingBottomStyleProperty, out var bottom))
			{
				pagePaddingBottom = Mathf.Max(0f, ParsePixels(bottom));
			}

			RefreshPageSizing();
		}

		private static float ParsePixels(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return 0f;
			}

			var trimmed = value.Trim();
			if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
			{
				trimmed = trimmed.Substring(0, trimmed.Length - 2).Trim();
			}

			return float.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var px)
				? px
				: 0f;
		}

		private static Func<float, float> ResolveEasing(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return Ease.Linear;
			}

			switch (name.Trim())
			{
				case "Linear": return Ease.Linear;
				case "InSine": return Ease.InSine;
				case "OutSine": return Ease.OutSine;
				case "InOutSine": return Ease.InOutSine;
				case "InQuad": return Ease.InQuad;
				case "OutQuad": return Ease.OutQuad;
				case "InOutQuad": return Ease.InOutQuad;
				case "InCubic": return Ease.InCubic;
				case "OutCubic": return Ease.OutCubic;
				case "InOutCubic": return Ease.InOutCubic;
				case "InQuart": return Ease.InQuart;
				case "OutQuart": return Ease.OutQuart;
				case "InOutQuart": return Ease.InOutQuart;
				case "InQuint": return Ease.InQuint;
				case "OutQuint": return Ease.OutQuint;
				case "InOutQuint": return Ease.InOutQuint;
				case "InExpo": return Ease.InExpo;
				case "OutExpo": return Ease.OutExpo;
				case "InOutExpo": return Ease.InOutExpo;
				case "InCirc": return Ease.InCirc;
				case "OutCirc": return Ease.OutCirc;
				case "InOutCirc": return Ease.InOutCirc;
				case "InBack": return Ease.InBack;
				case "OutBack": return Ease.OutBack;
				case "InOutBack": return Ease.InOutBack;
				case "InElastic": return Ease.InElastic;
				case "OutElastic": return Ease.OutElastic;
				case "InOutElastic": return Ease.InOutElastic;
				case "InBounce": return Ease.InBounce;
				case "OutBounce": return Ease.OutBounce;
				case "InOutBounce": return Ease.InOutBounce;
				default: return Ease.Linear;
			}
		}

		private static class Ease
		{
			public static float Linear(float t) => t;

			public static float InSine(float t) => 1f - Mathf.Cos((t * Mathf.PI) / 2f);
			public static float OutSine(float t) => Mathf.Sin((t * Mathf.PI) / 2f);
			public static float InOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

			public static float InQuad(float t) => t * t;
			public static float OutQuad(float t) => 1f - (1f - t) * (1f - t);
			public static float InOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

			public static float InCubic(float t) => t * t * t;
			public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
			public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

			public static float InQuart(float t) => t * t * t * t;
			public static float OutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
			public static float InOutQuart(float t) => t < 0.5f ? 8f * Mathf.Pow(t, 4f) : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f;

			public static float InQuint(float t) => t * t * t * t * t;
			public static float OutQuint(float t) => 1f - Mathf.Pow(1f - t, 5f);
			public static float InOutQuint(float t) => t < 0.5f ? 16f * Mathf.Pow(t, 5f) : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f;

			public static float InExpo(float t) => Mathf.Approximately(t, 0f) ? 0f : Mathf.Pow(2f, 10f * t - 10f);
			public static float OutExpo(float t) => Mathf.Approximately(t, 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t);
			public static float InOutExpo(float t)
			{
				if (Mathf.Approximately(t, 0f)) return 0f;
				if (Mathf.Approximately(t, 1f)) return 1f;
				return t < 0.5f
					? Mathf.Pow(2f, 20f * t - 10f) / 2f
					: (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f;
			}

			public static float InCirc(float t) => 1f - Mathf.Sqrt(1f - t * t);
			public static float OutCirc(float t) => Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f));
			public static float InOutCirc(float t) => t < 0.5f
				? (1f - Mathf.Sqrt(1f - Mathf.Pow(2f * t, 2f))) / 2f
				: (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f;

			public static float InBack(float t)
			{
				const float c1 = 1.70158f;
				const float c3 = c1 + 1f;
				return c3 * t * t * t - c1 * t * t;
			}

			public static float OutBack(float t)
			{
				const float c1 = 1.70158f;
				const float c3 = c1 + 1f;
				return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
			}

			public static float InOutBack(float t)
			{
				const float c1 = 1.70158f;
				const float c2 = c1 * 1.525f;
				return t < 0.5f
					? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
					: (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;
			}

			public static float InElastic(float t)
			{
				const float c4 = (2f * Mathf.PI) / 3f;
				if (Mathf.Approximately(t, 0f)) return 0f;
				if (Mathf.Approximately(t, 1f)) return 1f;
				return -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * c4);
			}

			public static float OutElastic(float t)
			{
				const float c4 = (2f * Mathf.PI) / 3f;
				if (Mathf.Approximately(t, 0f)) return 0f;
				if (Mathf.Approximately(t, 1f)) return 1f;
				return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
			}

			public static float InOutElastic(float t)
			{
				const float c5 = (2f * Mathf.PI) / 4.5f;
				if (Mathf.Approximately(t, 0f)) return 0f;
				if (Mathf.Approximately(t, 1f)) return 1f;
				return t < 0.5f
					? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f
					: (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * c5)) / 2f + 1f;
			}

			public static float InBounce(float t) => 1f - OutBounce(1f - t);

			public static float OutBounce(float t)
			{
				const float n1 = 7.5625f;
				const float d1 = 2.75f;

				if (t < 1f / d1)
				{
					return n1 * t * t;
				}
				if (t < 2f / d1)
				{
					t -= 1.5f / d1;
					return n1 * t * t + 0.75f;
				}
				if (t < 2.5f / d1)
				{
					t -= 2.25f / d1;
					return n1 * t * t + 0.9375f;
				}

				t -= 2.625f / d1;
				return n1 * t * t + 0.984375f;
			}

			public static float InOutBounce(float t) => t < 0.5f
				? (1f - OutBounce(1f - 2f * t)) / 2f
				: (1f + OutBounce(2f * t - 1f)) / 2f;
		}

		private void OnPointerDown(PointerDownEvent evt)
		{
			if (!manualMovementEnabled)
			{
				return;
			}

			if (evt.button != 0)
			{
				return;
			}

			StopSnapAnimation();
			CancelPendingSnap();

			isPointerDown = true;
			isDragging = false;
			activePointerId = evt.pointerId;
			pointerStart = evt.position;
			scrollOffsetStart = GetScrollOffset();
			startPageIndex = CurrentPageIndex;
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (!manualMovementEnabled || !isPointerDown || evt.pointerId != activePointerId)
			{
				return;
			}

			var delta = evt.position - pointerStart;
			var primary = orientation == ScrollSnapOrientation.Horizontal ? delta.x : delta.y;
			var secondary = orientation == ScrollSnapOrientation.Horizontal ? delta.y : delta.x;

			var absPrimary = Mathf.Abs(primary);
			var absSecondary = Mathf.Abs(secondary);

			const float intentThresholdPx = 8f;

			if (!isDragging)
			{
				// Only claim the gesture if it's primarily along our paging axis.
				if (absPrimary >= intentThresholdPx && absPrimary >= absSecondary)
				{
					isDragging = true;
				}
				else
				{
					// Likely a perpendicular scroll for child content; let it through.
					return;
				}
			}

			// Dragging: update scroll offset directly.
			var next = scrollOffsetStart - primary;
			next = Mathf.Clamp(next, 0f, GetMaxScrollOffset());
			SetScrollOffset(next);

			evt.StopPropagation();
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (!isPointerDown || evt.pointerId != activePointerId)
			{
				return;
			}

			FinishPointerGesture(evt.position);
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (!isPointerDown || evt.pointerId != activePointerId)
			{
				return;
			}

			FinishPointerGesture(evt.position);
		}

		private void OnScrollWheel(WheelEvent evt)
		{
			// Only react to wheel/trackpad gestures that are primarily along our snap axis.
			var absPrimary = Mathf.Abs(orientation == ScrollSnapOrientation.Horizontal ? evt.delta.x : evt.delta.y);
			var absSecondary = Mathf.Abs(orientation == ScrollSnapOrientation.Horizontal ? evt.delta.y : evt.delta.x);
			var isPrimaryGesture = absPrimary > 0f && absPrimary >= absSecondary;

			if (!isPrimaryGesture)
			{
				return;
			}

			if (!manualMovementEnabled)
			{
				// Disable ScrollSnap's own movement without affecting child perpendicular scrolling.
				evt.StopImmediatePropagation();
				return;
			}

			// Let ScrollView process the wheel, then snap after the scroll settles.
			ScheduleSnapToNearestPage(animate: true);
		}

		private void OnScrollViewPointerUp(PointerUpEvent _)
		{
			if (!manualMovementEnabled)
			{
				return;
			}

			// If the ScrollView handled the drag, we still want to settle onto a page.
			ScheduleSnapToNearestPage(animate: true);
		}

		private void OnScrollViewPointerCancel(PointerCancelEvent _)
		{
			if (!manualMovementEnabled)
			{
				return;
			}

			ScheduleSnapToNearestPage(animate: true);
		}

		private void FinishPointerGesture(Vector3 endPosition)
		{
			var delta = endPosition - pointerStart;
			var primary = orientation == ScrollSnapOrientation.Horizontal ? delta.x : delta.y;

			isPointerDown = false;

			if (!isDragging)
			{
				return;
			}

			isDragging = false;

			var pageSize = GetResolvedPageSize();
			if (pageSize <= 0f)
			{
				return;
			}

			// Only move one page per swipe.
			var threshold = pageSize * 0.15f;
			var target = startPageIndex;

			if (primary <= -threshold)
			{
				target = startPageIndex + 1;
			}
			else if (primary >= threshold)
			{
				target = startPageIndex - 1;
			}
			else
			{
				// Snap to nearest page.
				var raw = GetScrollOffset() / pageSize;
				target = Mathf.RoundToInt(raw);
			}

			GoToPage(target, animate: true);
		}
	}
}