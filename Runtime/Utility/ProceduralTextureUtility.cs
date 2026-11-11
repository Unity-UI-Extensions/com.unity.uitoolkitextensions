/// Credit SimonDarksideJ

using UnityEngine;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Factory methods for runtime-generated <see cref="Texture2D"/> assets used by UI controls.
    /// The caller owns each returned texture and must destroy it.
    /// </summary>
    public static class ProceduralTextureUtility
    {
        /// <summary>
        /// Creates a 1-pixel-tall gradient texture that runs left to right.
        /// </summary>
        public static Texture2D CreateHorizontalGradient(Color startColor, Color endColor, int width = 256)
        {
            return Gradient(width, 1, startColor, endColor);
        }

        /// <summary>
        /// Creates a 1-pixel-wide gradient texture that runs top to bottom.
        /// </summary>
        public static Texture2D CreateVerticalGradient(Color topColor, Color bottomColor, int height = 256)
        {
            // Texture rows run bottom to top.
            return Gradient(1, height, bottomColor, topColor);
        }

        /// <summary>
        /// Creates a square texture that shows a single-color arc (spinner ring) centred on the top.
        /// </summary>
        /// <param name="sweepDegrees">Degrees of the ring that are filled.</param>
        /// <param name="ringFraction">Ring thickness as a fraction of the radius.</param>
        public static Texture2D CreateSpinnerArc(int size, Color arcColor, float sweepDegrees = 270f, float ringFraction = 0.15f)
        {
            var pixels = new Color[size * size];

            float centre = (size - 1) * 0.5f;
            float innerR = centre * (1f - ringFraction * 2f);
            float halfSweep = sweepDegrees * 0.5f * Mathf.Deg2Rad;
            float startAngle = -Mathf.PI * 0.5f - halfSweep;
            float endAngle = -Mathf.PI * 0.5f + halfSweep;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centre;
                    float dy = y - centre;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < innerR || dist > centre)
                    {
                        continue;
                    }

                    // Normalize the angle to [startAngle, startAngle + 2 pi).
                    float angle = Mathf.Atan2(dy, dx);
                    while (angle < startAngle) angle += Mathf.PI * 2f;
                    while (angle > startAngle + Mathf.PI * 2f) angle -= Mathf.PI * 2f;
                    if (angle <= endAngle)
                    {
                        pixels[y * size + x] = arcColor;
                    }
                }
            }

            return Build(size, size, pixels);
        }

        /// <summary>
        /// Creates a square texture with a solid circle that has a 1-pixel soft edge.
        /// </summary>
        public static Texture2D CreateSolidCircle(int size, Color color)
        {
            var pixels = new Color[size * size];
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                    var c = color;
                    c.a *= Mathf.Clamp01(centre - dist + 0.5f);
                    pixels[y * size + x] = c;
                }
            }

            return Build(size, size, pixels);
        }

        /// <summary>
        /// Destroys a generated texture and clears the reference. Safe in Edit Mode and Play Mode.
        /// </summary>
        internal static void Release(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }

            texture = null;
        }

        // One of width and height is 1, so the pixel index is also the position on the gradient.
        private static Texture2D Gradient(int width, int height, Color from, Color to)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.Lerp(from, to, (float)i / (pixels.Length - 1));
            }

            return Build(width, height, pixels);
        }

        private static Texture2D Build(int width, int height, Color[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
