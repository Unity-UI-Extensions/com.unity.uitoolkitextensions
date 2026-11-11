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
	/// The control always rests on a page. Nested ScrollViews can handle the perpendicular axis.
	/// Children still receive events when manual movement is off.
	/// The USS custom property <c>--scrollsnap-easing</c> sets the easing (default: Linear), for example <c>.scrollSnap { --scrollsnap-easing: OutCubic; }</c>.
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
		private const int SnapDurationMs = 320;

		public event Action<int> PageChanged;

		/// <summary>
		/// Fired immediately when a swipe gesture is detected, before validation runs.
		/// The target is the page the user is attempting to reach; moveAllowed indicates whether the current validation state permits movement in that direction.
		/// </summary>
		public event Action<int, bool> OnPageStartChange;

		/// <summary>
		/// Fired when a swipe gesture is blocked and the control snaps back to the current page. Use this to show on-screen feedback explaining the restriction.
		/// </summary>
		public event Action<int> OnPageChangeRestricted;

		/// <summary>
		/// Optional async validation callback. Return true to allow the page transition,
		/// false to block it. While the callback is pending, further swipe gestures are ignored.
		/// Only invoked when ValidatePageChange is true and the CanMove flags permit movement.
		/// </summary>
		public Func<int, Task<bool>> OnValidatePageTransition;

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
			this.ApplyControlStyles();
			AddToClassList(RootClass);

			// Custom viewport (replaces ScrollView) - clips overflow
			viewport = new VisualElement();
			viewport.AddToClassList(ScrollViewClass);
			viewport.style.flexGrow = 1;
			viewport.style.flexShrink = 1;
			viewport.style.minWidth = 0;
			viewport.style.minHeight = 0;
			viewport.style.overflow = Overflow.Hidden;

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
		/// Set to less than 0 to use the control's resolved width/height.
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
		/// The page will resist movement beyond the adjacent page boundary (with a small tolerance) and snap back if the user exceeds it. Default: true.
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
		/// <see cref="ValidatePageChange"/> is true. Automatically reset to false when the current page index changes. Set to true externally to permit the next forward swipe.
		/// Default: true (permits movement until validation resets it).
		/// </summary>
		public bool CanMoveNextPage
		{
			get => canMoveNextPage;
			set => canMoveNextPage = value;
		}

		/// <summary>
		/// Controls whether the user may swipe backward to the previous page when <see cref="ValidatePageChange"/> is true.
		/// Mirrors <see cref="CanMoveNextPage"/> for backward gestures. Default: true.
		/// </summary>
		public bool CanMoveBackPage
		{
			get => canMoveBackPage;
			set => canMoveBackPage = value;
		}

		/// <summary>
		/// When false and <see cref="ValidatePageChange"/> is true, backward swipes are treated the same as a blocked forward swipe:
		/// the user sees a preview drag up to <see cref="ValidationDragLimit"/> and the control snaps back on release.
		/// Default: true.
		/// </summary>
		[UxmlAttribute("allow-move-back")]
		public bool AllowMoveBack
		{
			get => allowMoveBack;
			set => allowMoveBack = value;
		}

		/// <summary>
		/// The maximum fraction of the page size (0 to 1) that a blocked drag can travel before the offset is clamped. Default: 0.2 (20%).
		/// Can be overridden per-element via USS: <c>--scrollsnap-validation-drag-limit: 30px;</c> (interpreted as a fraction of page size when value &lt;= 1, or as raw pixels when &gt; 1).
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

		/*
		 Padding/gap around each page (in pixels). This is applied as margins on the page,
		 while the page's size is reduced so the overall snap stride stays equal to PageSize.
		
		 The USS custom properties --scrollsnap-page-padding-left, -right, -top and -bottom set these values in pixels.
		*/

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

		public int PageCount => content.childCount;

		public int CurrentPageIndex { get; private set; }

		/// <summary>
		/// Moves to the next page. If <see cref="ValidatePageChange"/> is active,
		/// <see cref="CanMoveNextPage"/> must be true unless <paramref name="force"/> is true.
		/// </summary>
		public void MoveNext(bool animate = true, bool force = false) => GoToPage(CurrentPageIndex + 1, animate, force);

		/// <summary>
		/// Moves to the previous page. If <see cref="ValidatePageChange"/> is active,
		/// <see cref="AllowMoveBack"/> and <see cref="CanMoveBackPage"/> must be true unless <paramref name="force"/> is true.
		/// </summary>
		public void MovePrevious(bool animate = true, bool force = false) => GoToPage(CurrentPageIndex - 1, animate, force);

		/// <summary>
		/// Navigates to the specified page.
		/// <paramref name="force"/> bypasses all validation checks and moves immediately.
		/// </summary>
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

			if (isStateChange)
			{
				var isRestricted = validatePageChange && !force;
				var isForward = clamped > CurrentPageIndex;

				OnPageStartChange?.Invoke(clamped, !isRestricted || IsMoveAllowed(isForward));

				// Read the flags again. An OnPageStartChange handler can change them.
				if (isRestricted && !IsMoveAllowed(isForward))
				{
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
				SetCurrentPage(clamped);
				onCompleted?.Invoke();
				return;
			}

			StopSnapAnimation();
			CancelPendingSnap();

			var targetOffset = clamped * pageSize;
			if (!animate)
			{
				SetScrollOffset(targetOffset);
				SetCurrentPage(clamped);
				onCompleted?.Invoke();
				return;
			}

			snapAnimation = experimental.animation.Start(GetScrollOffset(), targetOffset, SnapDurationMs, (_, v) => SetScrollOffset(v));
			snapAnimation.easingCurve = easingCurve ?? Easing.Linear;
			snapAnimation.KeepAlive();

			snapAnimation.onAnimationCompleted += () =>
			{
				StopSnapAnimation();
				SetScrollOffset(targetOffset);
				SetCurrentPage(clamped);
				onCompleted?.Invoke();
			};
		}

		private bool IsMoveAllowed(bool forward) => forward ? canMoveNextPage : allowMoveBack && canMoveBackPage;

		private void SnapToNearestPage()
		{
			pendingSnap = null;
			var pageSize = GetResolvedPageSize();
			if (PageCount > 0 && pageSize > 0f)
			{
				GoToPage(Mathf.RoundToInt(GetScrollOffset() / pageSize));
			}
		}

		private void ScheduleSnapToNearestPage()
		{
			CancelPendingSnap();
			pendingSnap = schedule.Execute(SnapToNearestPage).StartingIn(ScrollEndSnapDelayMs);
		}

		private void CancelPendingSnap()
		{
			pendingSnap?.Pause();
			pendingSnap = null;
		}

		private void SetCurrentPage(int index)
		{
			if (CurrentPageIndex == index)
			{
				return;
			}

			CurrentPageIndex = index;

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
			catch (InvalidOperationException)
			{
				// UI Toolkit can recycle an animation while a reference to it still exists.
			}
			finally
			{
				snapAnimation = null;
			}
		}

		private void UpdateOrientation(ScrollSnapOrientation newOrientation)
		{
			content.style.flexDirection = newOrientation == ScrollSnapOrientation.Horizontal ? FlexDirection.Row : FlexDirection.Column;
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
				child.style.flexGrow = 0;
				child.style.flexShrink = 0;

				child.style.marginLeft = padLeft;
				child.style.marginRight = padRight;
				child.style.marginTop = padTop;
				child.style.marginBottom = padBottom;

				if (orientation == ScrollSnapOrientation.Horizontal)
				{
					child.style.width = hasPageSize ? innerPrimary : new Length(100, LengthUnit.Percent);
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

			SetScrollOffset(targetOffset);
		}

		private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
		{
			easingCurve = evt.customStyle.TryGetValue(EasingStyleProperty, out var easingName) ? Ease.ResolveEasing(easingName) : Ease.Linear;

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

			// The value is a fraction of the page size.
			if (evt.customStyle.TryGetValue(ValidationDragLimitStyleProperty, out var dragLimitStr))
			{
				var parsed = ParsePixels(dragLimitStr);
				if (parsed > 0f)
				{
					validationDragLimit = parsed;
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

			// Do not capture the pointer here, so children still receive clicks. OnPointerMove captures it when a drag starts.
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
				evt.StopPropagation();
				return;
			}

			var delta = evt.position - pointerStart;
			var primary = orientation == ScrollSnapOrientation.Horizontal ? delta.x : delta.y;
			var secondary = orientation == ScrollSnapOrientation.Horizontal ? delta.y : delta.x;

			if (pointerStartedOnInteractiveChild)
			{
				return;
			}

			var absPrimary = Mathf.Abs(primary);
			var absSecondary = Mathf.Abs(secondary);

			var intentThresholdPx = pointerStartedOnChild ? 24f : 8f;
			var axisDominancePx = pointerStartedOnChild ? 12f : 0f;

			if (!isDragging)
			{
				// Claim the gesture only when it moves mainly along the paging axis.
				if (absPrimary < intentThresholdPx || absPrimary < absSecondary + axisDominancePx)
				{
					return;
				}

				isDragging = true;
				viewport.CapturePointer(evt.pointerId);
			}

			var next = scrollOffsetStart - primary;
			var pageSize = GetResolvedPageSize();
			var dragLimitOffset = pageSize > 0f ? pageSize * validationDragLimit : 0f;

			if (validatePageChange && next > scrollOffsetStart && !canMoveNextPage)
			{
				next = Mathf.Min(next, scrollOffsetStart + dragLimitOffset);
			}
			else if (validatePageChange && next < scrollOffsetStart && !IsMoveAllowed(false))
			{
				next = Mathf.Max(next, scrollOffsetStart - dragLimitOffset);
			}
			else if (onlySinglePageSwipeAllowed && pageSize > 0f)
			{
				next = Mathf.Clamp(next, startPageIndex * pageSize, (startPageIndex + 1) * pageSize);
			}

			next = Mathf.Clamp(next, 0f, GetMaxScrollOffset());
			SetScrollOffset(next);

			evt.StopPropagation();
		}

		private void OnPointerUp(PointerUpEvent evt) => OnPointerEnd(evt.pointerId, evt.position);

		private void OnPointerCancel(PointerCancelEvent evt) => OnPointerEnd(evt.pointerId, evt.position);

		private void OnPointerEnd(int pointerId, Vector3 position)
		{
			if (!isPointerDown || pointerId != activePointerId)
			{
				return;
			}

			// The pointer is captured only during a drag.
			if (isDragging)
			{
				viewport.ReleasePointer(pointerId);
			}

			FinishPointerGesture(position);
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

			var absPrimary = Mathf.Abs(orientation == ScrollSnapOrientation.Horizontal ? evt.delta.x : evt.delta.y);
			var absSecondary = Mathf.Abs(orientation == ScrollSnapOrientation.Horizontal ? evt.delta.y : evt.delta.x);
			var isPrimaryGesture = absPrimary > 0f && absPrimary >= absSecondary;

			if (!isPrimaryGesture)
			{
				return;
			}

			ScheduleSnapToNearestPage();
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

			var threshold = pageSize * 0.15f;
			int target;

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
				target = Mathf.RoundToInt(GetScrollOffset() / pageSize);
			}

			target = Mathf.Clamp(target, 0, PageCount - 1);

			if (target == CurrentPageIndex)
			{
				GoToPage(target, animate: true, force: true);
				return;
			}

			if (!validatePageChange)
			{
				OnPageStartChange?.Invoke(target, true);
				GoToPage(target, animate: true, force: true);
				return;
			}

			var moveAllowed = IsMoveAllowed(target > CurrentPageIndex);

			OnPageStartChange?.Invoke(target, moveAllowed);

			if (!moveAllowed)
			{
				GoToPageInternal(CurrentPageIndex, animate: true, force: true, () => OnPageChangeRestricted?.Invoke(target));
				return;
			}

			if (OnValidatePageTransition != null)
			{
				// Fire and forget. The method catches its own exceptions.
				_ = ExecutePageTransitionWithValidation(target);
			}
			else
			{
				GoToPage(target, animate: true, force: true);
			}
		}

		// Awaits OnValidatePageTransition, then goes to the target page or snaps back to the current page.
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
}