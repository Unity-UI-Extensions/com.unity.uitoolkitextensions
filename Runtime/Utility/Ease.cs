/// Credit SimonDarksideJ

using System;
using UnityEngine;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Simple easing library for UIToolkit animations.
    /// </summary>
    public static class Ease
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

        public static Func<float, float> ResolveEasing(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Linear;
            }

            switch (name.Trim())
            {
                case "Linear": return Linear;
                case "InSine": return InSine;
                case "OutSine": return OutSine;
                case "InOutSine": return InOutSine;
                case "InQuad": return InQuad;
                case "OutQuad": return OutQuad;
                case "InOutQuad": return InOutQuad;
                case "InCubic": return InCubic;
                case "OutCubic": return OutCubic;
                case "InOutCubic": return InOutCubic;
                case "InQuart": return InQuart;
                case "OutQuart": return OutQuart;
                case "InOutQuart": return InOutQuart;
                case "InQuint": return InQuint;
                case "OutQuint": return OutQuint;
                case "InOutQuint": return InOutQuint;
                case "InExpo": return InExpo;
                case "OutExpo": return OutExpo;
                case "InOutExpo": return InOutExpo;
                case "InCirc": return InCirc;
                case "OutCirc": return OutCirc;
                case "InOutCirc": return InOutCirc;
                case "InBack": return InBack;
                case "OutBack": return OutBack;
                case "InOutBack": return InOutBack;
                case "InElastic": return InElastic;
                case "OutElastic": return OutElastic;
                case "InOutElastic": return InOutElastic;
                case "InBounce": return InBounce;
                case "OutBounce": return OutBounce;
                case "InOutBounce": return InOutBounce;
                default: return Linear;
            }
        }
    }
}