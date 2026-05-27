/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityUIToolkit.Extensions
{
    public sealed class ToastSwipeDismissManipulator : PointerManipulator
    {
        private enum SwipeAxis
        {
            None,
            Horizontal,
            Vertical,
        }

        private const float TapSlop = 8f;
        private const float SwipeAxisLockThreshold = 12f;
        private const float SwipeDismissThreshold = 80f;
        private const float DismissVelocityThreshold = 1100f;
        private const float DragFollowFactor = 1f;
        private const int SnapBackDurationMs = 140;
        private const int DismissDurationMs = 180;

        private readonly Func<bool> canInteract;
        private readonly Func<Vector2, bool> canStartAtPosition;
        private readonly Func<float> getHorizontalDismissTravelDistance;
        private readonly Func<float> getVerticalDismissTravelDistance;
        private readonly Func<VisualElement> getOffsetTarget;
        private readonly Action onInteractionStarted;
        private readonly Action onInteractionAborted;
        private readonly Action onTapped;
        private readonly Action onDismissed;

        private ValueAnimation<float> snapBackAnimation;
        private ValueAnimation<float> dismissAnimation;
        private int activePointerId = -1;
        private bool isPointerDown;
        private bool hasSwipeAxisLock;
        private SwipeAxis swipeAxis = SwipeAxis.None;
        private SwipeAxis offsetAxis = SwipeAxis.None;
        private bool isDragging;
        private bool isDismissAnimating;
        private Vector3 pointerStart;
        private Vector3 lastPointerPosition;
        private float lastPointerSampleTime;
        private float pointerVelocityX;
        private float pointerVelocityY;
        private float currentOffset;
        private int moveEventCount;

        public ToastSwipeDismissManipulator(
            Func<bool> canInteract,
            Func<Vector2, bool> canStartAtPosition,
            Func<float> getHorizontalDismissTravelDistance,
            Func<float> getVerticalDismissTravelDistance,
            Func<VisualElement> getOffsetTarget,
            Action onInteractionStarted,
            Action onInteractionAborted,
            Action onTapped,
            Action onDismissed)
        {
            this.canInteract = canInteract ?? throw new ArgumentNullException(nameof(canInteract));
            this.canStartAtPosition = canStartAtPosition ?? throw new ArgumentNullException(nameof(canStartAtPosition));
            this.getHorizontalDismissTravelDistance = getHorizontalDismissTravelDistance ?? throw new ArgumentNullException(nameof(getHorizontalDismissTravelDistance));
            this.getVerticalDismissTravelDistance = getVerticalDismissTravelDistance ?? throw new ArgumentNullException(nameof(getVerticalDismissTravelDistance));
            this.getOffsetTarget = getOffsetTarget ?? throw new ArgumentNullException(nameof(getOffsetTarget));
            this.onInteractionStarted = onInteractionStarted;
            this.onInteractionAborted = onInteractionAborted;
            this.onTapped = onTapped;
            this.onDismissed = onDismissed;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.RegisterCallback<PointerCaptureEvent>(OnPointerCaptured);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.UnregisterCallback<PointerCaptureEvent>(OnPointerCaptured);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            ResetState();
        }

        public void ResetState()
        {
            StopSnapBackAnimation();
            StopDismissAnimation();

            int pointerId = activePointerId;
            ClearPointerState();
            ReleasePointerCapture(pointerId);
            ApplyOffset(0f);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            bool interactionAllowed = target != null && canInteract() && !isDismissAnimating && !isPointerDown;

            if (!interactionAllowed)
            {
                return;
            }

            if (evt.pointerType == UnityEngine.UIElements.PointerType.mouse && evt.button != 0)
            {
                return;
            }

            if (!canStartAtPosition(evt.position))
            {
                return;
            }

            StopSnapBackAnimation();
            StopDismissAnimation();

            activePointerId = evt.pointerId;
            isPointerDown = true;
            hasSwipeAxisLock = false;
            swipeAxis = SwipeAxis.None;
            isDragging = false;
            pointerStart = evt.position;
            lastPointerPosition = evt.position;
            lastPointerSampleTime = Time.realtimeSinceStartup;
            pointerVelocityX = 0f;
            pointerVelocityY = 0f;
            moveEventCount = 0;

            onInteractionStarted?.Invoke();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!isPointerDown || evt.pointerId != activePointerId || target == null)
            {
                return;
            }

            Vector3 delta = evt.position - pointerStart;
            float currentSampleTime = Time.realtimeSinceStartup;
            float sampleDeltaTime = Mathf.Max(currentSampleTime - lastPointerSampleTime, 0.0001f);
            pointerVelocityX = (evt.position.x - lastPointerPosition.x) / sampleDeltaTime;
            pointerVelocityY = (evt.position.y - lastPointerPosition.y) / sampleDeltaTime;
            lastPointerPosition = evt.position;
            lastPointerSampleTime = currentSampleTime;
            moveEventCount++;

            if (!hasSwipeAxisLock && delta.magnitude >= SwipeAxisLockThreshold)
            {
                bool isHorizontalDrag = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
                if (isHorizontalDrag)
                {
                    hasSwipeAxisLock = true;
                    swipeAxis = SwipeAxis.Horizontal;
                }
                else if (delta.y < 0f)
                {
                    hasSwipeAxisLock = true;
                    swipeAxis = SwipeAxis.Vertical;
                }

                if (hasSwipeAxisLock)
                {
                    offsetAxis = swipeAxis;
                    isDragging = true;
                    target.CapturePointer(evt.pointerId);
                }
            }

            bool hasCapture = target.HasPointerCapture(activePointerId);

            if (!hasSwipeAxisLock || swipeAxis == SwipeAxis.None || !hasCapture)
            {
                return;
            }

            float axisDelta = swipeAxis == SwipeAxis.Horizontal ? delta.x : Mathf.Min(delta.y, 0f);
            float maxFollowOffset = Mathf.Max(GetDismissTravelDistance(), 0f);
            float followOffset = Mathf.Clamp(axisDelta * DragFollowFactor, -maxFollowOffset, maxFollowOffset);
            ApplyOffset(followOffset);
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            CompletePointerInteraction(evt.position, evt);
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }

            bool shouldStopPropagation = isDragging;
            AbortInteraction(animateSnapBack: true);

            if (shouldStopPropagation)
            {
                evt.StopPropagation();
            }
        }

        private void OnPointerCaptured(PointerCaptureEvent evt)
        {
            if (evt.pointerId != activePointerId)
            {
                return;
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (evt.pointerId != activePointerId || !isPointerDown)
            {
                return;
            }

            AbortInteraction(animateSnapBack: true);
        }

        private void CompletePointerInteraction(Vector3 pointerPosition, EventBase evt)
        {
            Vector3 delta = pointerPosition - pointerStart;
            bool shouldDismiss = ShouldDismissOnRelease(delta);
            bool shouldTap = ShouldInvokeTap(delta);
            bool shouldStopPropagation = isDragging || shouldTap;
            float dismissDirection = DetermineDismissDirection(delta);
            float dismissTravelDistance = GetDismissTravelDistance();
            int pointerId = activePointerId;

            ClearPointerState();
            ReleasePointerCapture(pointerId);

            if (shouldDismiss)
            {
                StartDismissAnimation(dismissDirection * dismissTravelDistance);
            }
            else if (Mathf.Abs(currentOffset) > 0.01f)
            {
                StartSnapBackAnimation();
                onInteractionAborted?.Invoke();
            }
            else if (!shouldTap)
            {
                onInteractionAborted?.Invoke();
            }

            if (shouldTap)
            {
                onTapped?.Invoke();
            }

            if (shouldStopPropagation)
            {
                evt.StopPropagation();
            }
        }

        private void AbortInteraction(bool animateSnapBack)
        {
            int pointerId = activePointerId;
            bool shouldRestart = isPointerDown;

            ClearPointerState();
            ReleasePointerCapture(pointerId);

            if (animateSnapBack)
            {
                StartSnapBackAnimation();
            }
            else
            {
                ApplyOffset(0f);
            }

            if (shouldRestart)
            {
                onInteractionAborted?.Invoke();
            }
        }

        private bool ShouldDismissOnRelease(Vector3 delta)
        {
            if (!hasSwipeAxisLock)
            {
                return false;
            }

            switch (swipeAxis)
            {
                case SwipeAxis.Horizontal:
                {
                    bool exceededDistance = Mathf.Abs(delta.x) >= SwipeDismissThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
                    bool exceededVelocity = Mathf.Abs(pointerVelocityX) >= DismissVelocityThreshold && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 0.5f;
                    return exceededDistance || exceededVelocity;
                }

                case SwipeAxis.Vertical:
                {
                    bool movingUp = delta.y < 0f || pointerVelocityY < 0f;
                    bool exceededDistance = Mathf.Abs(delta.y) >= SwipeDismissThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x);
                    bool exceededVelocity = Mathf.Abs(pointerVelocityY) >= DismissVelocityThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 0.5f;
                    return movingUp && (exceededDistance || exceededVelocity);
                }

                default:
                    return false;
            }
        }

        private bool ShouldInvokeTap(Vector3 delta)
        {
            if (hasSwipeAxisLock)
            {
                return false;
            }

            return delta.sqrMagnitude <= TapSlop * TapSlop;
        }

        private float DetermineDismissDirection(Vector3 delta)
        {
            if (swipeAxis == SwipeAxis.Horizontal)
            {
                if (!Mathf.Approximately(delta.x, 0f))
                {
                    return Mathf.Sign(delta.x);
                }

                if (!Mathf.Approximately(pointerVelocityX, 0f))
                {
                    return Mathf.Sign(pointerVelocityX);
                }

                return 1f;
            }

            if (!Mathf.Approximately(delta.y, 0f))
            {
                return Mathf.Sign(delta.y);
            }

            if (!Mathf.Approximately(pointerVelocityY, 0f))
            {
                return Mathf.Sign(pointerVelocityY);
            }

            return -1f;
        }

        private void StartSnapBackAnimation()
        {
            if (target == null || isDismissAnimating || Mathf.Abs(currentOffset) <= 0.01f)
            {
                ApplyOffset(0f);
                return;
            }

            StopSnapBackAnimation();

            snapBackAnimation = target.experimental.animation.Start(currentOffset, 0f, SnapBackDurationMs, (_, value) => ApplyOffset(value));
            snapBackAnimation.easingCurve = EaseOutCubic;
            snapBackAnimation.KeepAlive();
            snapBackAnimation.onAnimationCompleted += () =>
            {
                StopSnapBackAnimation();
                ApplyOffset(0f);
            };
        }

        private void StopSnapBackAnimation()
        {
            try
            {
                snapBackAnimation?.Stop();
            }
            catch
            {
            }
            finally
            {
                snapBackAnimation = null;
            }
        }

        private void StartDismissAnimation(float targetOffset)
        {
            if (target == null)
            {
                onDismissed?.Invoke();
                return;
            }

            StopDismissAnimation();
            StopSnapBackAnimation();

            isDismissAnimating = true;

            dismissAnimation = target.experimental.animation.Start(currentOffset, targetOffset, DismissDurationMs, (_, value) => ApplyOffset(value));
            dismissAnimation.easingCurve = EaseOutCubic;
            dismissAnimation.KeepAlive();
            dismissAnimation.onAnimationCompleted += () =>
            {
                dismissAnimation = null;
                onDismissed?.Invoke();
                isDismissAnimating = false;
            };
        }

        private void StopDismissAnimation()
        {
            try
            {
                dismissAnimation?.Stop();
            }
            catch
            {
            }
            finally
            {
                dismissAnimation = null;
                isDismissAnimating = false;
            }
        }

        private void ReleasePointerCapture(int pointerId)
        {
            if (target == null || pointerId == -1 || !target.HasPointerCapture(pointerId))
            {
                return;
            }

            target.ReleasePointer(pointerId);
        }

        private void ApplyOffset(float offset)
        {
            currentOffset = offset;

            VisualElement offsetTarget = getOffsetTarget();
            if (offsetTarget != null)
            {
                offsetTarget.style.translate = offsetAxis switch
                {
                    SwipeAxis.Vertical => new Translate(0f, offset, 0f),
                    SwipeAxis.Horizontal => new Translate(offset, 0f, 0f),
                    _ => new Translate(0f, 0f, 0f),
                };
            }

            if (Mathf.Abs(offset) <= 0.01f)
            {
                offsetAxis = SwipeAxis.None;
            }
        }

        private float GetDismissTravelDistance()
        {
            return swipeAxis switch
            {
                SwipeAxis.Vertical => getVerticalDismissTravelDistance(),
                SwipeAxis.Horizontal => getHorizontalDismissTravelDistance(),
                _ => 0f,
            };
        }

        private void ClearPointerState()
        {
            activePointerId = -1;
            isPointerDown = false;
            hasSwipeAxisLock = false;
            swipeAxis = SwipeAxis.None;
            isDragging = false;
            pointerVelocityX = 0f;
            pointerVelocityY = 0f;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
