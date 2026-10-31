/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Tells a tap from a press and hold. A release before <see cref="ThresholdMs"/> raises <see cref="Tapped"/>;
    /// a press that lasts the threshold raises <see cref="HoldStarted"/>, then <see cref="HoldMoved"/> while the
    /// pointer moves and <see cref="HoldEnded"/> on release.
    /// </summary>
    /// <remarks>
    /// <para>The target captures the pointer, so sliding off it still ends the hold; <see cref="HoldEnded"/> says
    /// whether the release was over the target. A lost capture or a cancelled pointer ends the hold as not released
    /// over the target. A disabled target ignores presses.</para>
    /// <para><see cref="MoveTolerance"/> cancels a press that travels too far before the threshold, so a pan or a
    /// scroll that starts on the target is not taken for a tap or a hold. Positions are panel coordinates.</para>
    /// </remarks>
    public sealed class LongPressManipulator : PointerManipulator
    {
        private int thresholdMs;
        private int pointerId = PointerId.invalidPointerId;
        private bool holding;
        private Vector2 pressPosition;
        private IVisualElementScheduledItem holdTimer;

        /// <summary>
        /// Raised for a press released over the target before the threshold.
        /// </summary>
        public event Action Tapped;

        /// <summary>
        /// Raised when a press has lasted the threshold, with the press position.
        /// </summary>
        public event Action<Vector2> HoldStarted;

        /// <summary>
        /// Raised while a hold moves, with the pointer position.
        /// </summary>
        public event Action<Vector2> HoldMoved;

        /// <summary>
        /// Raised when a hold ends: true when released over the target, false when cancelled or released off it.
        /// </summary>
        public event Action<bool> HoldEnded;

        /// <param name="thresholdMs">How long a press lasts before it becomes a hold.</param>
        public LongPressManipulator(int thresholdMs = 300)
        {
            ThresholdMs = thresholdMs;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        public int ThresholdMs
        {
            get => thresholdMs;
            set => thresholdMs = Mathf.Max(0, value);
        }

        /// <summary>
        /// Travel in pixels that cancels a press before it becomes a hold. Infinity (the default) never cancels.
        /// </summary>
        public float MoveTolerance { get; set; } = float.PositiveInfinity;

        public bool IsPressed => pointerId != PointerId.invalidPointerId;

        public bool IsHolding => holding;

        /// <summary>
        /// Where the current press started, in panel coordinates.
        /// </summary>
        public Vector2 PressPosition => pressPosition;

        /// <summary>
        /// Ends a press or a hold now without a tap; a hold ends as not released over the target.
        /// </summary>
        public void Cancel() => End(released: false);

        /// <summary>
        /// Starts the hold now; called by the threshold timer.
        /// </summary>
        internal void BeginHold()
        {
            if (!IsPressed || holding)
            {
                return;
            }

            holding = true;
            HoldStarted?.Invoke(pressPosition);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.RegisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            Cancel();
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnCaptureOut);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (IsPressed || !CanStartManipulation(evt) || !target.enabledInHierarchy)
            {
                return;
            }

            pointerId = evt.pointerId;
            pressPosition = evt.position;
            target.CapturePointer(pointerId);
            holdTimer = target.schedule.Execute(BeginHold).StartingIn(thresholdMs);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointerId)
            {
                return;
            }

            if (holding)
            {
                HoldMoved?.Invoke(evt.position);
            }
            else if (((Vector2)evt.position - pressPosition).sqrMagnitude > MoveTolerance * MoveTolerance)
            {
                End(released: false);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != pointerId)
            {
                return;
            }

            var over = target.worldBound.Contains(evt.position);
            if (holding)
            {
                End(released: over);
            }
            else
            {
                Reset();
                if (over && target.enabledInHierarchy)
                {
                    Tapped?.Invoke();
                }
            }

            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId == pointerId)
            {
                End(released: false);
            }
        }

        private void OnCaptureOut(PointerCaptureOutEvent evt) => End(released: false);

        private void OnDetach(DetachFromPanelEvent evt) => End(released: false);

        private void End(bool released)
        {
            var wasHolding = holding;
            Reset();
            if (wasHolding)
            {
                HoldEnded?.Invoke(released);
            }
        }

        private void Reset()
        {
            holdTimer?.Pause();
            holdTimer = null;

            // Clear the state before releasing capture: the capture-out event re-enters End.
            holding = false;
            var id = pointerId;
            pointerId = PointerId.invalidPointerId;
            if (id != PointerId.invalidPointerId && target != null && target.HasPointerCapture(id))
            {
                target.ReleasePointer(id);
            }
        }
    }
}
