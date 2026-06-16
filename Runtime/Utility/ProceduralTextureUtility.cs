/// Credit SimonDarksideJ

using UnityEngine;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Factory methods for runtime-generated <see cref="Texture2D"/> assets used by UI controls.
    /// Callers are responsible for calling <c>Object.Destroy</c> on returned textures when no longer needed.
    /// </summary>
    public static class ProceduralTextureUtility
    {
        /// <summary>
        /// Creates a 1-pixel-tall horizontal gradient texture from <paramref name="startColor"/> to <paramref name="endColor"/>.
        /// </summary>
        /// <param name="startColor">Left edge color.</param>
        /// <param name="endColor">Right edge color.</param>
        /// <param name="width">Horizontal resolution in pixels (default 256).</param>
        public static Texture2D CreateHorizontalGradient(Color startColor, Color endColor, int width = 256)
        {
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[width];
            for (int x = 0; x < width; x++)
                pixels[x] = Color.Lerp(startColor, endColor, (float)x / (width - 1));
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a square texture showing a single-color arc (spinner ring).
        /// </summary>
        /// <param name="size">Width and height in pixels.</param>
        /// <param name="arcColor">Color of the visible arc stroke.</param>
        /// <param name="sweepDegrees">How many degrees of the ring are filled (default 270).</param>
        /// <param name="ringFraction">Thickness of the ring as a fraction of the radius (default 0.15).</param>
        public static Texture2D CreateSpinnerArc(int size, Color arcColor, float sweepDegrees = 270f, float ringFraction = 0.15f)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float outerR = cx;
            float innerR = cx * (1f - ringFraction * 2f);
            float halfSweep = sweepDegrees * 0.5f * Mathf.Deg2Rad;
            // Arc runs from -halfSweep to +halfSweep around the top (−90° axis)
            float startAngle = -Mathf.PI * 0.5f - halfSweep;
            float endAngle   = -Mathf.PI * 0.5f + halfSweep;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < innerR || dist > outerR)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }
                    float angle = Mathf.Atan2(dy, dx);
                    // Normalize angle to [startAngle, startAngle + 2π)
                    while (angle < startAngle) angle += Mathf.PI * 2f;
                    while (angle > startAngle + Mathf.PI * 2f) angle -= Mathf.PI * 2f;
                    pixels[y * size + x] = (angle <= endAngle) ? arcColor : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Creates a square texture with a solid anti-aliased circle (useful for avatar placeholders).
        /// </summary>
        /// <param name="size">Width and height in pixels.</param>
        /// <param name="color">Fill color of the circle.</param>
        public static Texture2D CreateSolidCircle(int size, Color color)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float r = cx;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    // 1-pixel soft edge
                    float alpha = Mathf.Clamp01(r - dist + 0.5f);
                    var c = color;
                    c.a *= alpha;
                    pixels[y * size + x] = c;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
