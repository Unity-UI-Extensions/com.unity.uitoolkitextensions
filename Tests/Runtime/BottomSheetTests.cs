/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class BottomSheetTests : UIDocumentTestBase
    {
        private BottomSheet sheet;
        private readonly List<SheetDetent> changes = new();

        private IEnumerator MountSheet(bool reduceMotion = true)
        {
            // Reduce motion makes the translate transition instant, so positions can be read next frame.
            Root.SetReduceMotion(reduceMotion);
            sheet = new BottomSheet { FullTopOffset = 44f, PeekHeight = 100f };
            sheet.Add(new Label("Sheet content"));
            sheet.DetentChanged += changes.Add;
            changes.Clear();
            yield return Mount(sheet, 3);
        }

        private float VisibleHeight() => sheet.worldBound.yMax - sheet.Panel.worldBound.yMin;

        private static void SendPointer<T>(VisualElement target, Vector2 position) where T : PointerEventBase<T>, new()
        {
            var type = typeof(T) == typeof(PointerDownEvent) ? EventType.MouseDown
                : typeof(T) == typeof(PointerUpEvent) ? EventType.MouseUp
                : EventType.MouseDrag;
            var imgui = new Event { type = type, mousePosition = position, button = 0, clickCount = 1 };
            using var evt = PointerEventBase<T>.GetPooled(imgui);
            evt.target = target;
            target.SendEvent(evt);
        }

        [UnityTest]
        public IEnumerator StartsHidden_AndSnapsToDetents()
        {
            yield return MountSheet();

            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Hidden));
            Assert.IsTrue(sheet.ClassListContains(BottomSheet.HiddenClass));
            Assert.That(sheet.pickingMode, Is.EqualTo(PickingMode.Ignore), "the overlay layer passes taps through");

            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(2);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Half));
            Assert.IsTrue(sheet.ClassListContains(BottomSheet.HalfClass));
            Assert.IsFalse(sheet.ClassListContains(BottomSheet.HiddenClass));
            Assert.That(VisibleHeight(), Is.EqualTo(sheet.layout.height * 0.5f).Within(1f));

            sheet.SnapTo(SheetDetent.Peek);
            yield return WaitFrames(2);
            Assert.That(VisibleHeight(), Is.EqualTo(100f).Within(1f));

            sheet.SnapTo(SheetDetent.Full);
            yield return WaitFrames(2);
            Assert.That(VisibleHeight(), Is.EqualTo(sheet.layout.height - 44f).Within(1f));

            Assert.That(changes, Is.EqualTo(new[] { SheetDetent.Half, SheetDetent.Peek, SheetDetent.Full }));
        }

        [UnityTest]
        public IEnumerator DetentOutsideList_SnapsToNearestAllowed()
        {
            yield return MountSheet();
            sheet.Detents = new List<SheetDetent> { SheetDetent.Peek, SheetDetent.Full };

            sheet.SnapTo(SheetDetent.Half);
            Assert.That(sheet.Current, Is.Not.EqualTo(SheetDetent.Half));
            Assert.That(sheet.Detents, Is.EqualTo(new[] { SheetDetent.Peek, SheetDetent.Full }));
        }

        [UnityTest]
        public IEnumerator HandleBack_FullToHalf_ThenCloses()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Full);

            Assert.IsTrue(sheet.HandleBack());
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Half));
            Assert.IsTrue(sheet.HandleBack());
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Hidden));
            Assert.IsFalse(sheet.HandleBack(), "a hidden sheet leaves back to the caller");
        }

        [UnityTest]
        public IEnumerator PointerDrag_SettlesOnNearestDetent()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(2);

            var handle = sheet.Q(className: BottomSheet.HandleClass);
            var start = handle.worldBound.center;

            // Drag up slowly to just short of full height, over several frames.
            SendPointer<PointerDownEvent>(handle, start);
            var target = start.y - (sheet.VisibleHeightOf(SheetDetent.Full) - sheet.VisibleHeightOf(SheetDetent.Half)) + 30f;
            for (var i = 1; i <= 20; i++)
            {
                yield return WaitMs(40);
                SendPointer<PointerMoveEvent>(sheet.Panel, new Vector2(start.x, Mathf.Lerp(start.y, target, i / 20f)));
            }

            Assert.IsTrue(sheet.ClassListContains(BottomSheet.DraggingClass), "dragging turns the transition off");
            yield return WaitMs(150);
            SendPointer<PointerUpEvent>(sheet.Panel, new Vector2(start.x, target));
            yield return WaitFrames(2);

            Assert.IsFalse(sheet.ClassListContains(BottomSheet.DraggingClass));
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Full));
            Assert.That(changes, Is.EqualTo(new[] { SheetDetent.Half, SheetDetent.Full }));
        }

        [UnityTest]
        public IEnumerator HeaderControls_KeepTheirPointers_AndCategoriesCanBeReselected()
        {
            yield return MountSheet();
            var tabs = new ScrollingTabs { Variant = ScrollingTabsVariant.Chips };
            tabs.SetItems(new[] { new TabItem("first", "First"), new TabItem("second", "Second") });
            var selected = new List<string>();
            tabs.SelectionChanged += (id, _) => selected.Add(id);
            sheet.Header.Add(tabs);
            var input = new TextField();
            sheet.Header.Add(input);
            var button = new Button { text = "Close" };
            sheet.Header.Add(button);
            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(3);

            foreach (var index in new[] { 1, 0, 1, 0 })
            {
                var tab = tabs.TabElement(index);
                var position = tab.worldBound.center;
                SendPointer<PointerDownEvent>(tab, position);
                Assert.IsFalse(sheet.Panel.HasPointerCapture(PointerId.mousePointerId), "the sheet must not steal a chip tap");
                SendPointer<PointerUpEvent>(tab, position);
                Click(tab);
                Assert.That(tabs.SelectedIndex, Is.EqualTo(index));
            }
            Assert.That(selected, Is.EqualTo(new[] { "second", "first", "second", "first" }));

            foreach (var control in new VisualElement[] { input.Q(TextField.textInputUssName), button, tabs.ScrollView.contentContainer })
            {
                Assert.That(control, Is.Not.Null);
                var position = control.worldBound.center;
                SendPointer<PointerDownEvent>(control, position);
                SendPointer<PointerMoveEvent>(sheet.Panel, position + new Vector2(0f, 60f));
                Assert.IsFalse(sheet.Panel.HasPointerCapture(PointerId.mousePointerId));
                Assert.IsFalse(sheet.ClassListContains(BottomSheet.DraggingClass));
                SendPointer<PointerUpEvent>(control, position);
            }
        }

        [UnityTest]
        public IEnumerator HeaderSearchClear_KeepsPointer_AndClearsQuery()
        {
            yield return MountSheet();
            var search = new SearchField { DebounceMs = 0 };
            sheet.Header.Add(search);
            var queries = new List<string>();
            search.QueryChanged += queries.Add;
            search.TextField.value = "turbine";
            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(3);

            var clear = search.Q(className: SearchField.ClearClass);
            var position = clear.worldBound.center;
            SendPointer<PointerDownEvent>(clear, position);
            Assert.IsFalse(sheet.Panel.HasPointerCapture(PointerId.mousePointerId));
            SendPointer<PointerUpEvent>(clear, position);
            Assert.That(search.Value, Is.Empty);
            Assert.That(queries, Is.EqualTo(new[] { "turbine", string.Empty }));
        }

        [UnityTest]
        public IEnumerator PlainHeader_DragsOnlyAfterMovementThreshold()
        {
            yield return MountSheet();
            var title = new Label("Title");
            sheet.Header.Add(title);
            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(2);
            var start = title.worldBound.center;

            SendPointer<PointerDownEvent>(title, start);
            Assert.IsFalse(sheet.Panel.HasPointerCapture(PointerId.mousePointerId));
            SendPointer<PointerMoveEvent>(sheet.Panel, start + new Vector2(0f, 60f));
            Assert.IsTrue(sheet.ClassListContains(BottomSheet.DraggingClass));
            Assert.IsTrue(sheet.Panel.HasPointerCapture(PointerId.mousePointerId));
            SendPointer<PointerUpEvent>(sheet.Panel, start + new Vector2(0f, 60f));
            Assert.IsFalse(sheet.Panel.HasPointerCapture(PointerId.mousePointerId));
            Assert.IsFalse(sheet.ClassListContains(BottomSheet.DraggingClass));
        }

        [UnityTest]
        public IEnumerator FastDownwardFling_MovesOneDetentDown()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Full);
            yield return WaitFrames(2);

            // 40 px in 20 ms is 2000 px/s: a fling, though the distance alone is nearer full than half.
            sheet.DragStart(1, 300f, 1.00f);
            sheet.DragMove(320f, 1.01f);
            sheet.DragMove(340f, 1.02f);
            sheet.DragEnd(1.03f);

            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Half));
        }

        [UnityTest]
        public IEnumerator DragBelowLowestDetent_ClosesOnlyWhenDismissible()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Peek);
            yield return WaitFrames(2);

            sheet.Dismissible = false;
            sheet.DragStart(1, 700f, 1f);
            sheet.DragMove(800f, 2f);
            sheet.DragEnd(2f);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Peek));

            sheet.Dismissible = true;
            sheet.DragStart(1, 700f, 3f);
            sheet.DragMove(800f, 4f);
            sheet.DragEnd(4f);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Hidden));
        }

        [UnityTest]
        public IEnumerator ModalScrim_ShowsAtHalf_AndTapCloses()
        {
            yield return MountSheet();
            sheet.Modal = true;
            var scrimClicks = 0;
            sheet.ScrimClicked += () => scrimClicks++;

            sheet.SnapTo(SheetDetent.Peek);
            Assert.IsFalse(sheet.ClassListContains(BottomSheet.ScrimVisibleClass), "no scrim at peek");

            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(2);
            Assert.IsTrue(sheet.ClassListContains(BottomSheet.ScrimVisibleClass));
            var scrim = sheet.Q(className: BottomSheet.ScrimClass);
            Assert.That(scrim.pickingMode, Is.EqualTo(PickingMode.Position));

            Click(scrim);
            Assert.That(scrimClicks, Is.EqualTo(1));
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Hidden));
        }

        [UnityTest]
        public IEnumerator Docked_IsRightColumn_AndIgnoresDrag()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Half);
            sheet.Docked = true;
            yield return WaitFrames(3);

            Assert.IsTrue(sheet.ClassListContains(BottomSheet.DockedClass));
            Assert.That(sheet.Panel.layout.width, Is.EqualTo(360f).Within(1f));
            Assert.That(sheet.Panel.layout.height, Is.EqualTo(sheet.layout.height).Within(1f));
            Assert.That(sheet.Panel.resolvedStyle.translate.y, Is.EqualTo(0f));

            var handle = sheet.Q(className: BottomSheet.HandleClass);
            Assert.That(handle.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            Navigate(handle, NavigationMoveEvent.Direction.Up);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Half));
        }

        [UnityTest]
        public IEnumerator KeyboardOnHandle_StepsDetents()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Peek);
            var handle = sheet.Q(className: BottomSheet.HandleClass);

            Navigate(handle, NavigationMoveEvent.Direction.Up);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Half));
            Navigate(handle, NavigationMoveEvent.Direction.Up);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Full));
            Navigate(handle, NavigationMoveEvent.Direction.Down);
            Assert.That(sheet.Current, Is.EqualTo(SheetDetent.Half));
        }

        [UnityTest]
        public IEnumerator Animated_SnapTransitionsThenSettles()
        {
            yield return MountSheet(reduceMotion: false);
            sheet.SnapTo(SheetDetent.Full);
            yield return WaitFrames(2);

            var midway = sheet.Panel.resolvedStyle.translate.y;
            Assert.That(midway, Is.GreaterThan(1f), "the panel is still moving after two frames");

            yield return WaitMs(ThemeTokens.Motion.BaseMs + 200);
            Assert.That(sheet.Panel.resolvedStyle.translate.y, Is.EqualTo(0f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator Panel_ReadsSurfaceTokens()
        {
            yield return MountSheet();
            sheet.SnapTo(SheetDetent.Half);
            yield return WaitFrames(2);

            Assert.IsTrue(sheet.ClassListContains(ThemeTokens.ThemedClass));
            Assert.That(sheet.Panel.resolvedStyle.borderTopLeftRadius, Is.EqualTo(28f), "--radius-sheet");
            Assert.That(sheet.Panel.resolvedStyle.backgroundColor.a, Is.EqualTo(0.86f).Within(0.01f), "--surface-bg is the deep panel");

            sheet.SetSurface(ThemeSurface.Light);
            yield return WaitFrames(2);
            Assert.That(sheet.Panel.resolvedStyle.backgroundColor.a, Is.EqualTo(1f).Within(0.01f), "light surface is the page ground");
        }
    }
}
