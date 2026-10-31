/// Credit SimonDarksideJ

using System;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Draws a QR code for a string, such as a visitor link, with a quiet zone, dark modules on a light ground.
    /// </summary>
    /// <remarks>
    /// <para>Encoding is self-contained (<see cref="QrEncoder"/>); no barcode library is needed. The code is drawn as
    /// a point-filtered texture, one pixel per module, scaled to the element's size.</para>
    /// <para>Props: <c>text</c>, <c>error-correction</c> (default Medium), <c>quiet-zone</c> (modules of light border,
    /// default 4, the minimum scanners expect), <c>size</c> (sets width and height; 0 keeps the USS size of 200 px).
    /// Methods: <see cref="CreateTexture"/> returns a new texture you own, at a chosen scale, to share or save as an
    /// image. <see cref="Texture"/> is the view's own texture; it is destroyed when the view leaves its panel.</para>
    /// <para>Text that does not fit in a QR code logs a warning and leaves the view empty.</para>
    /// <para>USS classes: <c>qrCodeView</c> (root). Modifier: <c>qrCodeView--empty</c> (no text, or too long).</para>
    /// <para>Custom properties on the root: <c>--qr-dark</c> (default <c>--c-navy-2</c>) and <c>--qr-light</c>
    /// (default white). Keep them high in contrast so the code scans.</para>
    /// <para>Tokens read: <c>--c-navy-2</c>, <c>--radius-sm</c>.</para>
    /// </remarks>
    [UxmlElement]
    public partial class QRCodeView : VisualElement
    {
        public const string RootClass = "qrCodeView";
        public const string EmptyClass = "qrCodeView--empty";
        public const int DefaultQuietZone = 4;

        private static readonly CustomStyleProperty<Color> DarkProperty = new CustomStyleProperty<Color>("--qr-dark");
        private static readonly CustomStyleProperty<Color> LightProperty = new CustomStyleProperty<Color>("--qr-light");

        private string text = string.Empty;
        private QrErrorCorrection errorCorrection = QrErrorCorrection.Medium;
        private int quietZone = DefaultQuietZone;
        private float size;
        private QrCode code;
        private Texture2D texture;
        private Color darkColor = Color.black;
        private Color lightColor = Color.white;

        /// <summary>
        /// The content to encode; empty clears the view.
        /// </summary>
        [UxmlAttribute("text")]
        public string Text
        {
            get => text;
            set
            {
                text = value ?? string.Empty;
                Encode();
            }
        }

        [UxmlAttribute("error-correction")]
        public QrErrorCorrection ErrorCorrection
        {
            get => errorCorrection;
            set
            {
                errorCorrection = value;
                Encode();
            }
        }

        /// <summary>
        /// The light border around the code, in modules.
        /// </summary>
        [UxmlAttribute("quiet-zone")]
        public int QuietZone
        {
            get => quietZone;
            set
            {
                quietZone = Mathf.Max(0, value);
                Redraw();
            }
        }

        /// <summary>
        /// Width and height in pixels; 0 keeps the USS size.
        /// </summary>
        [UxmlAttribute("size")]
        public float Size
        {
            get => size;
            set
            {
                size = Mathf.Max(0f, value);
                style.width = size > 0f ? new StyleLength(size) : new StyleLength(StyleKeyword.Null);
                style.height = size > 0f ? new StyleLength(size) : new StyleLength(StyleKeyword.Null);
            }
        }

        /// <summary>
        /// The encoded code, or null when the view is empty.
        /// </summary>
        public QrCode Code => code;

        /// <summary>
        /// The view's texture, one pixel per module including the quiet zone. Null until attached.
        /// </summary>
        public Texture2D Texture => texture;

        /// <summary>
        /// Modules per side including the quiet zone, or 0 when empty.
        /// </summary>
        public int ModulesPerSide => code == null ? 0 : code.Size + quietZone * 2;

        public Color DarkColor => darkColor;

        public Color LightColor => lightColor;

        public QRCodeView()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            RegisterCallback<AttachToPanelEvent>(_ => Redraw());
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                style.backgroundImage = StyleKeyword.Null;
                ReleaseTexture();
            });

            Encode();
        }

        public QRCodeView(string text, float size = 0f)
            : this()
        {
            Text = text;
            Size = size;
        }

        /// <summary>
        /// Draws the code into a new readable texture the caller owns, for sharing or saving with
        /// <c>ImageConversion.EncodeToPNG</c>. Returns null when the view is empty.
        /// </summary>
        /// <param name="pixelsPerModule">The scale; 8 gives about 300 px for a short link.</param>
        public Texture2D CreateTexture(int pixelsPerModule = 8)
        {
            return code == null ? null : Draw(code, quietZone, Mathf.Max(1, pixelsPerModule), darkColor, lightColor);
        }

        private void Encode()
        {
            code = null;
            if (text.Length > 0)
            {
                try
                {
                    code = QrEncoder.Encode(text, errorCorrection);
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning($"QRCodeView: {e.Message}");
                }
            }

            EnableInClassList(EmptyClass, code == null);
            Redraw();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var dark = evt.customStyle.TryGetValue(DarkProperty, out var resolvedDark) ? resolvedDark : Color.black;
            var light = evt.customStyle.TryGetValue(LightProperty, out var resolvedLight) ? resolvedLight : Color.white;
            if (texture != null && dark == darkColor && light == lightColor)
            {
                return;
            }

            darkColor = dark;
            lightColor = light;
            Redraw();
        }

        private void Redraw()
        {
            ReleaseTexture();
            if (code == null || panel == null)
            {
                style.backgroundImage = StyleKeyword.Null;
                return;
            }

            texture = Draw(code, quietZone, 1, darkColor, lightColor);
            texture.hideFlags = HideFlags.HideAndDontSave;
            style.backgroundImage = new StyleBackground(texture);
        }

        private static Texture2D Draw(QrCode qr, int quiet, int pixelsPerModule, Color dark, Color light)
        {
            var modules = qr.Size + quiet * 2;
            var pixels = modules * pixelsPerModule;
            var result = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var darkPixel = (Color32)dark;
            var lightPixel = (Color32)light;
            var colours = new Color32[pixels * pixels];
            for (var py = 0; py < pixels; py++)
            {
                // Texture rows run bottom to top; module rows run top to bottom.
                var moduleY = (pixels - 1 - py) / pixelsPerModule - quiet;
                for (var px = 0; px < pixels; px++)
                {
                    var moduleX = px / pixelsPerModule - quiet;
                    var isDark = moduleX >= 0 && moduleY >= 0 && moduleX < qr.Size && moduleY < qr.Size && qr[moduleX, moduleY];
                    colours[py * pixels + px] = isDark ? darkPixel : lightPixel;
                }
            }

            result.SetPixels32(colours);
            result.Apply(false, false);
            return result;
        }

        private void ReleaseTexture()
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
    }
}
