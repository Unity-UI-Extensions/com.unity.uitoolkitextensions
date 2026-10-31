/// Credit SimonDarksideJ

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    /// <summary>
    /// Base fixture for component PlayMode tests. Creates a <see cref="UIDocument"/> on a
    /// 390 x 844 panel themed with the package token sheet and control styles, and destroys
    /// it after each test.
    /// </summary>
    public abstract class UIDocumentTestBase
    {
        public const string TestThemePath = "Packages/com.unity.uitoolkitextensions/Tests/Runtime/TestTheme.tss";

        protected GameObject DocumentObject { get; private set; }
        protected UIDocument Document { get; private set; }
        protected VisualElement Root => Document.rootVisualElement;

        private PanelSettings panelSettings;

        [UnitySetUp]
        public IEnumerator CreateDocument()
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.referenceResolution = new Vector2Int(390, 844);
#if UNITY_EDITOR
            panelSettings.themeStyleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(TestThemePath);
#endif
            DocumentObject = new GameObject(GetType().Name);
            Document = DocumentObject.AddComponent<UIDocument>();
            Document.panelSettings = panelSettings;
            yield return null;

            Assert.IsNotNull(Root, "UIDocument did not create a root visual element.");
            Root.style.width = 390;
            Root.style.height = 844;
        }

        [UnityTearDown]
        public IEnumerator DestroyDocument()
        {
            if (DocumentObject != null)
            {
                Object.Destroy(DocumentObject);
            }

            if (panelSettings != null)
            {
                Object.Destroy(panelSettings);
            }

            yield return null;
        }

        /// <summary>
        /// Adds an element to the document root and waits for layout and styles to resolve.
        /// </summary>
        protected IEnumerator Mount(VisualElement element, int frames = 2)
        {
            Root.Add(element);
            yield return WaitFrames(frames);
        }

        protected static IEnumerator WaitFrames(int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Waits real time, for transitions and scheduled items.
        /// </summary>
        protected static IEnumerator WaitMs(int milliseconds)
        {
            var end = Time.realtimeSinceStartup + (milliseconds / 1000f);
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Sends a click to an element the way a pointer tap would arrive. A <see cref="Button"/> listens
        /// through its Clickable manipulator rather than ClickEvent, so it is pressed with a submit instead.
        /// </summary>
        protected static void Click(VisualElement element)
        {
            if (element is Button)
            {
                using var submit = NavigationSubmitEvent.GetPooled();
                submit.target = element;
                element.SendEvent(submit);
                return;
            }

            using var evt = ClickEvent.GetPooled();
            evt.target = element;
            element.SendEvent(evt);
        }

        protected static void Navigate(VisualElement element, NavigationMoveEvent.Direction direction)
        {
            using var evt = NavigationMoveEvent.GetPooled(direction);
            evt.target = element;
            element.SendEvent(evt);
        }

        protected static void PressKey(VisualElement element, KeyCode key)
        {
            using var evt = KeyDownEvent.GetPooled('\0', key, EventModifiers.None);
            evt.target = element;
            element.SendEvent(evt);
        }

        /// <summary>
        /// Sends a left-button pointer down, move or up at a panel position, as a mouse would.
        /// </summary>
        protected static void SendPointerEvent<T>(VisualElement target, Vector2 position) where T : PointerEventBase<T>, new()
        {
            var type = typeof(T) == typeof(PointerDownEvent) ? EventType.MouseDown
                : typeof(T) == typeof(PointerUpEvent) ? EventType.MouseUp
                : EventType.MouseDrag;
            var imgui = new Event { type = type, mousePosition = position, button = 0, clickCount = 1 };
            using var evt = PointerEventBase<T>.GetPooled(imgui);
            evt.target = target;
            target.SendEvent(evt);
        }
    }
}
