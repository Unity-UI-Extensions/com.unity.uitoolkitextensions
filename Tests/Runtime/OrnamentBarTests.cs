/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class OrnamentBarTests : UIDocumentTestBase
    {
        private static List<OrnamentItem> Items() => new()
        {
            new OrnamentItem("library", "Library"),
            new OrnamentItem("place", "Place"),
            new OrnamentItem("capture", "Capture"),
        };

        [UnityTest]
        public IEnumerator Bottom_BarSizesToItems_AndIsCentred()
        {
            var ornament = new OrnamentBar { Anchor = OrnamentAnchor.Bottom };
            ornament.SetItems(Items(), primary: 1);
            yield return Mount(ornament, 3);

            Assert.IsTrue(ornament.ClassListContains(OrnamentBar.BottomClass));
            Assert.That(ornament.Bar.layout.width, Is.LessThan(ornament.layout.width), "never stretches");
            Assert.That(ornament.Bar.worldBound.center.x, Is.EqualTo(ornament.worldBound.center.x).Within(1f));
            Assert.That(ornament.worldBound.yMax - ornament.Bar.worldBound.yMax, Is.EqualTo(16f).Within(1f), "--space-4 from the edge");
            Assert.That(ornament.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [UnityTest]
        public IEnumerator Primary_HasBrandFill_AndMoves()
        {
            var ornament = new OrnamentBar();
            ornament.SetItems(Items(), primary: 1);
            yield return Mount(ornament, 3);

            var place = ornament.ItemElement(1);
            Assert.IsTrue(place.ClassListContains(OrnamentBar.PrimaryItemClass));
            Assert.IsTrue(place.ClassListContains(GradientFill.RootClass));
            Assert.That(place.resolvedStyle.backgroundImage.texture, Is.Not.Null);

            ornament.PrimaryIndex = 2;
            yield return WaitFrames(2);
            Assert.IsFalse(place.ClassListContains(GradientFill.RootClass));
            Assert.That(place.style.backgroundImage.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.IsTrue(ornament.ItemElement(2).ClassListContains(GradientFill.RootClass));
        }

        [UnityTest]
        public IEnumerator Click_RaisesItemSelected_SkipsDisabled()
        {
            var ornament = new OrnamentBar();
            ornament.SetItems(Items());
            var selected = new List<(int, string)>();
            ornament.ItemSelected += (i, id) => selected.Add((i, id));
            yield return Mount(ornament);

            Click(ornament.ItemElement(2));
            ornament.SetItemEnabled(0, false);
            Click(ornament.ItemElement(0));

            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = ornament.ItemElement(1);
                ornament.ItemElement(1).SendEvent(evt);
            }

            Assert.That(selected, Is.EqualTo(new[] { (2, "capture"), (1, "place") }));
            Assert.IsTrue(ornament.ItemElement(0).ClassListContains(OrnamentBar.DisabledItemClass));
        }

        [UnityTest]
        public IEnumerator PointerClickReleasesFocus_KeyboardSubmitKeepsIt()
        {
            var ornament = new OrnamentBar();
            ornament.SetItems(Items());
            yield return Mount(ornament);
            var item = ornament.ItemElement(0);
            item.Focus();
            Assert.AreSame(item, item.focusController.focusedElement);
            Click(item);
            Assert.AreNotSame(item, item.focusController.focusedElement, "A pointer tap must not leave the focus ring on");

            item.Focus();
            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = item;
                item.SendEvent(evt);
            }
            Assert.AreSame(item, item.focusController.focusedElement, "Keyboard navigation retains its focus indicator");
        }

        [UnityTest]
        public IEnumerator Auto_UsesRightEdgeInLandscape()
        {
            var host = new VisualElement();
            host.style.width = 800;
            host.style.height = 400;
            var ornament = new OrnamentBar { Anchor = OrnamentAnchor.Auto };
            ornament.SetItems(Items());
            host.Add(ornament);
            yield return Mount(host, 3);

            Assert.That(ornament.ResolvedAnchor, Is.EqualTo(OrnamentAnchor.Right));
            Assert.That(ornament.Bar.resolvedStyle.flexDirection, Is.EqualTo(FlexDirection.Column));
            Assert.That(ornament.worldBound.xMax - ornament.Bar.worldBound.xMax, Is.EqualTo(16f).Within(1f));

            host.style.width = 400;
            host.style.height = 800;
            yield return WaitFrames(3);
            Assert.That(ornament.ResolvedAnchor, Is.EqualTo(OrnamentAnchor.Bottom));
        }

        [UnityTest]
        public IEnumerator Uxml_LabelsAndLabelToggle()
        {
            var ornament = new OrnamentBar { Labels = new List<string> { "Map", "Scan" }, ShowLabels = false };
            yield return Mount(ornament);

            Assert.That(ornament.Items.Count, Is.EqualTo(2));
            Assert.That(ornament.Items[1].Id, Is.EqualTo("Scan"));
            var label = ornament.ItemElement(0).Q<Label>(className: OrnamentBar.LabelClass);
            Assert.That(label.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            ornament.ShowLabels = true;
            yield return WaitFrames(2);
            Assert.That(label.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }
    }
}
