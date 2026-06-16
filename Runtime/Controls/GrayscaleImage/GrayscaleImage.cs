/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A UIToolkit image element that renders a sprite or texture via ImmediateMode,
    /// with support for a custom material and a configurable greyscale shader toggle.
    /// </summary>
    public class GrayscaleImage : ImmediateModeElement
    {
        public const string DefaultMainTextureProperty = "_MainTex";
        public const string DefaultGreyscaleToggleProperty = "_GreyscaleEnabled";

        private Material material;
        private Sprite sprite;
        private Texture texture;
        private bool greyscaleEnabled = true;
        private ScaleMode scaleModeValue = ScaleMode.ScaleToFit;

        private string mainTextureProperty = DefaultMainTextureProperty;
        private string greyscaleToggleProperty = DefaultGreyscaleToggleProperty;

        public GrayscaleImage()
        {
            scaleModeValue = ScaleMode.ScaleToFit;
        }

        public Sprite SpriteProperty
        {
            get => sprite;
            set
            {
                if (sprite == value)
                {
                    return;
                }

                sprite = value;
                if (value != null)
                {
                    texture = null;
                }
                MarkDirtyRepaint();
            }
        }

        public Texture TextureProperty
        {
            get => texture;
            set
            {
                if (texture == value)
                {
                    return;
                }

                texture = value;
                if (value != null)
                {
                    sprite = null;
                }

                ApplyShaderProperties();
                MarkDirtyRepaint();
            }
        }

        public ScaleMode scaleMode
        {
            get => scaleModeValue;
            set
            {
                if (scaleModeValue == value)
                {
                    return;
                }

                scaleModeValue = value;
                MarkDirtyRepaint();
            }
        }

        public Material Material
        {
            get => material;
            set
            {
                if (material == value)
                {
                    return;
                }

                material = value;
                ApplyShaderProperties();
                MarkDirtyRepaint();
            }
        }

        public bool GreyscaleEnabled
        {
            get => greyscaleEnabled;
            set
            {
                if (greyscaleEnabled == value)
                {
                    return;
                }

                greyscaleEnabled = value;
                ApplyShaderProperties();
                MarkDirtyRepaint();
            }
        }

        public string MainTextureProperty
        {
            get => mainTextureProperty;
            set
            {
                if (mainTextureProperty == value)
                {
                    return;
                }

                mainTextureProperty = string.IsNullOrWhiteSpace(value) ? DefaultMainTextureProperty : value;
                ApplyShaderProperties();
                MarkDirtyRepaint();
            }
        }

        public string GreyscaleToggleProperty
        {
            get => greyscaleToggleProperty;
            set
            {
                if (greyscaleToggleProperty == value)
                {
                    return;
                }

                greyscaleToggleProperty = string.IsNullOrWhiteSpace(value) ? DefaultGreyscaleToggleProperty : value;
                ApplyShaderProperties();
                MarkDirtyRepaint();
            }
        }

        private void ApplyShaderProperties()
        {
            if (material == null)
            {
                return;
            }

            var tex = texture != null ? texture : (sprite != null ? sprite.texture : null);
            if (tex != null)
            {
                material.SetTexture(mainTextureProperty, tex);
            }

            material.SetFloat(greyscaleToggleProperty, greyscaleEnabled ? 1f : 0f);
        }

        protected override void ImmediateRepaint()
        {
            if (sprite == null && texture == null)
            {
                return;
            }

            var activeTexture = texture != null ? texture : sprite.texture;
            if (activeTexture == null)
            {
                return;
            }

            var w = contentRect.width;
            var h = contentRect.height;
            if (w <= 0f || h <= 0f)
            {
                return;
            }

            ApplyShaderProperties();

            var destRect = new Rect(0f, 0f, w, h);

            Rect uvRect;
            float sourceAspect;

            if (sprite != null)
            {
                var tr = sprite.textureRect;
                if (tr.width <= 0f || tr.height <= 0f)
                {
                    return;
                }

                uvRect = new Rect(
                    tr.xMin / activeTexture.width,
                    tr.yMin / activeTexture.height,
                    tr.width / activeTexture.width,
                    tr.height / activeTexture.height);

                sourceAspect = tr.width / tr.height;
            }
            else
            {
                uvRect = new Rect(0f, 0f, 1f, 1f);
                sourceAspect = activeTexture.width / (float)activeTexture.height;
            }

            var elementAspect = destRect.width / destRect.height;

            if (scaleMode == ScaleMode.ScaleAndCrop)
            {
                if (sourceAspect > elementAspect)
                {
                    var scale = elementAspect / sourceAspect;
                    var newW = uvRect.width * scale;
                    uvRect.x += (uvRect.width - newW) * 0.5f;
                    uvRect.width = newW;
                }
                else
                {
                    var scale = sourceAspect / elementAspect;
                    var newH = uvRect.height * scale;
                    uvRect.y += (uvRect.height - newH) * 0.5f;
                    uvRect.height = newH;
                }
            }
            else if (scaleMode == ScaleMode.ScaleToFit)
            {
                if (sourceAspect > elementAspect)
                {
                    var drawH = destRect.width / sourceAspect;
                    destRect.y += (destRect.height - drawH) * 0.5f;
                    destRect.height = drawH;
                }
                else
                {
                    var drawW = destRect.height * sourceAspect;
                    destRect.x += (destRect.width - drawW) * 0.5f;
                    destRect.width = drawW;
                }
            }

            Graphics.DrawTexture(destRect, activeTexture, uvRect, 0, 0, 0, 0, Color.white, material);
        }
    }
}
