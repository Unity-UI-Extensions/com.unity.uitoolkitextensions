/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A vertical list view with iOS-style elastic overscroll, model-agnostic content, and an optional
    /// swipe-up "load more" trigger.
    ///
    /// It hosts its items in one of two viewports and switches automatically:
    /// <list type="bullet">
    /// <item>When the content overflows, a real <see cref="ScrollView"/> scrolls it (with optional
    /// elastic overscroll via <see cref="EnableTouchElasticity"/>).</item>
    /// <item>When the content fits, a manual viewport provides a finger-following bounce with an
    /// ease-out snap-back, so a short list still feels alive instead of being inert.</item>
    /// </list>
    ///
    /// Items are plain <see cref="VisualElement"/>s supplied via <see cref="AddItem"/>/<see cref="SetItems(System.Collections.Generic.IEnumerable{UnityEngine.UIElements.VisualElement})"/>
    /// (or the generic <c>SetItems&lt;T&gt;</c> factory overloads), so the control carries no data-model
    /// dependency. An optional empty-state label is shown via <see cref="EmptyStateText"/>.
    /// </summary>
    /// <remarks>
    /// The control only really functions on device, events in the editor are unpredictable and do not produce the same swiping effect.
    /// </remarks>
    [UxmlElement]
    public partial class ElasticListView : VisualElement
    {
        private const float ManualElasticLimitPx = 100f;
        private const float ManualElasticFollowFactor = 0.5f;
        private const float ManualElasticDragThresholdPx = 6f;
        private const int ManualElasticSnapBackDurationMs = 210;
        private const float ScrollFitTolerancePx = 0.5f;
        private const float LoadMoreOverscrollThresholdPx = 50f;
        private const float LoadMoreManualThresholdPx = 50f;

        public const string RootClass = "elasticListView";
        public const string ScrollContainerClass = "elasticListView__scrollContainer";
        public const string ContentClass = "elasticListView__content";
        public const string EmptyLabelClass = "elasticListView__emptyLabel";
        public const string LoadMoreFooterClass = "elasticListView__loadMoreFooter";
        public const string LoadMoreSpinnerClass = "elasticListView__loadMoreSpinner";

        private readonly ScrollView scrollView;
        private VisualElement scrollViewport;
        private readonly VisualElement manualViewport;
        private readonly VisualElement content;
        private readonly Label emptyLabel;
        private VisualElement loadMoreFooter;
        private LoadingIcon loadMoreSpinner;

        private bool touchElasticityEnabled;
        private float touchElasticityAmount = 0.12f;
        private bool isUsingManualViewport;
        private int activeManualPointerId = -1;
        private bool isManualPointerDown;
        private bool isManualDragging;
        private float manualPointerStartY;
        private float currentManualOffsetY;
        private ValueAnimation<float> manualSnapBackAnimation;

        private bool loadMoreEnabled;
        private bool loadMoreInFlight;
        private bool loadMoreArmed;

        /// <summary>
        /// Raised when the user overscrolls past the bottom edge (swipe-up to load more). Fires once
        /// per gesture and re-arms when the pointer is released. Only active between
        /// <see cref="EnableLoadMore"/> and <see cref="DisableLoadMore"/>, and while no fetch is in
        /// flight (i.e. not between <see cref="BeginLoadMore"/> and <see cref="EndLoadMore"/>).
        /// </summary>
        public event Action LoadMoreRequested;

        public ElasticListView()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };
            scrollView.AddToClassList(ScrollContainerClass);
            Add(scrollView);
            scrollViewport = scrollView.Q<VisualElement>(className: ScrollView.viewportUssClassName);

            manualViewport = UIToolkitExtensions.CreateVisualElement(ScrollContainerClass);
            manualViewport.style.overflow = Overflow.Hidden;
            manualViewport.style.display = DisplayStyle.None;
            Add(manualViewport);

            content = UIToolkitExtensions.CreateVisualElement(scrollView, ContentClass);
            content.style.flexGrow = 0f;
            content.style.flexShrink = 0f;

            emptyLabel = UIToolkitExtensions.CreateVisualElement<Label>(this, EmptyLabelClass);
            emptyLabel.style.display = DisplayStyle.None;

            RegisterCallback<GeometryChangedEvent>(OnListGeometryChanged);
            content.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);
            manualViewport.RegisterCallback<PointerDownEvent>(OnManualElasticPointerDown, TrickleDown.TrickleDown);
            manualViewport.RegisterCallback<PointerMoveEvent>(OnManualElasticPointerMove, TrickleDown.TrickleDown);
            manualViewport.RegisterCallback<PointerUpEvent>(OnManualElasticPointerUp, TrickleDown.TrickleDown);
            manualViewport.RegisterCallback<PointerCancelEvent>(OnManualElasticPointerCancel, TrickleDown.TrickleDown);
            manualViewport.RegisterCallback<PointerCaptureOutEvent>(OnManualElasticPointerCaptureOut);

            scrollView.RegisterCallback<PointerMoveEvent>(OnScrollLoadMorePointerMove, TrickleDown.TrickleDown);
            scrollView.RegisterCallback<PointerUpEvent>(OnScrollLoadMorePointerUp, TrickleDown.TrickleDown);
            scrollView.RegisterCallback<PointerCancelEvent>(OnScrollLoadMorePointerCancel, TrickleDown.TrickleDown);
        }

        private bool HasFooter => loadMoreFooter != null && loadMoreFooter.hierarchy.parent == content;

        public int ItemCount => content == null ? 0 : content.childCount - (HasFooter ? 1 : 0);

        [UxmlAttribute("empty-text")]
        public string EmptyStateText
        {
            get => emptyLabel.text;
            set
            {
                emptyLabel.text = value;
                UpdateEmptyState();
            }
        }

        public void AddItem(VisualElement item)
        {
            InsertItem(item);
            AfterContentMutated(resetScroll: false);
        }

        public void AddItems(IEnumerable<VisualElement> items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    InsertItem(item);
                }
            }

            AfterContentMutated(resetScroll: false);
        }

        public void SetItems(IEnumerable<VisualElement> items)
        {
            ClearItemsInternal();
            if (items != null)
            {
                foreach (var item in items)
                {
                    InsertItem(item);
                }
            }

            AfterContentMutated(resetScroll: true);
        }

        public void SetItems<T>(IReadOnlyList<T> data, Func<T, VisualElement> factory)
        {
            ClearItemsInternal();
            BuildItems(data, factory);
            AfterContentMutated(resetScroll: true);
        }

        public void AddItems<T>(IReadOnlyList<T> data, Func<T, VisualElement> factory)
        {
            BuildItems(data, factory);
            AfterContentMutated(resetScroll: false);
        }

        public void ClearItems()
        {
            ClearItemsInternal();
            AfterContentMutated(resetScroll: true);
        }

        private void BuildItems<T>(IReadOnlyList<T> data, Func<T, VisualElement> factory)
        {
            if (data == null || factory == null)
            {
                return;
            }

            for (int i = 0; i < data.Count; i++)
            {
                var element = factory(data[i]);
                if (element != null)
                {
                    InsertItem(element);
                }
            }
        }

        private void InsertItem(VisualElement item)
        {
            if (item == null)
            {
                return;
            }

            if (HasFooter)
            {
                content.Insert(content.IndexOf(loadMoreFooter), item);
            }
            else
            {
                content.Add(item);
            }
        }

        private void ClearItemsInternal()
        {
            ResetManualElasticInteraction(resetOffset: true, releaseCapture: true);

            for (int i = content.childCount - 1; i >= 0; i--)
            {
                if (content[i] == loadMoreFooter)
                {
                    continue;
                }

                content.RemoveAt(i);
            }
        }

        private void AfterContentMutated(bool resetScroll)
        {
            UpdateEmptyState();

            schedule.Execute(() =>
            {
                if (resetScroll && scrollView != null)
                {
                    scrollView.scrollOffset = Vector2.zero;
                }

                RefreshLayout();
            }).ExecuteLater(0);
        }

        public void EnableTouchElasticity(float elasticity = 0.12f)
        {
            if (scrollView == null)
            {
                return;
            }

            touchElasticityEnabled = true;
            touchElasticityAmount = Mathf.Max(0f, elasticity);
            RefreshViewportMode();
        }

        public void RefreshLayout()
        {
            if (scrollView == null || content == null || manualViewport == null)
            {
                return;
            }

            UpdateEmptyState();

            if (ItemCount == 0)
            {
                return;
            }

            RefreshViewportMode();
        }

        private void UpdateEmptyState()
        {
            bool empty = ItemCount == 0;

            if (emptyLabel != null)
            {
                emptyLabel.style.display = empty && !string.IsNullOrEmpty(emptyLabel.text)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (empty)
            {
                scrollView.style.display = DisplayStyle.None;
                manualViewport.style.display = DisplayStyle.None;
            }
        }

        private void OnListGeometryChanged(GeometryChangedEvent evt)
        {
            RefreshLayout();
        }

        private void OnContentGeometryChanged(GeometryChangedEvent evt)
        {
            RefreshLayout();
        }

        private void RefreshViewportMode()
        {
            if (scrollView == null || content == null || manualViewport == null)
            {
                return;
            }

            bool shouldUseManualViewport = ContentFitsViewport();
            EnsureViewportHost(shouldUseManualViewport);

            manualViewport.style.display = shouldUseManualViewport ? DisplayStyle.Flex : DisplayStyle.None;
            scrollView.style.display = shouldUseManualViewport ? DisplayStyle.None : DisplayStyle.Flex;

            if (shouldUseManualViewport)
            {
                scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                scrollView.scrollOffset = Vector2.zero;
                return;
            }

            ResetManualElasticInteraction(resetOffset: true, releaseCapture: true);

            if (touchElasticityEnabled)
            {
                scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
                scrollView.elasticity = touchElasticityAmount;
            }
            else
            {
                scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            }
        }

        private void EnsureViewportHost(bool useManualViewport)
        {
            if (content == null || scrollView == null || manualViewport == null)
            {
                return;
            }

            var expectedParent = useManualViewport ? manualViewport : scrollView.contentContainer;
            bool hostAlreadyCorrect = isUsingManualViewport == useManualViewport && content.hierarchy.parent == expectedParent;
            if (hostAlreadyCorrect)
            {
                return;
            }

            ResetManualElasticInteraction(resetOffset: true, releaseCapture: true);
            content.RemoveFromHierarchy();

            if (useManualViewport)
            {
                manualViewport.Add(content);
                manualViewport.style.display = DisplayStyle.Flex;
                scrollView.style.display = DisplayStyle.None;
                scrollView.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
                scrollView.scrollOffset = Vector2.zero;
            }
            else
            {
                scrollView.Add(content);
                scrollView.style.display = DisplayStyle.Flex;
                manualViewport.style.display = DisplayStyle.None;
                scrollView.scrollOffset = Vector2.zero;
            }

            isUsingManualViewport = useManualViewport;
        }

        private float GetViewportHeight()
        {
            if (isUsingManualViewport)
            {
                if (manualViewport != null && manualViewport.layout.height > 0f)
                {
                    return manualViewport.layout.height;
                }

                if (manualViewport != null && manualViewport.contentRect.height > 0f)
                {
                    return manualViewport.contentRect.height;
                }
            }

            if (scrollViewport == null && scrollView != null)
            {
                scrollViewport = scrollView.Q<VisualElement>(className: ScrollView.viewportUssClassName);
            }

            if (scrollViewport != null && scrollViewport.layout.height > 0f)
            {
                return scrollViewport.layout.height;
            }

            if (scrollView != null && scrollView.contentRect.height > 0f)
            {
                return scrollView.contentRect.height;
            }

            if (manualViewport != null && manualViewport.contentRect.height > 0f)
            {
                return manualViewport.contentRect.height;
            }

            return 0f;
        }

        private float GetContentHeight()
        {
            if (content == null)
            {
                return 0f;
            }

            if (content.layout.height > 0f)
            {
                return content.layout.height;
            }

            return content.contentRect.height;
        }

        private bool ContentFitsViewport()
        {
            float viewportHeight = GetViewportHeight();
            float contentHeight = GetContentHeight();

            if (viewportHeight <= 0f)
            {
                return true;
            }

            return contentHeight <= viewportHeight + ScrollFitTolerancePx;
        }

        public void EnableLoadMore()
        {
            loadMoreEnabled = true;
            loadMoreArmed = true;
            EnsureLoadMoreFooter();
        }

        public void DisableLoadMore()
        {
            loadMoreEnabled = false;
            loadMoreInFlight = false;
            loadMoreArmed = false;

            loadMoreSpinner?.StopLoading();
            loadMoreFooter?.RemoveFromHierarchy();
            loadMoreFooter = null;
            loadMoreSpinner = null;
        }

        public void BeginLoadMore()
        {
            loadMoreInFlight = true;
            EnsureLoadMoreFooter();

            if (loadMoreFooter != null)
            {
                loadMoreFooter.style.display = DisplayStyle.Flex;
            }

            loadMoreSpinner?.PlayLoading();
        }

        public void EndLoadMore()
        {
            loadMoreInFlight = false;
            loadMoreSpinner?.StopLoading();

            if (loadMoreFooter != null)
            {
                loadMoreFooter.style.display = DisplayStyle.None;
            }
        }

        private void EnsureLoadMoreFooter()
        {
            if (content == null || !loadMoreEnabled)
            {
                return;
            }

            if (loadMoreFooter == null)
            {
                loadMoreFooter = UIToolkitExtensions.CreateVisualElement(LoadMoreFooterClass);
                loadMoreFooter.pickingMode = PickingMode.Ignore;
                loadMoreFooter.style.display = DisplayStyle.None;
                loadMoreSpinner = UIToolkitExtensions.CreateVisualElement<LoadingIcon>(loadMoreFooter, LoadMoreSpinnerClass);
            }

            if (loadMoreFooter.hierarchy.parent != content)
            {
                loadMoreFooter.RemoveFromHierarchy();
                content.Add(loadMoreFooter);
            }
            else if (content.IndexOf(loadMoreFooter) != content.childCount - 1)
            {
                loadMoreFooter.RemoveFromHierarchy();
                content.Add(loadMoreFooter);
            }
        }

        private void RequestLoadMore()
        {
            if (!loadMoreEnabled || loadMoreInFlight || !loadMoreArmed)
            {
                return;
            }

            loadMoreArmed = false;
            LoadMoreRequested?.Invoke();
        }

        private void ReArmLoadMore()
        {
            if (loadMoreEnabled)
            {
                loadMoreArmed = true;
            }
        }

        private void OnScrollLoadMorePointerMove(PointerMoveEvent evt)
        {
            if (!loadMoreEnabled || loadMoreInFlight || !loadMoreArmed || isUsingManualViewport || scrollView == null)
            {
                return;
            }

            if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && (evt.pressedButtons & 1) == 0)
            {
                return;
            }

            if (evt.deltaPosition.y >= 0f)
            {
                return;
            }

            if (IsAtScrollBottom(LoadMoreOverscrollThresholdPx))
            {
                RequestLoadMore();
            }
        }

        private void OnScrollLoadMorePointerUp(PointerUpEvent evt)
        {
            ReArmLoadMore();
        }

        private void OnScrollLoadMorePointerCancel(PointerCancelEvent evt)
        {
            ReArmLoadMore();
        }

        private bool IsAtScrollBottom(float thresholdPx)
        {
            if (scrollView == null)
            {
                return false;
            }

            float maxScrollY = Mathf.Max(0f, GetContentHeight() - GetViewportHeight());
            return scrollView.scrollOffset.y >= maxScrollY - thresholdPx;
        }

        private void OnManualElasticPointerDown(PointerDownEvent evt)
        {
            if (!ShouldHandleManualElastic(evt))
            {
                return;
            }

            StopManualSnapBackAnimation();
            activeManualPointerId = evt.pointerId;
            isManualPointerDown = true;
            isManualDragging = false;
            manualPointerStartY = evt.position.y;
        }

        private void OnManualElasticPointerMove(PointerMoveEvent evt)
        {
            if (!isManualPointerDown || evt.pointerId != activeManualPointerId || !isUsingManualViewport)
            {
                return;
            }

            float deltaY = evt.position.y - manualPointerStartY;

            if (!isManualDragging && Mathf.Abs(deltaY) < ManualElasticDragThresholdPx)
            {
                return;
            }

            if (!isManualDragging)
            {
                isManualDragging = true;
                manualViewport.CapturePointer(evt.pointerId);
            }

            ApplyManualElasticOffset(Mathf.Clamp(deltaY * ManualElasticFollowFactor, -ManualElasticLimitPx, ManualElasticLimitPx));

            if (currentManualOffsetY <= -LoadMoreManualThresholdPx)
            {
                RequestLoadMore();
            }

            evt.StopImmediatePropagation();
        }

        private void OnManualElasticPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != activeManualPointerId)
            {
                return;
            }

            ReArmLoadMore();

            bool shouldStopPropagation = isManualDragging || Mathf.Abs(currentManualOffsetY) > 0.01f;
            CompleteManualElasticInteraction();

            if (shouldStopPropagation)
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnManualElasticPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != activeManualPointerId)
            {
                return;
            }

            ReArmLoadMore();

            bool shouldStopPropagation = isManualDragging || Mathf.Abs(currentManualOffsetY) > 0.01f;
            AbortManualElasticInteraction();

            if (shouldStopPropagation)
            {
                evt.StopImmediatePropagation();
            }
        }

        private void OnManualElasticPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != activeManualPointerId || !isManualPointerDown)
            {
                return;
            }

            AbortManualElasticInteraction();
        }

        private bool ShouldHandleManualElastic(IPointerEvent evt)
        {
            if (!isUsingManualViewport || manualViewport == null || content == null)
            {
                return false;
            }

            if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && evt.button != 0)
            {
                return false;
            }

            return activeManualPointerId == -1;
        }

        private void CompleteManualElasticInteraction()
        {
            int pointerId = activeManualPointerId;
            bool shouldSnapBack = Mathf.Abs(currentManualOffsetY) > 0.01f;

            ClearManualPointerState();
            ReleaseManualPointerCapture(pointerId);

            if (shouldSnapBack)
            {
                StartManualSnapBackAnimation();
            }
            else
            {
                ApplyManualElasticOffset(0f);
            }
        }

        private void AbortManualElasticInteraction()
        {
            int pointerId = activeManualPointerId;

            ClearManualPointerState();
            ReleaseManualPointerCapture(pointerId);
            StartManualSnapBackAnimation();
        }

        private void StartManualSnapBackAnimation()
        {
            if (content == null || Mathf.Abs(currentManualOffsetY) <= 0.01f)
            {
                ApplyManualElasticOffset(0f);
                return;
            }

            StopManualSnapBackAnimation();

            manualSnapBackAnimation = content.experimental.animation.Start(
                currentManualOffsetY,
                0f,
                ManualElasticSnapBackDurationMs,
                (_, value) => ApplyManualElasticOffset(value));
            manualSnapBackAnimation.easingCurve = EaseOutCubic;
            manualSnapBackAnimation.KeepAlive();
            manualSnapBackAnimation.onAnimationCompleted += () =>
            {
                StopManualSnapBackAnimation();
                ApplyManualElasticOffset(0f);
            };
        }

        private void StopManualSnapBackAnimation()
        {
            try
            {
                manualSnapBackAnimation?.Stop();
            }
            catch
            { }
            finally
            {
                manualSnapBackAnimation = null;
            }
        }

        private void ApplyManualElasticOffset(float offsetY)
        {
            currentManualOffsetY = offsetY;

            if (content != null)
            {
                content.style.translate = new Translate(0f, offsetY, 0f);
            }
        }

        private void ResetManualElasticInteraction(bool resetOffset, bool releaseCapture)
        {
            StopManualSnapBackAnimation();

            int pointerId = activeManualPointerId;
            ClearManualPointerState();

            if (releaseCapture)
            {
                ReleaseManualPointerCapture(pointerId);
            }

            if (resetOffset)
            {
                ApplyManualElasticOffset(0f);
            }
        }

        private void ClearManualPointerState()
        {
            activeManualPointerId = -1;
            isManualPointerDown = false;
            isManualDragging = false;
            manualPointerStartY = 0f;
        }

        private void ReleaseManualPointerCapture(int pointerId)
        {
            if (manualViewport == null || pointerId == -1 || !manualViewport.HasPointerCapture(pointerId))
            {
                return;
            }

            manualViewport.ReleasePointer(pointerId);
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}