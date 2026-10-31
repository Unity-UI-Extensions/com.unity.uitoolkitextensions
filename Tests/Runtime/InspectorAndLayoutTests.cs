/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class InspectorAndLayoutTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator FilterChipRow_ValuesActiveLookAndClear()
        {
            var row = new FilterChipRow { UseMenu = false };
            row.style.width = 350;
            row.SetFilters(new List<FilterDefinition>
            {
                new("type", "Type", new[] { "Video", "Audio", "3D" }),
                new("floor", "Floor", new[] { "Ground", "First" }),
            });
            var changes = new List<string>();
            var tapped = new List<string>();
            row.FilterChanged += (id, index, text) => changes.Add($"{id}={index}:{text}");
            row.FilterTapped += tapped.Add;
            yield return Mount(row);

            var chip = row.ChipElement("type");
            Assert.That(chip.layout.height, Is.EqualTo(38f));
            Assert.That(row.Q<Label>(className: FilterChipRow.ClearClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "nothing to clear");
            Click(chip);
            Assert.That(tapped, Is.EqualTo(new[] { "type" }));

            row.SetValue("type", 0);
            yield return WaitFrames(2);
            Assert.That(chip.Q<Label>().text, Is.EqualTo("Type: Video"));
            Assert.IsTrue(chip.ClassListContains(FilterChipRow.ActiveClass));
            Assert.That(chip.resolvedStyle.backgroundImage.texture, Is.Not.Null, "filled when active");
            var clear = row.Q<Label>(className: FilterChipRow.ClearClass);
            Assert.That(clear.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            Click(clear);
            yield return WaitFrames(2);
            Assert.That(row.GetValue("type"), Is.EqualTo(-1));
            Assert.That(chip.Q<Label>().text, Is.EqualTo("Type"));
            Assert.That(changes, Is.EqualTo(new[] { "type=0:Video", "type=-1:" }));
        }

        [UnityTest]
        public IEnumerator FilterChipRow_MenuPicksAValue()
        {
            var row = new FilterChipRow();
            row.style.width = 350;
            row.SetFilters(new List<FilterDefinition> { new("type", "Type", new[] { "Video", "Audio" }) });
            yield return Mount(row);

            Click(row.ChipElement("type"));
            yield return WaitFrames(2);
            var rows = Root.panel.visualTree.Query(className: DropDownMenuControl.RowClass).ToList();
            Assert.That(rows.Count, Is.EqualTo(3), "All, then the two options");
            Click(rows[2]);
            yield return WaitFrames(2);
            Assert.That(row.GetValue("type"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RadialMenu_LongPressHighlightAndPick()
        {
            Root.SetReduceMotion(true);
            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 600;
            var target = new VisualElement();
            target.style.position = Position.Absolute;
            target.style.left = 0;
            target.style.top = 0;
            target.style.right = 0;
            target.style.bottom = 0;
            host.Add(target);
            var menu = new RadialMenu { HoldThresholdMs = 100 };
            menu.SetItems(new List<RadialMenuItem> { new("food", "Food"), new("music", "Music"), new("crafts", "Crafts"), new("games", "Games") });
            host.Add(menu);
            menu.Attach(target);
            var picked = new List<string>();
            menu.ItemSelected += picked.Add;
            yield return Mount(host);

            var press = host.worldBound.position + new Vector2(195f, 300f);
            SendPointerEvent<PointerDownEvent>(target, press);
            yield return WaitMs(250);
            Assert.IsTrue(menu.IsOpen);
            Assert.That(menu.Origin, Is.EqualTo(new Vector2(195f, 300f)));

            // Item 1 sits at three o'clock: slide right and release.
            SendPointerEvent<PointerMoveEvent>(target, press + new Vector2(80f, 0f));
            Assert.That(menu.HighlightedIndex, Is.EqualTo(1));
            Assert.IsTrue(menu.ItemElement(1).ClassListContains(RadialMenu.HighlightedClass));
            SendPointerEvent<PointerUpEvent>(target, press + new Vector2(80f, 0f));
            Assert.That(picked, Is.EqualTo(new[] { "music" }));
            Assert.IsFalse(menu.IsOpen);

            // Near the edge the ring is pulled inside the layer.
            menu.Open(new Vector2(5f, 5f));
            Assert.That(menu.Origin.x, Is.GreaterThanOrEqualTo(88f + 28f));
            menu.HighlightToward(menu.Origin + new Vector2(2f, 2f));
            Assert.That(menu.HighlightedIndex, Is.EqualTo(-1), "the centre highlights nothing");
            menu.Close();
        }

        [UnityTest]
        public IEnumerator NumericField_StepsClampScrubAndType()
        {
            var field = new NumericField("Height", 1.2f, "m") { Step = 0.1f, Min = 0f, Max = 2f };
            field.style.width = 200;
            var values = new List<float>();
            field.ValueChanged += values.Add;
            yield return Mount(field);

            Click(field.Q<Button>(className: NumericField.PlusClass));
            field.Nudge(10);
            Assert.That(field.Value, Is.EqualTo(2f), "clamped to max");

            var label = field.Q<Label>(className: NumericField.LabelClass);
            var start = label.worldBound.center;
            SendPointerEvent<PointerDownEvent>(label, start);
            SendPointerEvent<PointerMoveEvent>(label, start + new Vector2(-40f, 0f));
            SendPointerEvent<PointerUpEvent>(label, start + new Vector2(-40f, 0f));
            Assert.That(field.Value, Is.EqualTo(1f).Within(0.001f), "40 px left is 10 steps down");

            field.Input.value = 0.55f;
            Assert.That(field.Value, Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(values.Count, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator TransformGroup_PoseAndUniformScale()
        {
            var group = new TransformGroup();
            group.style.width = 350;
            var poses = 0;
            group.PoseChanged += (_, _, _) => poses++;
            yield return Mount(group);

            group.SetPose(new Vector3(1f, 0f, 2f), new Vector3(0f, 90f, 0f), Vector3.one * 1.5f);
            Assert.That(poses, Is.EqualTo(0), "SetPose is silent");
            Assert.That(group.Position, Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(group.Scale, Is.EqualTo(Vector3.one * 1.5f));

            group.RotationField(1).Value = 180f;
            Assert.That(poses, Is.EqualTo(1));
            Assert.That(group.Rotation.y, Is.EqualTo(180f));

            var x = group.PositionField(0);
            var z = group.PositionField(2);
            Assert.That(x.layout.width, Is.EqualTo(z.layout.width).Within(1f), "equal thirds");
            Assert.That(group.UniformScaleField.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            group.UniformScale = false;
            yield return WaitFrames(2);
            Assert.That(group.ScaleField(0).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(group.UniformScaleField.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator SafeAreaRootAndBreakpointRoot()
        {
            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 844;
            var breakpoints = new BreakpointRoot();
            var safe = new SafeAreaRoot { Edges = SafeAreaEdges.Top | SafeAreaEdges.Bottom };
            var child = new VisualElement();
            child.style.flexGrow = 1;
            safe.Add(child);
            breakpoints.Add(safe);
            host.Add(breakpoints);
            var changes = new List<Breakpoint>();
            breakpoints.BreakpointChanged += changes.Add;
            yield return Mount(host);

            safe.OverrideInsets(new SafeInsets(47f, 0f, 34f, 0f));
            yield return WaitFrames(2);
            Assert.That(child.worldBound.y - host.worldBound.y, Is.EqualTo(47f).Within(0.5f));
            Assert.That(host.worldBound.yMax - child.worldBound.yMax, Is.EqualTo(34f).Within(0.5f));
            Assert.That(safe.Insets.Top, Is.EqualTo(47f));

            Assert.That(breakpoints.Current, Is.EqualTo(Breakpoint.Compact));
            Assert.IsTrue(breakpoints.ClassListContains(BreakpointRoot.PortraitClass));
            host.style.width = 1180;
            host.style.height = 820;
            yield return WaitFrames(3);
            Assert.That(breakpoints.Current, Is.EqualTo(Breakpoint.Expanded));
            Assert.IsTrue(breakpoints.ClassListContains(BreakpointRoot.ExpandedClass));
            Assert.IsTrue(breakpoints.IsLandscape);
            host.style.width = 700;
            yield return WaitFrames(3);
            Assert.That(breakpoints.Current, Is.EqualTo(Breakpoint.Medium));
            Assert.That(changes, Is.EqualTo(new[] { Breakpoint.Compact, Breakpoint.Expanded, Breakpoint.Medium }));
        }
    }
}
