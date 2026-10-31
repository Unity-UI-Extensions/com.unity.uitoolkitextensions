/// Credit SimonDarksideJ

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class GradientFillTests : UIDocumentTestBase
    {
        private static void AssertColor(Color expected, Color actual, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.01f), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.01f), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.01f), message);
        }

        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        [UnityTest]
        public IEnumerator Primary_Horizontal_PaintsPrimaryStops()
        {
            var element = new VisualElement();
            element.style.width = 200;
            element.style.height = 52;
            var fill = new GradientFill();
            element.AddManipulator(fill);
            yield return Mount(element);

            Assert.IsTrue(element.ClassListContains(GradientFill.RootClass));
            Assert.That(fill.Texture, Is.Not.Null);
            Assert.That(fill.Texture.width, Is.EqualTo(256));
            Assert.That(fill.Texture.height, Is.EqualTo(1));
            AssertColor(Hex("#4F6BED"), fill.Texture.GetPixel(0, 0), "left is --grad-brand-start");
            AssertColor(Hex("#3956C5"), fill.Texture.GetPixel(255, 0), "right is --grad-brand-end");
            Assert.That(element.resolvedStyle.backgroundImage.texture, Is.SameAs(fill.Texture));
        }

        [UnityTest]
        public IEnumerator Navy_Vertical_PaintsNavyStopsTopToBottom()
        {
            var element = new VisualElement();
            element.style.flexGrow = 1;
            var fill = new GradientFill(GradientPreset.Navy, GradientDirection.Vertical);
            element.AddManipulator(fill);
            yield return Mount(element);

            Assert.IsTrue(element.ClassListContains(GradientFill.NavyClass));
            Assert.That(fill.Texture.width, Is.EqualTo(1));
            AssertColor(Hex("#202024"), fill.Texture.GetPixel(0, 255), "top is --c-navy");
            AssertColor(Hex("#34343A"), fill.Texture.GetPixel(0, 0), "bottom is --c-navy-2");
        }

        [UnityTest]
        public IEnumerator Detach_ReleasesTexture_AndReattachRebuilds()
        {
            var element = new VisualElement();
            var fill = new GradientFill();
            element.AddManipulator(fill);
            yield return Mount(element);
            var first = fill.Texture;
            Assert.That(first, Is.Not.Null);

            element.RemoveFromHierarchy();
            yield return WaitFrames(1);
            Assert.That(fill.Texture, Is.Null);
            Assert.IsTrue(first == null, "the texture was destroyed");

            yield return Mount(element);
            Assert.That(fill.Texture, Is.Not.Null);

            element.RemoveManipulator(fill);
            Assert.IsFalse(element.ClassListContains(GradientFill.RootClass));
            Assert.That(fill.Texture, Is.Null);
        }

        [UnityTest]
        public IEnumerator PresetSwitch_RepaintsFromNewTokens()
        {
            var element = new VisualElement();
            var fill = new GradientFill();
            element.AddManipulator(fill);
            yield return Mount(element);

            fill.Preset = GradientPreset.Navy;
            yield return WaitFrames(2);
            AssertColor(Hex("#202024"), fill.StartColor, "start follows the preset");
        }
    }
}
