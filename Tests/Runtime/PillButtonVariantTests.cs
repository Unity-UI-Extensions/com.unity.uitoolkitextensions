/// Credit SimonDarksideJ

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class PillButtonVariantTests : UIDocumentTestBase
    {
        private static VisualElement Background(PillButton button) => button.Q(className: PillButton.BackgroundClass);
        private static Label Label(PillButton button) => button.Q<Label>(className: PillButton.LabelClass);
        private static VisualElement Fill(PillButton button) => button.Q(className: PillButton.FillClass);

        private IEnumerator MountButton(PillButton button)
        {
            button.Text = "Continue";
            button.style.width = 300;
            yield return Mount(button);
        }

        [UnityTest]
        public IEnumerator Custom_KeepsInnerOuterGradient()
        {
            var button = new PillButton();
            button.SetInnerColor("#FF0000");
            yield return MountButton(button);

            Assert.That(button.Variant, Is.EqualTo(PillButtonVariant.Custom));
            Assert.That(Background(button).style.backgroundImage.value.texture, Is.Not.Null);
            Assert.That(Fill(button), Is.Null);
            Assert.IsFalse(button.ClassListContains(PillButton.PrimaryClass));
        }

        [UnityTest]
        public IEnumerator Primary_UsesBrandFill_AndTokens()
        {
            var button = new PillButton { Variant = PillButtonVariant.Primary };
            yield return MountButton(button);

            Assert.IsTrue(button.ClassListContains(PillButton.PrimaryClass));
            Assert.IsTrue(button.ClassListContains(ThemeTokens.ThemedClass));
            Assert.That(Background(button).style.backgroundImage.keyword, Is.EqualTo(StyleKeyword.Null), "no inner/outer texture");
            Assert.That(Fill(button), Is.Not.Null);
            Assert.That(Fill(button).resolvedStyle.backgroundImage.texture, Is.Not.Null, "GradientFill painted the fill layer");
            Assert.That(button.layout.height, Is.EqualTo(52f));
            Assert.That(Label(button).resolvedStyle.color, Is.EqualTo(Color.white), "--c-on-primary");
            Assert.That(Label(button).resolvedStyle.fontSize, Is.EqualTo(14f), "--text-label");
        }

        [UnityTest]
        public IEnumerator Ghost_IsTransparentWithStrongBorder()
        {
            var button = new PillButton { Variant = PillButtonVariant.Ghost };
            yield return MountButton(button);

            var background = Background(button).resolvedStyle;
            Assert.That(background.backgroundColor.a, Is.EqualTo(0f));
            Assert.That(background.borderTopWidth, Is.EqualTo(1.5f).Within(0.5f), "1.5 px, snapped to the pixel grid at scale 1");
            Assert.That(background.borderTopColor.r, Is.EqualTo(0x52 / 255f).Within(0.01f), "--surface-border-strong");
            Assert.That(Label(button).resolvedStyle.color.r, Is.EqualTo(0xF2 / 255f).Within(0.01f), "--surface-fg");
        }

        [UnityTest]
        public IEnumerator Warn_UsesWarnColour()
        {
            var button = new PillButton { Variant = PillButtonVariant.Warn };
            yield return MountButton(button);

            var warn = new Color(0xFB / 255f, 0xBF / 255f, 0x24 / 255f);
            Assert.That(Background(button).resolvedStyle.borderTopColor.r, Is.EqualTo(warn.r).Within(0.01f));
            Assert.That(Label(button).resolvedStyle.color.g, Is.EqualTo(warn.g).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator Compact_IsShorterWithSmallerLabel_AndScalesWithText()
        {
            var button = new PillButton { Variant = PillButtonVariant.Primary, Compact = true };
            yield return MountButton(button);

            Assert.That(button.layout.height, Is.EqualTo(46f));
            Assert.That(Label(button).resolvedStyle.fontSize, Is.EqualTo(13f), "--text-label-sm");

            Root.SetTextScale(1.15f);
            yield return WaitFrames(2);
            Assert.That(Label(button).resolvedStyle.fontSize, Is.EqualTo(15f));
        }

        [UnityTest]
        public IEnumerator Disabled_IsHalfOutline_AndIgnoresClicks()
        {
            var button = new PillButton { Variant = PillButtonVariant.Primary };
            var clicks = 0;
            button.Clicked += () => clicks++;
            yield return MountButton(button);

            button.SetEnabled(false);
            yield return WaitFrames(2);

            Assert.That(Fill(button).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(Background(button).resolvedStyle.borderTopColor.r, Is.EqualTo(0x3F / 255f).Within(0.01f), "--surface-border");
            Assert.That(Label(button).resolvedStyle.color.b, Is.EqualTo(0xAA / 255f).Within(0.01f), "--surface-muted");

            Click(Background(button));
            Assert.That(clicks, Is.EqualTo(0));

            button.SetEnabled(true);
            yield return WaitFrames(2);
            Assert.That(Fill(button).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(Background(button));
            Assert.That(clicks, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BackToCustom_RestoresGradient()
        {
            var button = new PillButton { Variant = PillButtonVariant.Ghost };
            yield return MountButton(button);

            button.Variant = PillButtonVariant.Custom;
            yield return WaitFrames(1);

            Assert.IsFalse(button.ClassListContains(PillButton.GhostClass));
            Assert.That(Background(button).style.backgroundImage.value.texture, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator Custom_ReattachRebuildsTexture()
        {
            var button = new PillButton();
            yield return MountButton(button);

            button.RemoveFromHierarchy();
            yield return WaitFrames(1);
            yield return Mount(button);

            var texture = Background(button).style.backgroundImage.value.texture;
            Assert.IsTrue(texture != null, "the gradient is rebuilt after the detach released it");
        }
    }
}
