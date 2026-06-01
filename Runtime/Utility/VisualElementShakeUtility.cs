// Copyright (c) Reality Collective. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Provides a reusable shake animation for any target VisualElement.
    /// </summary>
    public static class VisualElementShakeUtility
    {
        private sealed class ShakeState
        {
            public ValueAnimation<float> Animation;
            public float BaseX;
            public float BaseY;
            public float BaseZ;
            public EventCallback<DetachFromPanelEvent> DetachCallback;
        }

        private static readonly Dictionary<VisualElement, ShakeState> ActiveShakes = new();

        /// <summary>
        /// Shakes the target element horizontally for the specified wobble count and wobble speed.
        /// </summary>
        /// <param name="target">Target visual element to animate.</param>
        /// <param name="wobbleCount">Number of left/right wobble cycles.</param>
        /// <param name="wobbleDurationMs">Duration in milliseconds for each wobble segment.</param>
        /// <param name="amplitudePixels">Horizontal wobble amplitude in pixels.</param>
        /// <param name="easingCurve">Optional easing curve. Defaults to <see cref="Easing.OutCubic"/>.</param>
        /// <param name="onCompleted">Optional callback invoked when wobble completes.</param>
        public static void Shake(
            VisualElement target,
            int wobbleCount = 3,
            int wobbleDurationMs = 70,
            float amplitudePixels = 10f,
            Func<float, float> easingCurve = null,
            Action onCompleted = null)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            wobbleCount = Mathf.Max(1, wobbleCount);
            wobbleDurationMs = Mathf.Max(1, wobbleDurationMs);
            amplitudePixels = Mathf.Abs(amplitudePixels);

            StopShake(target, resetToBasePosition: true);

            var resolvedTranslate = target.resolvedStyle.translate;
            var state = new ShakeState
            {
                BaseX = resolvedTranslate.x,
                BaseY = resolvedTranslate.y,
                BaseZ = resolvedTranslate.z
            };

            state.DetachCallback = _ => StopShake(target, resetToBasePosition: true);
            target.RegisterCallback(state.DetachCallback);

            ActiveShakes[target] = state;

            var segmentIndex = 0;
            var totalSegments = (wobbleCount * 2) + 1;
            var currentOffset = 0f;

            void AnimateNextSegment()
            {
                if (!ActiveShakes.TryGetValue(target, out var activeState) || activeState != state)
                {
                    return;
                }

                if (segmentIndex >= totalSegments)
                {
                    FinishShake(target, state, onCompleted);
                    return;
                }

                float targetOffset;
                if (segmentIndex == totalSegments - 1)
                {
                    targetOffset = 0f;
                }
                else
                {
                    var direction = segmentIndex % 2 == 0 ? 1f : -1f;
                    targetOffset = amplitudePixels * direction;
                }

                state.Animation = target.experimental.animation.Start(currentOffset, targetOffset, wobbleDurationMs, (_, value) =>
                {
                    ApplyOffset(target, state, value);
                });

                state.Animation.KeepAlive();
                state.Animation.easingCurve = easingCurve ?? Easing.OutCubic;
                state.Animation.onAnimationCompleted += () =>
                {
                    currentOffset = targetOffset;
                    StopAnimation(state);
                    segmentIndex++;
                    AnimateNextSegment();
                };
            }

            AnimateNextSegment();
        }

        /// <summary>
        /// Stops an active shake for the given element.
        /// </summary>
        /// <param name="target">Target visual element with an active shake.</param>
        /// <param name="resetToBasePosition">If true, returns to the original translate position.</param>
        public static void StopShake(VisualElement target, bool resetToBasePosition = true)
        {
            if (target == null || !ActiveShakes.TryGetValue(target, out var state))
            {
                return;
            }

            StopAnimation(state);

            if (resetToBasePosition)
            {
                RestoreBasePosition(target, state);
            }

            if (state.DetachCallback != null)
            {
                target.UnregisterCallback(state.DetachCallback);
            }

            ActiveShakes.Remove(target);
        }

        private static void ApplyOffset(VisualElement target, ShakeState state, float offsetX)
        {
            target.style.translate = new Translate(
                new Length(state.BaseX + offsetX, LengthUnit.Pixel),
                new Length(state.BaseY, LengthUnit.Pixel),
                state.BaseZ);
        }

        private static void RestoreBasePosition(VisualElement target, ShakeState state)
        {
            target.style.translate = new Translate(
                new Length(state.BaseX, LengthUnit.Pixel),
                new Length(state.BaseY, LengthUnit.Pixel),
                state.BaseZ);
        }

        private static void FinishShake(VisualElement target, ShakeState state, Action onCompleted)
        {
            RestoreBasePosition(target, state);

            if (state.DetachCallback != null)
            {
                target.UnregisterCallback(state.DetachCallback);
            }

            ActiveShakes.Remove(target);
            onCompleted?.Invoke();
        }

        private static void StopAnimation(ShakeState state)
        {
            if (state.Animation == null)
            {
                return;
            }

            try
            {
                state.Animation.Stop();
            }
            catch (InvalidOperationException)
            {
                // UI Toolkit can recycle animations while references still exist.
            }
            finally
            {
                state.Animation = null;
            }
        }
    }
}
