/// Credit SimonDarksideJ  

using System;
using System.Threading.Tasks;
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
		private static readonly CustomStyleProperty<string> ValidationDragLimitStyleProperty = new("--scrollsnap-validation-drag-limit");

		private readonly VisualElement viewport;
		private readonly VisualElement content;
		private Vector2 scrollOffset = Vector2.zero;

		private ScrollSnapOrientation orientation = ScrollSnapOrientation.Horizontal;
		private float explicitPageSize = 0f;
		private bool manualMovementEnabled = false;
		private float pagePaddingLeft;
		private float pagePaddingRight;
		private float pagePaddingTop;
		private float pagePaddingBottom;

		// Validation state fields
		private bool validatePageChange = false;
		private bool onlySinglePageSwipeAllowed = true;
		private bool canMoveNextPage = true;
		private bool canMoveBackPage = true;
		private bool allowMoveBack = true;
		private float validationDragLimit = 0.2f;
		private bool isValidatingPageChange = false;

		private bool isPointerDown;
		private bool isDragging;
		private bool pointerStartedOnChild;
		private bool pointerStartedOnInteractiveChild;
		private int activePointerId;
		private Vector3 pointerStart;
		private float scrollOffsetStart;
		private int startPageIndex;

		private ValueAnimation<float> snapAnimation;
		private Func<float, float> easingCurve = Easing.Linear;
		private IVisualElementScheduledItem pendingSnap;
		private const long ScrollEndSnapDelayMs = 100;

		public event Action<int> PageChanged;

		/// <summary>Fired immediately when a swipe gesture is detected, before validation runs.
		/// targetPage is the page the user is attempting to reach; moveAllowed indicates whether
		/// the current validation state permits movement in that direction.</summary>
		public event Action<int, bool> OnPageStartChange;

		/// <summary>Fired when a swipe gesture is blocked and the control snaps back to the
		/// current page. Use this to show on-screen feedback explaining the restriction.</summary>
		public event Action<int> OnPageChangeRestricted;

		/// <summary>Optional async validation callback. Return true to allow the page transition,
		/// false to block it. While the callback is pending, further swipe gestures are ignored.
		/// Only invoked when ValidatePageChange is true and the CanMove flags permit movement.</summary>
		public Func<int, Task<bool>> OnValidatePageTransition;

		[System.Diagnostics.Conditional("SCROLLSNAP_DEBUG")]
		private static void DebugLog(string message)
		{
			Debug.Log($"[ScrollSnap] {message}");
		}

		private static string DescribeEventTarget(EventBase evt)
		{
			if (evt?.target is not VisualElement element)
			{
				return evt?.target?.GetType().Name ?? "null";
			}

			var elementName = string.IsNullOrEmpty(element.name) ? "<unnamed>" : element.name;
			return $"{element.GetType().Name}(name={elementName}, classes={string.Join(",", element.GetClasses())})";
		}

		private static bool IsInteractiveElement(VisualElement element)
		{
			for (var current = element; current != null; current = current.parent)
			{
				var typeName = current.GetType().Name;

				if (current is TextField || current is Button)
				{
					return true;
				}

				if (typeName.Contains("Button", StringComparison.Ordinal) ||
					typeName.Contains("Toggle", StringComparison.Ordinal) ||
					typeName.Contains("InputField", StringComparison.Ordinal) ||
					typeName.Contains("TextField", StringComparison.Ordinal))
				{
					return true;
				}

				if (current.ClassListContains("pillButton") ||
					current.ClassListContains("pillButton__background") ||
					current.ClassListContains("toggleButton") ||
					current.ClassListContains("unity-base-field") ||
					current.ClassListContains("unity-text-field") ||
					current.ClassListContains("unity-text-element--inner-input-field-component"))
				{
					return true;
				}

				if (current == element.panel?.visualTree)
				{
					break;
				}
			}

			return false;
		}

		public ScrollSnap()
		{
			AddToClassList(RootClass);

			// Custom viewport (replaces ScrollView) - clips overflow
			viewport = new VisualElement();
			viewport.AddToClassList(ScrollViewClass);
			viewport.style.flexGrow = 1;
			viewport.style.flexShrink = 1;
			viewport.style.minWidth = 0;
			viewport.style.minHeight = 0;
			viewport.style.overflow = Overflow.Hidden;

			// Content container inside viewport
			content = new VisualElement();
			content.AddToClassList(ContentClass);
			content.RegisterCallback<GeometryChangedEvent>(_ => RefreshPageSizing());

			viewport.Add(content);
			hierarchy.Add(viewport);

			UpdateOrientation(orientation);

			RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
			RegisterCallback<GeometryChangedEvent>(_ => RefreshPageSizing());

			// Register pointer events directly on viewport for full control
			viewport.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
			viewport.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
			viewport.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
			viewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
			viewport.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);

		}

		public override VisualElement contentContainer => content;

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
		/// When true, a swipe can move at most one page regardless of swipe distance or speed.
		/// The page will resist movement beyond the adjacent page boundary (with a small tolerance)
		/// and snap back if the user exceeds it. Default: true.
		/// </summary>
		[UxmlAttribute("only-single-page-swipe-allowed")]
		public bool OnlySinglePageSwipeAllowed
		{
			get => onlySinglePageSwipeAllowed;
			set => onlySinglePageSwipeAllowed = value;
		}

		/// <summary>
		/// When true, page transitions are subject to validation via <see cref="CanMoveNextPage"/>,
		/// <see cref="AllowMoveBack"/> and the <see cref="OnValidatePageTransition"/> callback.
		/// Validation only applies to swipe gestures (and programmatic calls without force: true).
		/// Default: false.
		/// </summary>
		[UxmlAttribute("validate-page-change")]
		public bool ValidatePageChange
		{
			get => validatePageChange;
			set => validatePageChange = value;
		}

		/// <summary>
		/// Controls whether the user may swipe forward to the next page when
		/// <see cref="ValidatePageChange"/> is true. Automatically reset to false when the
		/// current page index changes. Set to true externally to permit the next forward swipe.
		/// Default: true (permits movement until validation resets it).
		/// </summary>
		public bool CanMoveNextPage
		{
			get => canMoveNextPage;
			set => canMoveNextPage = value;
		}

		/// <summary>
		/// Controls whether the user may swipe backward to the previous page when
		/// <see cref="ValidatePageChange"/> is true. Mirrors <see cref="CanMoveNextPage"/> for
		/// backward gestures. Default: true.
		/// </summary>
		public bool CanMoveBackPage
		{
			get => canMoveBackPage;
			set => canMoveBackPage = value;
		}

		/// <summary>
		/// When false and <see cref="ValidatePageChange"/> is true, backward swipes are treated
		/// the same as a blocked forward swipe: the user sees a preview drag up to
		/// <see cref="ValidationDragLimit"/> and the control snaps back on release.
		/// Default: true.
		/// </summary>
		[UxmlAttribute("allow-move-back")]
		public bool AllowMoveBack
		{
			get => allowMoveBack;
			set => allowMoveBack = value;
		}

		/// <summary>
		/// The maximum fraction of the page size (0–1) that a blocked drag can travel before
		/// the offset is clamped. Provides tactile preview feedback. Default: 0.2 (20%).
		/// Can be overridden per-element via USS: <c>--scrollsnap-validation-drag-limit: 30px;</c>
		/// (interpreted as a fraction of page size when value &lt;= 1, or as raw pixels when &gt; 1).
		/// </summary>
		[UxmlAttribute("validation-drag-limit")]
		public float ValidationDragLimit
		{
			get => validationDragLimit;
			set => validationDragLimit = Mathf.Clamp01(value);
		}

		/// <summary>
		/// True while an async <see cref="OnValidatePageTransition"/> callback is in progress.
		/// Swipe gestures are ignored during this period.
		/// </summary>
		public bool IsValidatingPageChange => isValidatingPageChange;

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

		public int PageCount => content?.childCount ?? 0;

		public int CurrentPageIndex { get; private set; }

		/// <summary>Moves to the next page. If <see cref="ValidatePageChange"/> is active,
		/// <see cref="CanMoveNextPage"/> must be true unless <paramref name="force"/> is true.</summary>
		public void MoveNext(bool animate = true, bool force = false) => GoToPage(CurrentPageIndex + 1, animate, force);

		/// <summary>Moves to the previous page. If <see cref="ValidatePageChange"/> is active,
		/// <see cref="AllowMoveBack"/> and <see cref="CanMoveBackPage"/> must be true unless <paramref name="force"/> is true.</summary>
		public void MovePrevious(bool animate = true, bool force = false) => GoToPage(CurrentPageIndex - 1, animate, force);

		/// <summary>Navigates to the specified page.
		/// <paramref name="force"/> bypasses all validation checks and moves immediately.</summary>
		public void GoToPage(int index, bool animate = true, bool force = false)
		{
			GoToPageInternal(index, animate, force, onCompleted: null);
		}

		private void GoToPageInternal(int index, bool animate, bool force, Action onCompleted)
		{
			if (PageCount <= 0)
			{
				CurrentPageIndex = 0;
				SetScrollOffset(0f);
				onCompleted?.Invoke();
				return;
			}

			var clamped = Mathf.Clamp(index, 0, PageCount - 1);
			var isStateChange = clamped != CurrentPageIndex;
			DebugLog($"GoToPageInternal requested={index} clamped={clamped} current={CurrentPageIndex} animate={animate} force={force} stateChange={isStateChange}");

			if (isStateChange)
			{
				var moveAllowed = true;

				if (validatePageChange && !force)
				{
					var isForward = clamped > CurrentPageIndex;
					moveAllowed = isForward ? canMoveNextPage : (allowMoveBack && canMoveBackPage);
				}

				OnPageStartChange?.Invoke(clamped, moveAllowed);
			}

			// Validation gate for non-forced programmatic calls.
			if (!force && validatePageChange && isStateChange)
			{
				var isForward = clamped > CurrentPageIndex;
				if (isForward && !canMoveNextPage)
				{
					DebugLog($"ProgrammaticTransitionRestricted current={CurrentPageIndex} target={clamped} direction=forward");
					GoToPageInternal(CurrentPageIndex, animate: true, force: true, () =>
					{
						OnPageChangeRestricted?.Invoke(clamped);
						onCompleted?.Invoke();
					});
					return;
				}

				if (!isForward && (!allowMoveBack || !canMoveBackPage))
				{
					DebugLog($"ProgrammaticTransitionRestricted current={CurrentPageIndex} target={clamped} direction=backward allowMoveBack={allowMoveBack} canMoveBack={canMoveBackPage}");
					GoToPageInternal(CurrentPageIndex, animate: true, force: true, () =>
					{
						OnPageChangeRestricted?.Invoke(clamped);
						onCompleted?.Invoke();
					});
					return;
				}
			}

			var pageSize = GetResolvedPageSize();
			if (pageSize <= 0f)
			{
				DebugLog($"GoToPageInternal applying without pageSize current={CurrentPageIndex} target={clamped}");
				SetCurrentPage(clamped);
				onCompleted?.Invoke();
				return;
			}

			StopSnapAnimation();
			CancelPendingSnap();

			var targetOffset = clamped * pageSize;
			if (!animate)
			{
				DebugLog($"GoToPageInternal immediate current={CurrentPageIndex} target={clamped} offset={targetOffset:0.##}");
				SetScrollOffset(targetOffset);
				SetCurrentPage(clamped);
				onCompleted?.Invoke();
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
				DebugLog($"GoToPageInternal animation completed target={clamped} offset={targetOffset:0.##}");
				onCompleted?.Invoke();
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

			// Reset validation flags so the host must re-enable movement for the next transition.
			if (validatePageChange)
			{
				canMoveNextPage = false;
				canMoveBackPage = false;
			}

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
				content.style.flexDirection = FlexDirection.Row;
			}
			else
			{
				content.style.flexDirection = FlexDirection.Column;
			}

			content.style.flexWrap = Wrap.NoWrap;
		}

		private float GetResolvedPageSize()
		{
			if (explicitPageSize > 0f)
			{
				return explicitPageSize;
			}

			var size = orientation == ScrollSnapOrientation.Horizontal ? viewport.resolvedStyle.width : viewport.resolvedStyle.height;
			if (float.IsNaN(size) || size <= 0f)
			{
				return 0f;
			}

			return size;
		}

		private float GetScrollOffset()
		{
			return orientation == ScrollSnapOrientation.Horizontal ? scrollOffset.x : scrollOffset.y;
		}

		private void SetScrollOffset(float primary)
		{
			primary = Mathf.Max(0f, primary);

			if (orientation == ScrollSnapOrientation.Horizontal)
			{
				scrollOffset.x = primary;
			}
			else
			{
				scrollOffset.y = primary;
			}

			ApplyScrollOffset();
		}

		private void ApplyScrollOffset()
		{
			if (orientation == ScrollSnapOrientation.Horizontal)
			{
				content.style.translate = new Translate(-scrollOffset.x, 0, 0);
			}
			else
			{
				content.style.translate = new Translate(0, -scrollOffset.y, 0);
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
			if (content == null)
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
			var viewportCross = orientation == ScrollSnapOrientation.Horizontal ? viewport.resolvedStyle.height : viewport.resolvedStyle.width;
			var innerCross = viewportCross > 0f && !float.IsNaN(viewportCross)
				? Mathf.Max(0f, viewportCross - (orientation == ScrollSnapOrientation.Horizontal ? padY : padX))
				: 0f;

			foreach (var child in content.Children())
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

			RealignToCurrentPage(pageSize);
		}

		private void RealignToCurrentPage(float pageSize)
		{
			if (pageSize <= 0f || PageCount <= 0)
			{
				return;
			}

			if (isPointerDown || isDragging || snapAnimation != null)
			{
				return;
			}

			var maxOffset = (PageCount - 1) * pageSize;
			var targetOffset = Mathf.Clamp(CurrentPageIndex * pageSize, 0f, maxOffset);
			if (Mathf.Approximately(GetScrollOffset(), targetOffset))
			{
				return;
			}

			DebugLog($"RealignCurrentPage current={CurrentPageIndex} offset={GetScrollOffset():0.##} targetOffset={targetOffset:0.##}");
			SetScrollOffset(targetOffset);
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

			// Validation drag limit via USS custom property.
			// Value is treated as a fraction (0-1) of page size when <= 1, or as raw pixels
			// that are converted to a fraction at drag time.
			if (evt.customStyle.TryGetValue(ValidationDragLimitStyleProperty, out var dragLimitStr))
			{
				var parsed = ParsePixels(dragLimitStr);
				if (parsed > 0f)
				{
					// Store as fraction when <= 1, otherwise store raw px to be resolved at drag time.
					validationDragLimit = parsed <= 1f ? parsed : parsed;
				}
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
				// Allow events to propagate to children when disabled
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
			pointerStartedOnChild = evt.target is VisualElement target && target != viewport && target != content;
			pointerStartedOnInteractiveChild = evt.target is VisualElement interactiveTarget && IsInteractiveElement(interactiveTarget);
			activePointerId = evt.pointerId;
			pointerStart = evt.position;
			scrollOffsetStart = GetScrollOffset();
			startPageIndex = CurrentPageIndex;
			DebugLog($"PointerDown pointer={evt.pointerId} pos={evt.position} target={DescribeEventTarget(evt)} childStart={pointerStartedOnChild} interactiveChildStart={pointerStartedOnInteractiveChild} page={CurrentPageIndex} offset={scrollOffsetStart:0.##}");

			// Don't capture pointer yet - let children receive click events
			// Pointer will be captured in OnPointerMove if drag is detected
		}

		private void OnPointerMove(PointerMoveEvent evt)
		{
			if (!manualMovementEnabled)
			{
				// Allow events to propagate to children when disabled
				return;
			}

			if (!isPointerDown || evt.pointerId != activePointerId)
			{
				return;
			}

			// Block gestures while async validation is running.
			if (isValidatingPageChange)
			{
				DebugLog($"PointerMove blocked during validation pointer={evt.pointerId} pos={evt.position}");
				evt.StopPropagation();
				return;
			}

			var delta = evt.position - pointerStart;
			var primary = orientation == ScrollSnapOrientation.Horizontal ? delta.x : delta.y;
			var secondary = orientation == ScrollSnapOrientation.Horizontal ? delta.y : delta.x;

			if (pointerStartedOnInteractiveChild)
			{
				if (Mathf.Abs(primary) > 0f || Mathf.Abs(secondary) > 0f)
				{
					DebugLog($"PointerMove ignored interactive child pointer={evt.pointerId} delta={delta} primary={primary:0.##} secondary={secondary:0.##}");
				}
				return;
			}

			var absPrimary = Mathf.Abs(primary);
			var absSecondary = Mathf.Abs(secondary);

			var intentThresholdPx = pointerStartedOnChild ? 24f : 8f;
			var axisDominancePx = pointerStartedOnChild ? 12f : 0f;

			if (!isDragging)
			{
				// Only claim the gesture if it's primarily along our paging axis.
				if (absPrimary >= intentThresholdPx && absPrimary >= absSecondary + axisDominancePx)
				{
					isDragging = true;
					DebugLog($"DragStarted pointer={evt.pointerId} pos={evt.position} delta={delta} primary={primary:0.##} secondary={secondary:0.##} threshold={intentThresholdPx:0.##} childStart={pointerStartedOnChild}");
					// Now capture the pointer since we've confirmed it's a drag gesture
					viewport.CapturePointer(evt.pointerId);
				}
				else
				{
					if (absPrimary > 0f || absSecondary > 0f)
					{
						DebugLog($"PointerMove ignored pointer={evt.pointerId} delta={delta} primary={primary:0.##} secondary={secondary:0.##} threshold={intentThresholdPx:0.##} childStart={pointerStartedOnChild}");
					}
					// Likely a perpendicular scroll for child content; let it through.
					return;
				}
			}

			// Dragging: compute unclamped offset then apply validation clamping.
			var next = scrollOffsetStart - primary;

			if (validatePageChange)
			{
				var pageSize = GetResolvedPageSize();
				var movingForward = next > scrollOffsetStart;
				var movingBack = next < scrollOffsetStart;

				// Compute the maximum allowed offset delta when validation blocks movement.
				var dragLimitOffset = pageSize > 0f ? pageSize * validationDragLimit : 0f;

				if (movingForward && !canMoveNextPage)
				{
					// Clamp forward drag to the validation preview limit.
					next = Mathf.Min(next, scrollOffsetStart + dragLimitOffset);
				}
				else if (movingBack && (!allowMoveBack || !canMoveBackPage))
				{
					// Clamp backward drag to the validation preview limit.
					next = Mathf.Max(next, scrollOffsetStart - dragLimitOffset);
				}
				else if (onlySinglePageSwipeAllowed && pageSize > 0f)
				{
					// Permitted direction – still clamp to max one page stride.
					var minAllowed = startPageIndex * pageSize;
					var maxAllowed = (startPageIndex + 1) * pageSize;
					next = Mathf.Clamp(next, minAllowed, maxAllowed);
				}
			}
			else if (onlySinglePageSwipeAllowed)
			{
				var pageSize = GetResolvedPageSize();
				if (pageSize > 0f)
				{
					var minAllowed = startPageIndex * pageSize;
					var maxAllowed = (startPageIndex + 1) * pageSize;
					next = Mathf.Clamp(next, minAllowed, maxAllowed);
				}
			}

			next = Mathf.Clamp(next, 0f, GetMaxScrollOffset());
			SetScrollOffset(next);
			DebugLog($"Dragging pointer={evt.pointerId} nextOffset={next:0.##} startOffset={scrollOffsetStart:0.##}");

			evt.StopPropagation();
		}

		private void OnPointerUp(PointerUpEvent evt)
		{
			if (!isPointerDown || evt.pointerId != activePointerId)
			{
				return;
			}

			// Only release if we captured it (during drag)
			if (isDragging)
			{
				viewport.ReleasePointer(evt.pointerId);
			}
			DebugLog($"PointerUp pointer={evt.pointerId} pos={evt.position} wasDragging={isDragging}");
			FinishPointerGesture(evt.position);
		}

		private void OnPointerCancel(PointerCancelEvent evt)
		{
			if (!isPointerDown || evt.pointerId != activePointerId)
			{
				return;
			}

			// Only release if we captured it (during drag)
			if (isDragging)
			{
				viewport.ReleasePointer(evt.pointerId);
			}
			DebugLog($"PointerCancel pointer={evt.pointerId} pos={evt.position} wasDragging={isDragging}");
			FinishPointerGesture(evt.position);
		}

		private void OnScrollWheel(WheelEvent evt)
		{
			if (!manualMovementEnabled)
			{
				// Block wheel scrolling when disabled
				evt.StopImmediatePropagation();
				evt.StopPropagation();
				return;
			}

			// Only react to wheel/trackpad gestures that are primarily along our snap axis.
			var absPrimary = Mathf.Abs(orientation == ScrollSnapOrientation.Horizontal ? evt.delta.x : evt.delta.y);
			var absSecondary = Mathf.Abs(orientation == ScrollSnapOrientation.Horizontal ? evt.delta.y : evt.delta.x);
			var isPrimaryGesture = absPrimary > 0f && absPrimary >= absSecondary;

			if (!isPrimaryGesture)
			{
				return;
			}

			// Snap after wheel scroll settles
			ScheduleSnapToNearestPage(animate: true);
		}

		private void FinishPointerGesture(Vector3 endPosition)
		{
			var delta = endPosition - pointerStart;
			var primary = orientation == ScrollSnapOrientation.Horizontal ? delta.x : delta.y;

			isPointerDown = false;

			if (!isDragging)
			{
				DebugLog($"PointerReleasedWithoutDrag pos={endPosition} delta={delta} childStart={pointerStartedOnChild}");
				return;
			}

			isDragging = false;

			var pageSize = GetResolvedPageSize();
			if (pageSize <= 0f)
			{
				return;
			}

			// Only move one page per swipe (when enabled).
			var threshold = pageSize * 0.15f;
			var target = startPageIndex;

			if (primary <= -threshold)
			{
				target = onlySinglePageSwipeAllowed
					? startPageIndex + 1
					: Mathf.Clamp(startPageIndex + Mathf.Max(1, Mathf.FloorToInt(Mathf.Abs(primary) / pageSize)), 0, PageCount - 1);
			}
			else if (primary >= threshold)
			{
				target = onlySinglePageSwipeAllowed
					? startPageIndex - 1
					: Mathf.Clamp(startPageIndex - Mathf.Max(1, Mathf.FloorToInt(primary / pageSize)), 0, PageCount - 1);
			}
			else
			{
				// Snap to nearest page.
				var raw = GetScrollOffset() / pageSize;
				target = Mathf.RoundToInt(raw);
			}

			target = Mathf.Clamp(target, 0, PageCount - 1);
			DebugLog($"FinishPointerGesture primary={primary:0.##} threshold={threshold:0.##} startPage={startPageIndex} currentPage={CurrentPageIndex} target={target} offset={GetScrollOffset():0.##}");

		if (target == CurrentPageIndex)
		{
			// No page change needed, but snap back to current page position visually
			DebugLog($"SnapBackToCurrentPage target={target}");
			GoToPage(target, animate: true, force: true);
			return;
		}

		if (!validatePageChange)
		{
			// Validation disabled – always allow movement and navigate directly.
			// Fire OnPageStartChange with moveAllowed=true since there are no restrictions.
			DebugLog($"PageTransitionWithoutValidation current={CurrentPageIndex} target={target}");
			OnPageStartChange?.Invoke(target, true);
			GoToPage(target, animate: true, force: true);
			return;
		}

		// Validation enabled – determine if movement is permitted by the current validation state.
		var movingForward = target > CurrentPageIndex;
		var moveAllowed = movingForward ? canMoveNextPage : (allowMoveBack && canMoveBackPage);

		// Notify host of the attempted transition and whether it's allowed.
		DebugLog($"PageTransitionAttempt current={CurrentPageIndex} target={target} movingForward={movingForward} moveAllowed={moveAllowed} allowMoveBack={allowMoveBack} canMoveNext={canMoveNextPage} canMoveBack={canMoveBackPage}");
		OnPageStartChange?.Invoke(target, moveAllowed);

		if (!moveAllowed)
		{
			// Direction is blocked by flags alone – snap back and notify once snap-back completes.
			DebugLog($"PageTransitionRestricted current={CurrentPageIndex} target={target}");
			GoToPageInternal(CurrentPageIndex, animate: true, force: true, () => OnPageChangeRestricted?.Invoke(target));
			return;
		}

		// Flags permit movement – run async validation if a callback is registered.
		if (OnValidatePageTransition != null)
		{
			ExecutePageTransitionWithValidation(target).Forget();
		}
		else
		{
			GoToPage(target, animate: true, force: true);
		}
	}

	/// <summary>
	/// Awaits the <see cref="OnValidatePageTransition"/> callback and either navigates to
	/// <paramref name="target"/> or snaps back to the current page. Runs as a fire-and-forget
	/// async operation so the UI thread is never blocked.
	/// </summary>
	private async Task ExecutePageTransitionWithValidation(int target)
		{
			isValidatingPageChange = true;

			bool allowed;
			try
			{
				allowed = await OnValidatePageTransition.Invoke(target);
			}
			catch
			{
				// Treat any exception from the validator as a denial to avoid soft-locks.
				allowed = false;
			}
			finally
			{
				isValidatingPageChange = false;
			}

			if (allowed)
			{
				GoToPage(target, animate: true, force: true);
			}
			else
			{
				GoToPageInternal(CurrentPageIndex, animate: true, force: true, () => OnPageChangeRestricted?.Invoke(target));
			}
		}
	}

	/// <summary>
	/// Minimal fire-and-forget extension to suppress CS4014 warnings on unawaited Tasks
	/// without pulling in UniTask or other async utilities.
	/// </summary>
	internal static class TaskExtensions
	{
		internal static void Forget(this Task task)
		{
			// Intentionally fire-and-forget. Exceptions are swallowed inside
			// ExecutePageTransitionWithValidation via the try/catch block.
			_ = task;
		}
	}
}