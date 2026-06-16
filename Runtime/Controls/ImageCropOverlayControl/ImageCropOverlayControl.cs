/// Credit SimonDarksideJ

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A full-screen modal image cropper that supports drag, pinch, and wheel zoom,
    /// then exports the visible square selection to a new texture.
    /// </summary>
    public class ImageCropOverlayControl : VisualElement
    {
        private const float MinPinchDistancePx = 12f;

        public const string RootClass = "imageCropOverlay";
        public const string PanelClass = "imageCropOverlay__panel";
        public const string HeaderClass = "imageCropOverlay__header";
        public const string TitleClass = "imageCropOverlay__title";
        public const string ViewportHostClass = "imageCropOverlay__viewportHost";
        public const string ViewportClass = "imageCropOverlay__viewport";
        public const string ImageClass = "imageCropOverlay__image";
        public const string FooterClass = "imageCropOverlay__footer";
        public const string ButtonSlotClass = "imageCropOverlay__buttonSlot";
        public const string ButtonClass = "imageCropOverlay__button";
        public const string ButtonCancelClass = "imageCropOverlay__button--cancel";
        public const string ButtonSaveClass = "imageCropOverlay__button--save";

        public static readonly Vector4 CircleCornerRadiusPercent = new(0.5f, 0.5f, 0.5f, 0.5f);

        public sealed class Configuration
        {
            public string Title { get; set; } = "Move and Scale";
            public string CancelLabel { get; set; } = "Cancel";
            public string SaveLabel { get; set; } = "Save";
            public int ExportSize { get; set; } = 512;
            public float ScreenMarginPx { get; set; } = 48f;
            public float MaxViewportWidthRatio { get; set; } = 0.9f;
            public float MaxZoom { get; set; } = 4f;
            public Vector4 CornerRadiusPercent { get; set; } = CircleCornerRadiusPercent;
        }

        private readonly Texture2D sourceTexture;
        private readonly Configuration configuration;
        private readonly Action<Texture2D> onConfirmed;
        private readonly Action onCancelled;
        private readonly VisualElement modalPanel;
        private readonly VisualElement header;
        private readonly Label titleLabel;
        private readonly VisualElement viewportHost;
        private readonly VisualElement cropViewport;
        private readonly VisualElement imageElement;
        private readonly VisualElement footer;
        private readonly VisualElement cancelButtonSlot;
        private readonly VisualElement saveButtonSlot;
        private readonly PillButton cancelButton;
        private readonly PillButton saveButton;
        private readonly Dictionary<int, Vector2> activePointers = new();

        private float viewportSizePx;
        private Vector2 baseDisplaySizePx;
        private Vector2 currentOffsetPx;
        private float currentZoom = 1f;
        private int panPointerId = -1;
        private Vector2 panStartPointerPosition;
        private Vector2 panStartOffsetPx;
        private int pinchPointerIdA = -1;
        private int pinchPointerIdB = -1;
        private float pinchStartDistancePx;
        private float pinchStartZoom;
        private Vector2 pinchAnchorNormalized;

        public static ImageCropOverlayControl Show(
            VisualElement anchor,
            Texture2D sourceTexture,
            Configuration configuration,
            Action<Texture2D> onConfirmed,
            Action onCancelled = null)
        {
            if (anchor?.panel?.visualTree == null || sourceTexture == null)
            {
                return null;
            }

            VisualElement root = anchor.panel.visualTree;
            VisualElement existingOverlay = null;
            foreach (VisualElement child in root.Children())
            {
                if (child.ClassListContains(RootClass))
                {
                    existingOverlay = child;
                    break;
                }
            }

            existingOverlay?.RemoveFromHierarchy();

            var overlay = new ImageCropOverlayControl(sourceTexture, configuration, onConfirmed, onCancelled);
            root.Add(overlay);
            overlay.BringToFront();
            return overlay;
        }

        public static Vector4 CreateUniformCornerRadiusPercent(float percent)
        {
            float clampedPercent = Mathf.Clamp(percent, 0f, 0.5f);
            return new Vector4(clampedPercent, clampedPercent, clampedPercent, clampedPercent);
        }

        public static Vector4 ResolveNormalizedCornerRadiusPercent(VisualElement sourceElement, Vector4 fallback)
        {
            if (sourceElement == null)
            {
                return SanitizeCornerRadiusPercent(fallback);
            }

            float width = sourceElement.resolvedStyle.width;
            if (width <= 0f)
            {
                width = sourceElement.layout.width > 0f ? sourceElement.layout.width : sourceElement.contentRect.width;
            }

            float height = sourceElement.resolvedStyle.height;
            if (height <= 0f)
            {
                height = sourceElement.layout.height > 0f ? sourceElement.layout.height : sourceElement.contentRect.height;
            }

            float baseDimension = Mathf.Max(1f, Mathf.Min(width, height));
            if (baseDimension <= 0f)
            {
                return SanitizeCornerRadiusPercent(fallback);
            }

            return SanitizeCornerRadiusPercent(new Vector4(
                sourceElement.resolvedStyle.borderTopLeftRadius / baseDimension,
                sourceElement.resolvedStyle.borderTopRightRadius / baseDimension,
                sourceElement.resolvedStyle.borderBottomRightRadius / baseDimension,
                sourceElement.resolvedStyle.borderBottomLeftRadius / baseDimension));
        }

        private ImageCropOverlayControl(
            Texture2D sourceTexture,
            Configuration configuration,
            Action<Texture2D> onConfirmed,
            Action onCancelled)
        {
            this.sourceTexture = sourceTexture;
            this.configuration = configuration ?? new Configuration();
            this.onConfirmed = onConfirmed;
            this.onCancelled = onCancelled;

            AddToClassList(RootClass);
            pickingMode = PickingMode.Position;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.right = 0f;
            style.bottom = 0f;
            style.paddingLeft = this.configuration.ScreenMarginPx;
            style.paddingRight = this.configuration.ScreenMarginPx;
            style.paddingTop = this.configuration.ScreenMarginPx;
            style.paddingBottom = this.configuration.ScreenMarginPx;
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            modalPanel = UIToolkitExtensions.CreateVisualElement(this, PanelClass);
            modalPanel.RegisterCallback<GeometryChangedEvent>(OnLayoutGeometryChanged);
            modalPanel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            header = UIToolkitExtensions.CreateVisualElement(modalPanel, HeaderClass);
            header.RegisterCallback<GeometryChangedEvent>(OnLayoutGeometryChanged);

            titleLabel = UIToolkitExtensions.CreateVisualElement<Label>(header, TitleClass);
            titleLabel.text = string.IsNullOrWhiteSpace(this.configuration.Title) ? "Move and Scale" : this.configuration.Title;

            viewportHost = UIToolkitExtensions.CreateVisualElement(modalPanel, ViewportHostClass);

            cropViewport = UIToolkitExtensions.CreateVisualElement(viewportHost, ViewportClass);
            cropViewport.pickingMode = PickingMode.Position;
            cropViewport.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            cropViewport.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            cropViewport.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            cropViewport.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            cropViewport.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            cropViewport.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);

            imageElement = UIToolkitExtensions.CreateVisualElement(cropViewport, ImageClass);
            imageElement.style.backgroundImage = new StyleBackground(sourceTexture);

            footer = UIToolkitExtensions.CreateVisualElement(modalPanel, FooterClass);
            footer.RegisterCallback<GeometryChangedEvent>(OnLayoutGeometryChanged);

            cancelButtonSlot = UIToolkitExtensions.CreateVisualElement(footer, ButtonSlotClass);
            cancelButton = UIToolkitExtensions.CreateVisualElement<PillButton>(cancelButtonSlot, ButtonClass, ButtonCancelClass);
            cancelButton.Text = this.configuration.CancelLabel ?? "Cancel";
            cancelButton.SetInnerColor("#485164");
            cancelButton.SetOuterColor("#5C667A");
            cancelButton.SetTextColor(Color.white);
            cancelButton.Clicked += OnCancelButtonClicked;

            saveButtonSlot = UIToolkitExtensions.CreateVisualElement(footer, ButtonSlotClass);
            saveButton = UIToolkitExtensions.CreateVisualElement<PillButton>(saveButtonSlot, ButtonClass, ButtonSaveClass);
            saveButton.Text = this.configuration.SaveLabel ?? "Save";
            saveButton.SetInnerColor("#4A90E2");
            saveButton.SetOuterColor("#7B68EE");
            saveButton.SetTextColor(Color.white);
            saveButton.Clicked += OnSaveButtonClicked;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var pointerIds = new List<int>(activePointers.Keys);
            foreach (int pointerId in pointerIds)
            {
                if (cropViewport.HasPointerCapture(pointerId))
                {
                    cropViewport.ReleasePointer(pointerId);
                }
            }

            activePointers.Clear();
            panPointerId = -1;
            pinchPointerIdA = -1;
            pinchPointerIdB = -1;
        }

        private void OnLayoutGeometryChanged(GeometryChangedEvent evt)
        {
            float availableWidth = Mathf.Max(0f, modalPanel.contentRect.width);
            float headerHeight = header.layout.height > 0f ? header.layout.height : header.resolvedStyle.height;
            float footerHeight = footer.layout.height > 0f ? footer.layout.height : footer.resolvedStyle.height;
            float availableHeight = Mathf.Max(0f, modalPanel.contentRect.height - headerHeight - footerHeight);
            if (availableWidth <= 0f || availableHeight <= 0f || sourceTexture == null)
            {
                return;
            }

            float fullOverlayWidth = worldBound.width > 0f ? worldBound.width : availableWidth;
            float maxWidthByRatio = Mathf.Min(availableWidth, fullOverlayWidth * configuration.MaxViewportWidthRatio);
            float targetViewportSize = Mathf.Min(maxWidthByRatio, availableHeight);
            if (targetViewportSize <= 0f)
            {
                return;
            }

            viewportSizePx = targetViewportSize;
            cropViewport.style.width = viewportSizePx;
            cropViewport.style.height = viewportSizePx;

            float scaleToCover = Mathf.Max(
                viewportSizePx / Mathf.Max(1f, sourceTexture.width),
                viewportSizePx / Mathf.Max(1f, sourceTexture.height));
            baseDisplaySizePx = new Vector2(sourceTexture.width * scaleToCover, sourceTexture.height * scaleToCover);
            currentOffsetPx = ClampOffset(currentOffsetPx, currentZoom);
            ApplyViewportMask();
            ApplyImageTransform(currentZoom, currentOffsetPx);
        }

        private void ApplyViewportMask()
        {
            if (viewportSizePx <= 0f)
            {
                return;
            }

            Vector4 normalizedRadius = SanitizeCornerRadiusPercent(configuration.CornerRadiusPercent);
            cropViewport.style.borderTopLeftRadius = viewportSizePx * normalizedRadius.x;
            cropViewport.style.borderTopRightRadius = viewportSizePx * normalizedRadius.y;
            cropViewport.style.borderBottomRightRadius = viewportSizePx * normalizedRadius.z;
            cropViewport.style.borderBottomLeftRadius = viewportSizePx * normalizedRadius.w;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!ShouldHandlePointer(evt))
            {
                return;
            }

            activePointers[evt.pointerId] = ToVector2(evt.localPosition);
            cropViewport.CapturePointer(evt.pointerId);

            if (activePointers.Count == 1)
            {
                StartPan(evt.pointerId);
            }
            else if (activePointers.Count == 2)
            {
                StartPinchGesture();
            }

            evt.StopImmediatePropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!activePointers.ContainsKey(evt.pointerId) || viewportSizePx <= 0f)
            {
                return;
            }

            Vector2 localPosition = ToVector2(evt.localPosition);
            activePointers[evt.pointerId] = localPosition;

            if (HasActivePinch())
            {
                UpdatePinchGesture();
                evt.StopImmediatePropagation();
                return;
            }

            if (activePointers.Count == 1 && evt.pointerId == panPointerId)
            {
                Vector2 delta = localPosition - panStartPointerPosition;
                ApplyImageTransform(currentZoom, panStartOffsetPx + delta);
                evt.StopImmediatePropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            CompletePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            CompletePointer(evt.pointerId);
            evt.StopImmediatePropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            CompletePointer(evt.pointerId);
        }

        private void CompletePointer(int pointerId)
        {
            if (!activePointers.Remove(pointerId))
            {
                return;
            }

            if (cropViewport.HasPointerCapture(pointerId))
            {
                cropViewport.ReleasePointer(pointerId);
            }

            if (pointerId == pinchPointerIdA || pointerId == pinchPointerIdB)
            {
                pinchPointerIdA = -1;
                pinchPointerIdB = -1;
            }

            if (activePointers.Count >= 2)
            {
                StartPinchGesture();
            }
            else if (activePointers.Count == 1)
            {
                foreach (KeyValuePair<int, Vector2> activePointer in activePointers)
                {
                    StartPan(activePointer.Key);
                    break;
                }
            }
            else
            {
                panPointerId = -1;
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            if (viewportSizePx <= 0f)
            {
                return;
            }

            float step = evt.delta.y > 0f ? 0.9f : 1.1f;
            float newZoom = Mathf.Clamp(currentZoom * step, 1f, Mathf.Max(1f, configuration.MaxZoom));
            SetZoomAroundAnchor(newZoom, ToVector2(evt.localMousePosition));
            evt.StopImmediatePropagation();
        }

        private bool ShouldHandlePointer(IPointerEvent evt)
        {
            if (sourceTexture == null)
            {
                return false;
            }

            if (evt.pointerType == PointerType.mouse && evt.button != 0)
            {
                return false;
            }

            return activePointers.Count < 2 || activePointers.ContainsKey(evt.pointerId);
        }

        private void StartPan(int pointerId)
        {
            if (!activePointers.TryGetValue(pointerId, out Vector2 startPosition))
            {
                return;
            }

            panPointerId = pointerId;
            panStartPointerPosition = startPosition;
            panStartOffsetPx = currentOffsetPx;
        }

        private void StartPinchGesture()
        {
            if (activePointers.Count < 2)
            {
                return;
            }

            using var enumerator = activePointers.GetEnumerator();
            enumerator.MoveNext();
            KeyValuePair<int, Vector2> firstPointer = enumerator.Current;
            enumerator.MoveNext();
            KeyValuePair<int, Vector2> secondPointer = enumerator.Current;

            pinchPointerIdA = firstPointer.Key;
            pinchPointerIdB = secondPointer.Key;
            panPointerId = -1;
            Vector2 pinchStartCenterPx = (firstPointer.Value + secondPointer.Value) * 0.5f;
            pinchStartDistancePx = Vector2.Distance(firstPointer.Value, secondPointer.Value);
            pinchStartDistancePx = Mathf.Max(pinchStartDistancePx, MinPinchDistancePx);
            pinchStartZoom = currentZoom;
            pinchAnchorNormalized = GetNormalizedPointWithinImage(pinchStartCenterPx, currentZoom, currentOffsetPx);
        }

        private bool HasActivePinch()
        {
            return pinchPointerIdA != -1
                && pinchPointerIdB != -1
                && activePointers.ContainsKey(pinchPointerIdA)
                && activePointers.ContainsKey(pinchPointerIdB);
        }

        private void UpdatePinchGesture()
        {
            if (!HasActivePinch())
            {
                return;
            }

            Vector2 pointerA = activePointers[pinchPointerIdA];
            Vector2 pointerB = activePointers[pinchPointerIdB];
            float currentDistance = Mathf.Max(Vector2.Distance(pointerA, pointerB), MinPinchDistancePx);
            float newZoom = Mathf.Clamp(
                pinchStartZoom * (currentDistance / pinchStartDistancePx),
                1f,
                Mathf.Max(1f, configuration.MaxZoom));
            Vector2 currentCenter = (pointerA + pointerB) * 0.5f;
            Vector2 newOffset = ComputeOffsetForAnchor(newZoom, currentCenter, pinchAnchorNormalized);
            ApplyImageTransform(newZoom, newOffset);
        }

        private void SetZoomAroundAnchor(float zoom, Vector2 anchorViewportPosition)
        {
            Vector2 anchorNormalized = GetNormalizedPointWithinImage(anchorViewportPosition, currentZoom, currentOffsetPx);
            Vector2 newOffset = ComputeOffsetForAnchor(zoom, anchorViewportPosition, anchorNormalized);
            ApplyImageTransform(zoom, newOffset);
        }

        private Vector2 ComputeOffsetForAnchor(float zoom, Vector2 anchorViewportPosition, Vector2 normalizedImagePoint)
        {
            Vector2 displaySize = GetDisplaySize(zoom);
            Vector2 newTopLeft = anchorViewportPosition - Vector2.Scale(normalizedImagePoint, displaySize);
            Vector2 viewportCenter = GetViewportCenter();
            Vector2 newOffset = newTopLeft + (displaySize * 0.5f) - viewportCenter;
            return ClampOffset(newOffset, zoom);
        }

        private Vector2 GetNormalizedPointWithinImage(Vector2 viewportPosition, float zoom, Vector2 offset)
        {
            Rect imageRect = GetImageRect(zoom, offset);
            if (imageRect.width <= 0f || imageRect.height <= 0f)
            {
                return new Vector2(0.5f, 0.5f);
            }

            return new Vector2(
                Mathf.Clamp01((viewportPosition.x - imageRect.xMin) / imageRect.width),
                Mathf.Clamp01((viewportPosition.y - imageRect.yMin) / imageRect.height));
        }

        private void ApplyImageTransform(float zoom, Vector2 offset)
        {
            currentZoom = Mathf.Clamp(zoom, 1f, Mathf.Max(1f, configuration.MaxZoom));
            currentOffsetPx = ClampOffset(offset, currentZoom);

            Rect imageRect = GetImageRect(currentZoom, currentOffsetPx);
            imageElement.style.left = imageRect.xMin;
            imageElement.style.top = imageRect.yMin;
            imageElement.style.width = imageRect.width;
            imageElement.style.height = imageRect.height;
        }

        private Rect GetImageRect(float zoom, Vector2 offset)
        {
            Vector2 displaySize = GetDisplaySize(zoom);
            Vector2 center = GetViewportCenter() + offset;
            Vector2 topLeft = center - (displaySize * 0.5f);
            return new Rect(topLeft, displaySize);
        }

        private Vector2 GetDisplaySize(float zoom)
        {
            return baseDisplaySizePx * Mathf.Max(1f, zoom);
        }

        private Vector2 GetViewportCenter()
        {
            return new Vector2(viewportSizePx * 0.5f, viewportSizePx * 0.5f);
        }

        private Vector2 ClampOffset(Vector2 offset, float zoom)
        {
            if (viewportSizePx <= 0f)
            {
                return Vector2.zero;
            }

            Vector2 displaySize = GetDisplaySize(zoom);
            float maxOffsetX = Mathf.Max(0f, (displaySize.x - viewportSizePx) * 0.5f);
            float maxOffsetY = Mathf.Max(0f, (displaySize.y - viewportSizePx) * 0.5f);
            return new Vector2(
                Mathf.Clamp(offset.x, -maxOffsetX, maxOffsetX),
                Mathf.Clamp(offset.y, -maxOffsetY, maxOffsetY));
        }

        private static Vector2 ToVector2(Vector3 value)
        {
            return new Vector2(value.x, value.y);
        }

        private static Vector4 SanitizeCornerRadiusPercent(Vector4 cornerRadiusPercent)
        {
            return new Vector4(
                Mathf.Clamp(cornerRadiusPercent.x, 0f, 0.5f),
                Mathf.Clamp(cornerRadiusPercent.y, 0f, 0.5f),
                Mathf.Clamp(cornerRadiusPercent.z, 0f, 0.5f),
                Mathf.Clamp(cornerRadiusPercent.w, 0f, 0.5f));
        }

        private void OnCancelButtonClicked()
        {
            CloseOverlay();
            onCancelled?.Invoke();
        }

        private void OnSaveButtonClicked()
        {
            try
            {
                Texture2D croppedTexture = CreateCroppedTexture();
                CloseOverlay();
                onConfirmed?.Invoke(croppedTexture);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private Texture2D CreateCroppedTexture()
        {
            if (sourceTexture == null)
            {
                throw new InvalidOperationException("Cannot crop a null texture.");
            }

            int exportSize = Mathf.Max(1, configuration.ExportSize);
            var croppedTexture = new Texture2D(exportSize, exportSize, TextureFormat.RGBA32, false);
            var pixels = new Color[exportSize * exportSize];
            Rect imageRect = GetImageRect(currentZoom, currentOffsetPx);
            Texture2D samplingTexture = sourceTexture;
            bool destroySamplingTexture = false;

            if (!sourceTexture.isReadable)
            {
                samplingTexture = CreateReadableCopy(sourceTexture);
                destroySamplingTexture = samplingTexture != null && samplingTexture != sourceTexture;
            }

            if (samplingTexture == null)
            {
                throw new InvalidOperationException("Could not create a readable source texture for cropping.");
            }

            try
            {
                for (int y = 0; y < exportSize; y++)
                {
                    float viewportY = ((y + 0.5f) / exportSize) * viewportSizePx;
                    int destinationRow = exportSize - 1 - y;
                    for (int x = 0; x < exportSize; x++)
                    {
                        float viewportX = ((x + 0.5f) / exportSize) * viewportSizePx;
                        float u = Mathf.Clamp01((viewportX - imageRect.xMin) / imageRect.width);
                        float normalizedTop = Mathf.Clamp01((viewportY - imageRect.yMin) / imageRect.height);
                        float v = 1f - normalizedTop;
                        pixels[(destinationRow * exportSize) + x] = samplingTexture.GetPixelBilinear(u, v);
                    }
                }
            }
            finally
            {
                if (destroySamplingTexture)
                {
                    UnityEngine.Object.Destroy(samplingTexture);
                }
            }

            croppedTexture.SetPixels(pixels);
            croppedTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return croppedTexture;
        }

        private static Texture2D CreateReadableCopy(Texture2D texture)
        {
            if (texture == null)
            {
                return null;
            }

            RenderTexture temporaryTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previousActiveTexture = RenderTexture.active;

            try
            {
                Graphics.Blit(texture, temporaryTexture);
                RenderTexture.active = temporaryTexture;

                var readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                readableTexture.ReadPixels(new Rect(0f, 0f, temporaryTexture.width, temporaryTexture.height), 0, 0);
                readableTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                return readableTexture;
            }
            finally
            {
                RenderTexture.active = previousActiveTexture;
                RenderTexture.ReleaseTemporary(temporaryTexture);
            }
        }

        private void CloseOverlay()
        {
            RemoveFromHierarchy();
        }
    }
}