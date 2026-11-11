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
    /// A reusable shake animation for any VisualElement.
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
        /// <param name="wobbleDurationMs">Duration in milliseconds of each wobble segment.</param>
        /// <param name="amplitudePixels">Horizontal wobble amplitude in pixels.</param>
        /// <param name="easingCurve">Optional easing curve. Defaults to <see cref="Easing.OutCubic"/>.</param>
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
                    StopShake(target, resetToBasePosition: true);
                    onCompleted?.Invoke();
                    return;
                }

                // The last segment returns to rest. The others alternate right and left.
                var targetOffset = segmentIndex == totalSegments - 1 ? 0f
                    : segmentIndex % 2 == 0 ? amplitudePixels : -amplitudePixels;

                state.Animation = target.experimental.animation.Start(currentOffset, targetOffset, wobbleDurationMs,
                    (_, value) => ApplyOffset(target, state, value));

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
        /// <param name="resetToBasePosition">If true, returns the element to its original translate position.</param>
        public static void StopShake(VisualElement target, bool resetToBasePosition = true)
        {
            if (target == null || !ActiveShakes.TryGetValue(target, out var state))
            {
                return;
            }

            StopAnimation(state);

            if (resetToBasePosition)
            {
                ApplyOffset(target, state, 0f);
            }

            target.UnregisterCallback(state.DetachCallback);
            ActiveShakes.Remove(target);
        }

        private static void ApplyOffset(VisualElement target, ShakeState state, float offsetX)
        {
            target.style.translate = new Translate(
                new Length(state.BaseX + offsetX, LengthUnit.Pixel),
                new Length(state.BaseY, LengthUnit.Pixel),
                state.BaseZ);
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
