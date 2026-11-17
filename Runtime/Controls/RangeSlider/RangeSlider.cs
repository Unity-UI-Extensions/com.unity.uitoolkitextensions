/// Credit Ben MacKinnon @Dover8
/// Sourced from - https://github.com/Dover8/Unity-UI-Extensions/pull/1 (UI Toolkit port of the uGUI Extensions RangeSlider)

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A horizontal slider with two handles that select a low and a high value between a minimum and maximum,
    /// e.g. for filtering search results by a price or size range.
    /// Fires <see cref="OnValueChanged"/> (and a <see cref="ChangeEvent{T}"/> of <see cref="Vector2"/>) whenever the range changes.
    /// </summary>
    /// <remarks>
    /// Interaction:
    /// - Drag either handle to move that end of the range; the handles cannot cross.
    /// - Drag the fill between the handles to move the whole range, keeping its size.
    /// - Press the track outside the range to jump the nearest handle to that point and keep dragging it.
    /// - When both handles overlap, the drag direction decides which handle moves.
    /// - Tab to a handle and use the left / right navigation keys to step it by <see cref="StepSize"/>.
    /// - Optionally, <see cref="SnapToStep"/> snaps user input to multiples of <see cref="StepSize"/>.
    /// If the low / high value sits on <see cref="MinValue"/> / <see cref="MaxValue"/> when that limit changes,
    /// it follows the new limit, so a full-range selection stays full.
    /// </remarks>
    [UxmlElement]
    public partial class RangeSlider : VisualElement, INotifyValueChanged<Vector2>
    {
        public const string RootClass = "rangeSlider";
        public const string TrackClass = "rangeSlider__track";
        public const string FillClass = "rangeSlider__fill";
        public const string HandleClass = "rangeSlider__handle";
        public const string HandleKnobClass = "rangeSlider__handleKnob";
        public const string LowHandleClass = "rangeSlider__handle--low";
        public const string HighHandleClass = "rangeSlider__handle--high";
        public const string HandleActiveClass = "rangeSlider__handle--active";
        public const string FillActiveClass = "rangeSlider__fill--active";
        public const string DraggingClass = "rangeSlider--dragging";

        private const int NoPointer = -1;
        // Tolerance (in steps) so values already on a snap stop are not skipped due to float error.
        private const float StopTolerance = 0.0001f;

        /// <summary>
        /// What the active pointer is currently dragging.
        /// </summary>
        private enum InteractionState
        {
            None,
            Low,
            High,
            Bar,
            /// <summary>Pressed where both handles overlap; the first drag direction picks the handle.</summary>
            Overlapped
        }

        private readonly VisualElement track;
        private readonly VisualElement fill;
        private readonly VisualElement lowHandle;
        private readonly VisualElement highHandle;

        private float minValue;
        private float maxValue = 1f;
        private float lowValue;
        private float highValue = 1f;
        private bool wholeNumbers;
        private float stepSize;
        private bool snapToStep;

        private InteractionState interactionState = InteractionState.None;
        private int activePointerId = NoPointer;
        private float pressPointerX;
        private float pressLowValue;
        private float pressHighValue;
        // The offset from the pressed handle's centre to the pointer, so the handle does not jump on press.
        private float handleDragOffset;

        /// <summary>
        /// Raised with the (low, high) values whenever either value changes through interaction or a notifying setter.
        /// </summary>
        public event Action<float, float> OnValueChanged;

        /// <summary>
        /// Raised with the final (low, high) values when the user finishes a pointer drag that changed the range.
        /// Use this instead of <see cref="OnValueChanged"/> for costly work, e.g. re-running a search filter.
        /// Not raised for changes from code, or for keyboard / gamepad steps.
        /// </summary>
        public event Action<float, float> OnDragEnd;

        public RangeSlider()
        {
            this.ApplyControlStyles();
            AddToClassList(RootClass);

            pickingMode = PickingMode.Position;

            track = UIToolkitExtensions.CreateVisualElement(this, TrackClass);
            track.pickingMode = PickingMode.Ignore;

            fill = UIToolkitExtensions.CreateVisualElement(track, FillClass);
            fill.pickingMode = PickingMode.Ignore;

            lowHandle = CreateHandle(LowHandleClass, isLow: true);
            highHandle = CreateHandle(HighHandleClass, isLow: false);

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            RegisterCallback<DetachFromPanelEvent>(_ => EndDrag(notifyDragEnd: false));

            UpdateVisuals();
        }

        public RangeSlider(float minValue, float maxValue, float lowValue, float highValue, bool wholeNumbers = false)
            : this()
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
            this.wholeNumbers = wholeNumbers;
            SetValuesInternal(lowValue, highValue, notify: false);
        }

        // UXML attributes are applied in declaration order, so keep the limits (min / max) declared
        // before the values (low / high), otherwise the values are clamped to the default 0..1 range.

        /// <summary>
        /// The lowest value the range can start at. Should be less than <see cref="MaxValue"/>.
        /// </summary>
        [UxmlAttribute("min-value")]
        public float MinValue
        {
            get => minValue;
            set
            {
                if (minValue == value)
                {
                    return;
                }

                bool lowPinnedToMin = Mathf.Approximately(lowValue, minValue);
                minValue = value;
                SetValuesInternal(lowPinnedToMin ? minValue : lowValue, highValue, notify: true);
            }
        }

        /// <summary>
        /// The highest value the range can end at. Should be greater than <see cref="MinValue"/>.
        /// </summary>
        [UxmlAttribute("max-value")]
        public float MaxValue
        {
            get => maxValue;
            set
            {
                if (maxValue == value)
                {
                    return;
                }

                bool highPinnedToMax = Mathf.Approximately(highValue, maxValue);
                maxValue = value;
                SetValuesInternal(lowValue, highPinnedToMax ? maxValue : highValue, notify: true);
            }
        }

        /// <summary>
        /// The start of the selected range, clamped between <see cref="MinValue"/> and <see cref="HighValue"/>.
        /// </summary>
        [UxmlAttribute("low-value")]
        public float LowValue
        {
            get => lowValue;
            set => SetLow(value, notify: true);
        }

        /// <summary>
        /// The end of the selected range, clamped between <see cref="LowValue"/> and <see cref="MaxValue"/>.
        /// </summary>
        [UxmlAttribute("high-value")]
        public float HighValue
        {
            get => highValue;
            set => SetHigh(value, notify: true);
        }

        /// <summary>
        /// When true, the low and high values are rounded to whole numbers (use whole-number limits with this option).
        /// </summary>
        [UxmlAttribute("whole-numbers")]
        public bool WholeNumbers
        {
            get => wholeNumbers;
            set
            {
                if (wholeNumbers == value)
                {
                    return;
                }

                wholeNumbers = value;
                SetValuesInternal(lowValue, highValue, notify: true);
            }
        }

        /// <summary>
        /// How far each keyboard / gamepad left or right step moves a handle. Does not affect dragging.
        /// 0 (the default) uses 1% of the range. With <see cref="WholeNumbers"/>, the step is rounded to a whole number of at least 1.
        /// </summary>
        [UxmlAttribute("step-size")]
        public float StepSize
        {
            get => stepSize;
            set => stepSize = Mathf.Max(0f, value);
        }

        /// <summary>
        /// When true, user input snaps to stops every <see cref="StepSize"/> from <see cref="MinValue"/>, with <see cref="MaxValue"/>
        /// always a stop too. Handle drags and track presses go to the nearest stop, range drags move in whole steps, and
        /// keyboard / gamepad steps go to the next stop. Values set from code or UXML are not snapped, and changing this
        /// at runtime does not move the current values. Off by default.
        /// </summary>
        [UxmlAttribute("snap-to-step")]
        public bool SnapToStep
        {
            get => snapToStep;
            set => snapToStep = value;
        }

        /// <summary>
        /// <see cref="LowValue"/> expressed as 0..1 between <see cref="MinValue"/> and <see cref="MaxValue"/>.
        /// </summary>
        public float NormalizedLowValue
        {
            get => Normalize(lowValue);
            set => LowValue = Mathf.Lerp(minValue, maxValue, value);
        }

        /// <summary>
        /// <see cref="HighValue"/> expressed as 0..1 between <see cref="MinValue"/> and <see cref="MaxValue"/>.
        /// </summary>
        public float NormalizedHighValue
        {
            get => Normalize(highValue);
            set => HighValue = Mathf.Lerp(minValue, maxValue, value);
        }

        /// <summary>
        /// The selected range as (low, high). Setting it notifies listeners if the range changes.
        /// </summary>
        public Vector2 value
        {
            get => new Vector2(lowValue, highValue);
            set => SetValuesInternal(value.x, value.y, notify: true);
        }

        // The step applied per keyboard / navigation move, resolving the automatic (0) step size.
        private float EffectiveStepSize
        {
            get
            {
                float step = stepSize > 0f ? stepSize : Mathf.Abs(maxValue - minValue) * 0.01f;
                return wholeNumbers ? Mathf.Max(1f, Mathf.Round(step)) : step;
            }
        }

        private float SnapToNearestStop(float rangeValue)
        {
            float step = EffectiveStepSize;
            float lowerLimit = Mathf.Min(minValue, maxValue);
            float upperLimit = Mathf.Max(minValue, maxValue);
            if (!(step > 0f))
            {
                return rangeValue;
            }

            float stop = lowerLimit + Mathf.Round((rangeValue - lowerLimit) / step) * step;

            // The upper limit is always a stop, even when the range is not a whole multiple of the step.
            if (upperLimit - rangeValue < Mathf.Abs(stop - rangeValue))
            {
                stop = upperLimit;
            }

            return Mathf.Clamp(stop, lowerLimit, upperLimit);
        }

        private float GetNextStop(float rangeValue, float direction)
        {
            float step = EffectiveStepSize;
            float lowerLimit = Mathf.Min(minValue, maxValue);
            float upperLimit = Mathf.Max(minValue, maxValue);
            if (!(step > 0f))
            {
                return rangeValue;
            }

            float stepsFromMin = (rangeValue - lowerLimit) / step;
            float stop = direction > 0f
                ? lowerLimit + (Mathf.Floor(stepsFromMin + StopTolerance) + 1f) * step
                : lowerLimit + (Mathf.Ceil(stepsFromMin - StopTolerance) - 1f) * step;

            return Mathf.Clamp(stop, lowerLimit, upperLimit);
        }

        /// <summary>
        /// Sets the low and high values without raising <see cref="OnValueChanged"/> or a <see cref="ChangeEvent{T}"/>.
        /// </summary>
        /// <param name="low">The new low value.</param>
        /// <param name="high">The new high value; if lower than <paramref name="low"/>, the low value is clamped to it.</param>
        public void SetValueWithoutNotify(float low, float high)
        {
            SetValuesInternal(low, high, notify: false);
        }

        /// <summary>
        /// Sets the range (x = low, y = high) without raising <see cref="OnValueChanged"/> or a <see cref="ChangeEvent{T}"/>.
        /// </summary>
        public void SetValueWithoutNotify(Vector2 newValue)
        {
            SetValuesInternal(newValue.x, newValue.y, notify: false);
        }

        private VisualElement CreateHandle(string modifierClass, bool isLow)
        {
            var handle = UIToolkitExtensions.CreateVisualElement(track, HandleClass, modifierClass);
            handle.pickingMode = PickingMode.Position;
            handle.focusable = true;
            handle.tabIndex = 0;
            handle.RegisterCallback<NavigationMoveEvent>(evt => OnHandleNavigationMove(evt, isLow));

            // The handle is the (larger) touch target; the knob is the visible part.
            var knob = UIToolkitExtensions.CreateVisualElement(handle, HandleKnobClass);
            knob.pickingMode = PickingMode.Ignore;
            return handle;
        }

        private void SetLow(float input, bool notify)
        {
            // Clamp against the current high value, so the low handle cannot pass the high handle.
            SetValuesInternal(Mathf.Min(input, highValue), highValue, notify);
        }

        private void SetHigh(float input, bool notify)
        {
            // Clamp against the current low value, so the high handle cannot pass the low handle.
            SetValuesInternal(lowValue, Mathf.Max(input, lowValue), notify);
        }

        private void SetValuesInternal(float low, float high, bool notify)
        {
            // Order-tolerant limits, as min / max are set one at a time (e.g. from UXML).
            float lowerLimit = Mathf.Min(minValue, maxValue);
            float upperLimit = Mathf.Max(minValue, maxValue);

            high = Mathf.Clamp(high, lowerLimit, upperLimit);
            low = Mathf.Clamp(low, lowerLimit, high);

            if (wholeNumbers)
            {
                low = Mathf.Round(low);
                high = Mathf.Round(high);
            }

            var previousValue = new Vector2(lowValue, highValue);
            bool changed = low != lowValue || high != highValue;

            lowValue = low;
            highValue = high;

            // Always refresh, as limit changes move the handles even when the values are unchanged.
            UpdateVisuals();

            if (changed && notify)
            {
                NotifyValueChanged(previousValue);
            }
        }

        private void NotifyValueChanged(Vector2 previousValue)
        {
            OnValueChanged?.Invoke(lowValue, highValue);

            using (var changeEvent = ChangeEvent<Vector2>.GetPooled(previousValue, value))
            {
                changeEvent.target = this;
                SendEvent(changeEvent);
            }
        }

        private float Normalize(float rangeValue)
        {
            if (Mathf.Approximately(minValue, maxValue))
            {
                return 0f;
            }

            return Mathf.InverseLerp(minValue, maxValue, rangeValue);
        }

        private void UpdateVisuals()
        {
            float normalizedLow = NormalizedLowValue;
            float normalizedHigh = NormalizedHighValue;

            lowHandle.style.left = Length.Percent(normalizedLow * 100f);
            highHandle.style.left = Length.Percent(normalizedHigh * 100f);

            fill.style.left = Length.Percent(normalizedLow * 100f);
            fill.style.width = Length.Percent(Mathf.Max(0f, normalizedHigh - normalizedLow) * 100f);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!enabledInHierarchy || activePointerId != NoPointer)
            {
                return;
            }

            if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && evt.button != 0)
            {
                return;
            }

            float trackWidth = track.layout.width;
            if (!(trackWidth > 0f))
            {
                return;
            }

            float pointerX = track.WorldToLocal(evt.position).x;
            float lowX = NormalizedLowValue * trackWidth;
            float highX = NormalizedHighValue * trackWidth;

            pressPointerX = pointerX;
            pressLowValue = lowValue;
            pressHighValue = highValue;
            handleDragOffset = 0f;

            bool onLowHandle = lowHandle.worldBound.Contains(evt.position);
            bool onHighHandle = highHandle.worldBound.Contains(evt.position);

            if (onLowHandle && onHighHandle)
            {
                // The touch targets overlap: take the nearer handle, and only let the drag direction decide on a tie
                // (e.g. when both handles sit at the same value).
                float distanceToLow = Mathf.Abs(pointerX - lowX);
                float distanceToHigh = Mathf.Abs(pointerX - highX);
                if (Mathf.Approximately(distanceToLow, distanceToHigh))
                {
                    // The grab offset is set once the drag direction picks the handle.
                    interactionState = InteractionState.Overlapped;
                }
                else if (distanceToLow < distanceToHigh)
                {
                    interactionState = InteractionState.Low;
                    handleDragOffset = pointerX - lowX;
                }
                else
                {
                    interactionState = InteractionState.High;
                    handleDragOffset = pointerX - highX;
                }
            }
            else if (onHighHandle)
            {
                interactionState = InteractionState.High;
                handleDragOffset = pointerX - highX;
            }
            else if (onLowHandle)
            {
                interactionState = InteractionState.Low;
                handleDragOffset = pointerX - lowX;
            }
            else if (pointerX > lowX && pointerX < highX)
            {
                interactionState = InteractionState.Bar;
            }
            else
            {
                // Outside the selected range: jump the nearest handle to the pointer and keep dragging it.
                interactionState = pointerX <= lowX ? InteractionState.Low : InteractionState.High;
                DragHandle(pointerX);
            }

            activePointerId = evt.pointerId;
            this.CapturePointer(evt.pointerId);
            UpdateInteractionClasses();
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != activePointerId || interactionState == InteractionState.None)
            {
                return;
            }

            if (!enabledInHierarchy)
            {
                EndDrag(notifyDragEnd: true);
                return;
            }

            float pointerX = track.WorldToLocal(evt.position).x;

            if (interactionState == InteractionState.Overlapped)
            {
                float direction = pointerX - pressPointerX;
                if (Mathf.Approximately(direction, 0f))
                {
                    return;
                }

                interactionState = direction < 0f ? InteractionState.Low : InteractionState.High;
                float pressedHandleValue = interactionState == InteractionState.Low ? pressLowValue : pressHighValue;
                handleDragOffset = pressPointerX - Normalize(pressedHandleValue) * track.layout.width;
                UpdateInteractionClasses();
            }

            if (interactionState == InteractionState.Bar)
            {
                DragBar(pointerX);
            }
            else
            {
                DragHandle(pointerX);
            }

            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            EndDrag(notifyDragEnd: true);
            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            EndDrag(notifyDragEnd: true);
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            EndDrag(notifyDragEnd: true);
        }

        private void DragHandle(float pointerX)
        {
            float rangeValue = TrackXToValue(pointerX - handleDragOffset);
            if (snapToStep)
            {
                rangeValue = SnapToNearestStop(rangeValue);
            }

            if (interactionState == InteractionState.Low)
            {
                SetLow(rangeValue, notify: true);
            }
            else if (interactionState == InteractionState.High)
            {
                SetHigh(rangeValue, notify: true);
            }
        }

        private void DragBar(float pointerX)
        {
            float trackWidth = track.layout.width;
            if (!(trackWidth > 0f))
            {
                return;
            }

            // Move both ends by the drag delta (rather than re-centring on the pointer), keeping the range size.
            float delta = (pointerX - pressPointerX) / trackWidth * (maxValue - minValue);
            float step = EffectiveStepSize;
            if (snapToStep && step > 0f)
            {
                delta = Mathf.Round(delta / step) * step;
            }
            else if (wholeNumbers)
            {
                delta = Mathf.Round(delta);
            }

            float lowerLimit = Mathf.Min(minValue, maxValue);
            float upperLimit = Mathf.Max(minValue, maxValue);
            delta = Mathf.Clamp(delta, lowerLimit - pressLowValue, upperLimit - pressHighValue);

            SetValuesInternal(pressLowValue + delta, pressHighValue + delta, notify: true);
        }

        private float TrackXToValue(float trackX)
        {
            float trackWidth = track.layout.width;
            float normalized = trackWidth > 0f ? Mathf.Clamp01(trackX / trackWidth) : 0f;
            return Mathf.Lerp(minValue, maxValue, normalized);
        }

        private void EndDrag(bool notifyDragEnd)
        {
            if (activePointerId == NoPointer && interactionState == InteractionState.None)
            {
                return;
            }

            // Reset state before releasing, as releasing the capture raises PointerCaptureOutEvent.
            int pointerId = activePointerId;
            activePointerId = NoPointer;
            interactionState = InteractionState.None;
            UpdateInteractionClasses();

            if (pointerId != NoPointer && this.HasPointerCapture(pointerId))
            {
                this.ReleasePointer(pointerId);
            }

            bool rangeChanged = lowValue != pressLowValue || highValue != pressHighValue;
            if (notifyDragEnd && rangeChanged)
            {
                OnDragEnd?.Invoke(lowValue, highValue);
            }
        }

        private void UpdateInteractionClasses()
        {
            bool overlapped = interactionState == InteractionState.Overlapped;
            lowHandle.EnableInClassList(HandleActiveClass, overlapped || interactionState == InteractionState.Low);
            highHandle.EnableInClassList(HandleActiveClass, overlapped || interactionState == InteractionState.High);
            fill.EnableInClassList(FillActiveClass, interactionState == InteractionState.Bar);
            EnableInClassList(DraggingClass, interactionState != InteractionState.None);
        }

        private void OnHandleNavigationMove(NavigationMoveEvent evt, bool isLow)
        {
            float direction;
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left:
                    direction = -1f;
                    break;
                case NavigationMoveEvent.Direction.Right:
                    direction = 1f;
                    break;
                default:
                    // Leave up / down / next / previous to normal focus navigation.
                    return;
            }

            if (!enabledInHierarchy)
            {
                return;
            }

            float current = isLow ? lowValue : highValue;
            float target = snapToStep
                ? GetNextStop(current, direction)
                : current + direction * EffectiveStepSize;

            if (isLow)
            {
                SetLow(target, notify: true);
            }
            else
            {
                SetHigh(target, notify: true);
            }

            // Consume the move so it adjusts the value instead of moving focus to a neighbouring element.
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }
    }
}