/// Credit SimonDarksideJ

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    public enum SkeletonShape
    {
        /// <summary>
        /// A square thumbnail with two text lines, for grids.
        /// </summary>
        Tile,

        /// <summary>
        /// A round avatar with two text lines, for lists.
        /// </summary>
        Row,

        /// <summary>
        /// Text lines only.
        /// </summary>
        Text,

        /// <summary>
        /// A circle, for avatars and rings.
        /// </summary>
        Circle,
    }

    /// <summary>
    /// Loading placeholders in the shape of the content to come. They pulse gently, and hold still when
    /// an ancestor carries <c>reduce-motion</c>.
    /// </summary>
    /// <remarks>
    /// <para>The class <c>skeletonTile--dim</c> marks the low point of the pulse.</para>
    /// </remarks>
    [UxmlElement]
    public partial class SkeletonTile : VisualElement
    {
        public const string RootClass = "skeletonTile";
        public const string ItemClass = "skeletonTile__item";
        public const string ThumbClass = "skeletonTile__thumb";
        public const string AvatarClass = "skeletonTile__avatar";
        public const string LinesClass = "skeletonTile__lines";
        public const string LineClass = "skeletonTile__line";
        public const string ShortLineClass = "skeletonTile__line--short";
        public const string DimClass = "skeletonTile--dim";

        /// <summary>
        /// Time between pulse phases in milliseconds.
        /// </summary>
        public const int PulseIntervalMs = 700;

        private int count = 1;
        private SkeletonShape shape = SkeletonShape.Tile;
        private int columns = 2;
        private int lines = 2;
        private bool animate = true;
        private float thumbnailAspect = 1f;
        private DisplayedTimer pulse;

        [UxmlAttribute("count")]
        public int Count
        {
            get => count;
            set
            {
                count = Mathf.Max(0, value);
                Rebuild();
            }
        }

        [UxmlAttribute("shape")]
        public SkeletonShape Shape
        {
            get => shape;
            set
            {
                shape = value;
                Rebuild();
            }
        }

        /// <summary>
        /// Items per row for the tile and circle shapes.
        /// </summary>
        [UxmlAttribute("columns")]
        public int Columns
        {
            get => columns;
            set
            {
                columns = Mathf.Max(1, value);
                Rebuild();
            }
        }

        /// <summary>
        /// Text lines per item for the tile, row and text shapes.
        /// </summary>
        [UxmlAttribute("lines")]
        public int Lines
        {
            get => lines;
            set
            {
                lines = Mathf.Max(1, value);
                Rebuild();
            }
        }

        /// <summary>
        /// Width over height of the tile shape's thumbnail; 1 is square, 1.5 is 3:2.
        /// </summary>
        [UxmlAttribute("thumbnail-aspect")]
        public float ThumbnailAspect
        {
            get => thumbnailAspect;
            set
            {
                thumbnailAspect = value > 0.01f ? value : 1f;
                this.Query(className: ThumbClass).ForEach(thumb => thumb.style.height = thumb.layout.width / thumbnailAspect);
            }
        }

        [UxmlAttribute("animate")]
        public bool Animate
        {
            get => animate;
            set
            {
                animate = value;
                if (!animate)
                {
                    StopPulse();
                }
                else if (panel != null && pulse == null)
                {
                    StartPulse();
                }
            }
        }

        public bool IsPulsing => animate && panel != null && !this.IsReduceMotion();

        public SkeletonTile()
        {
            this.ApplyControlStyles();
            this.UseThemeTokens();
            AddToClassList(RootClass);
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(_ => { if (animate) StartPulse(); });
            RegisterCallback<DetachFromPanelEvent>(_ => StopPulse());
            Rebuild();
        }

        public SkeletonTile(SkeletonShape shape, int count = 1)
            : this()
        {
            this.shape = shape;
            this.count = Mathf.Max(0, count);
            Rebuild();
        }

        private static readonly string[] ShapeClasses =
        {
            RootClass + "--tile",
            RootClass + "--row",
            RootClass + "--text",
            RootClass + "--circle",
        };

        private void Rebuild()
        {
            Clear();
            for (var i = 0; i < ShapeClasses.Length; i++)
            {
                EnableInClassList(ShapeClasses[i], i == (int)shape);
            }

            var perRow = shape is SkeletonShape.Tile or SkeletonShape.Circle ? columns : 1;
            for (var i = 0; i < count; i++)
            {
                var item = UIToolkitExtensions.CreateVisualElement(this, ItemClass);
                item.style.width = Length.Percent(100f / perRow);

                switch (shape)
                {
                    case SkeletonShape.Tile:
                        var thumb = UIToolkitExtensions.CreateVisualElement(item, ThumbClass);
                        thumb.RegisterCallback<GeometryChangedEvent>(evt => thumb.style.height = evt.newRect.width / thumbnailAspect);
                        AddLines(item);
                        break;
                    case SkeletonShape.Row:
                        UIToolkitExtensions.CreateVisualElement(item, AvatarClass);
                        AddLines(item);
                        break;
                    case SkeletonShape.Text:
                        AddLines(item);
                        break;
                    case SkeletonShape.Circle:
                        UIToolkitExtensions.CreateVisualElement(item, AvatarClass);
                        break;
                }
            }
        }

        private void AddLines(VisualElement item)
        {
            var container = UIToolkitExtensions.CreateVisualElement(item, LinesClass);
            for (var i = 0; i < lines; i++)
            {
                var line = UIToolkitExtensions.CreateVisualElement(container, LineClass);
                line.EnableInClassList(ShortLineClass, i == lines - 1 && lines > 1);
            }
        }

        private void StartPulse()
        {
            StopPulse();
            pulse = DisplayedTimer.Start(this, () =>
            {
                if (IsPulsing)
                {
                    ToggleInClassList(DimClass);
                }
                else
                {
                    RemoveFromClassList(DimClass);
                }
            }, PulseIntervalMs, PulseIntervalMs);
        }

        private void StopPulse()
        {
            pulse?.Stop();
            pulse = null;
            RemoveFromClassList(DimClass);
        }
    }
}
