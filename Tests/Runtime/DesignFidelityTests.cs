/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    /// <summary>
    /// Checks for the first U1 screenshot comparison: header, input, dialog, ornament, coaching card and settings row.
    /// </summary>
    public class DesignFidelityTests : UIDocumentTestBase
    {
        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        [UnityTest]
        public IEnumerator ScreenHeader_Compact_OnLightPage_IsReadable_WithBackChevronAndEyebrow()
        {
            var page = new VisualElement();
            page.SetSurface(ThemeSurface.Light);
            var header = new ScreenHeader { Variant = ScreenHeaderVariant.Compact, Title = "Account", Eyebrow = "OPTIONS" };
            header.Configure(showAction1: true, showTitle: true, showAction2: false, showAction3: false, showAction4Toggle: false);
            page.Add(header);
            yield return Mount(page);

            Assert.IsTrue(header.ClassListContains(ScreenHeader.CompactClass));
            Assert.That(header.Q(className: ScreenHeader.BarClass).layout.height, Is.EqualTo(56f));
            Assert.That(header.Q(className: ScreenHeader.NotchSpacerClass).layout.height, Is.EqualTo(0f));

            var title = header.Q<Label>(className: ScreenHeader.TitleClass);
            Assert.That(title.resolvedStyle.color.r, Is.EqualTo(Hex("#27272A").r).Within(0.01f), "--surface-fg on a light page");
            var eyebrow = header.Q<Label>(className: ScreenHeader.EyebrowClass);
            Assert.That(eyebrow.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(eyebrow.worldBound.yMax, Is.LessThanOrEqualTo(title.worldBound.yMin + 0.5f), "the eyebrow sits above the title");

            var back = header.Q(className: ScreenHeader.Action1ButtonClass).Q(className: ToggleButton.ImageClass);
            Assert.That(back.resolvedStyle.backgroundImage.vectorImage, Is.Not.Null, "built-in back chevron");
            Assert.That(back.resolvedStyle.unityBackgroundImageTintColor.r, Is.EqualTo(Hex("#27272A").r).Within(0.01f), "the chevron is tinted --surface-fg");

            header.SetActionIcon(1, Background.FromTexture2D(Texture2D.whiteTexture));
            yield return WaitFrames(2);
            Assert.That(back.resolvedStyle.backgroundImage.texture, Is.SameAs(Texture2D.whiteTexture));
            Assert.That(back.resolvedStyle.unityBackgroundImageTintColor.r, Is.EqualTo(Hex("#27272A").r).Within(0.01f), "SetActionIcon leaves the tint to USS");
        }

        [UnityTest]
        public IEnumerator ScreenHeader_Default_KeepsOriginalLayout()
        {
            var header = new ScreenHeader { Title = "Settings" };
            yield return Mount(header);

            Assert.That(header.Q(className: ScreenHeader.BarClass).layout.height, Is.EqualTo(80f));
            Assert.That(header.Q(className: ScreenHeader.NotchSpacerClass).layout.height, Is.EqualTo(120f));
            Assert.That(header.Q<Label>(className: ScreenHeader.EyebrowClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator RoundedInputField_Themed_OnDeepAndLight()
        {
            var field = new RoundedInputField { Variant = RoundedInputFieldVariant.Themed, Value = "mara@example.org" };
            field.style.width = 300;
            yield return Mount(field);

            var style = field.resolvedStyle;
            Assert.That(style.height, Is.EqualTo(50f).Within(1f));
            Assert.That(style.borderTopLeftRadius, Is.EqualTo(12f), "--radius-input");
            Assert.That(style.borderTopWidth, Is.EqualTo(1.5f).Within(0.5f));
            Assert.That(style.borderTopColor.r, Is.EqualTo(0x52 / 255f).Within(0.01f), "--surface-input-border on deep");
            Assert.That(style.backgroundColor.r, Is.EqualTo(0x20 / 255f).Within(0.01f), "--surface-input on deep");
            var input = field.Q(className: RoundedInputField.InputClass);
            Assert.That(input.resolvedStyle.backgroundColor.a, Is.EqualTo(0f), "no grey fill from the default theme");
            Assert.That(input.resolvedStyle.color.r, Is.EqualTo(Hex("#FAFAFA").r).Within(0.01f), "--surface-fg");

            field.SetSurface(ThemeSurface.Light);
            yield return WaitFrames(2);
            Assert.That(field.resolvedStyle.backgroundColor, Is.EqualTo(Color.white), "white fill on light");

            field.Invalid = true;
            yield return WaitFrames(2);
            Assert.That(field.resolvedStyle.borderTopColor.r, Is.EqualTo(Hex("#FBBF24").r).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ModalDialog_CardIsOpaqueOverlay_AndBodyIsSoft()
        {
            Root.SetReduceMotion(true);
            var dialog = new ModalDialog();
            yield return Mount(dialog);
            dialog.Show(new DialogOptions { Title = "Sign out?", Body = "Content stays on this phone.", PrimaryText = "Sign out", SecondaryText = "Stay signed in" });
            yield return WaitFrames(2);

            Assert.That(dialog.Card.resolvedStyle.backgroundColor.a, Is.EqualTo(0.96f).Within(0.01f), "--surface-overlay");
            var body = dialog.Q<Label>(className: ModalDialog.BodyClass);
            Assert.That(body.resolvedStyle.color.r, Is.EqualTo(Hex("#E4E4E7").r).Within(0.01f), "--surface-fg-soft");
        }

        [UnityTest]
        public IEnumerator OrnamentBar_PrimaryAlone_IsALabelledPill()
        {
            var ornament = new OrnamentBar { ShowLabels = false };
            ornament.SetItems(new List<OrnamentItem> { new("add", "Add content", Background.FromTexture2D(Texture2D.whiteTexture)) }, primary: 0);
            yield return Mount(ornament, 3);

            var item = ornament.ItemElement(0);
            var label = item.Q<Label>(className: OrnamentBar.LabelClass);
            Assert.That(label.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "the primary label shows with labels off");
            Assert.That(item.resolvedStyle.flexDirection, Is.EqualTo(FlexDirection.Row));
            Assert.That(item.layout.width, Is.GreaterThan(item.layout.height), "a pill, not a circle");
        }

        [UnityTest]
        public IEnumerator CoachingOverlay_TopCard_WithIconTitleAndDetail()
        {
            Root.SetReduceMotion(true);
            var overlay = new CoachingOverlay { Placement = CoachingPlacement.Top, TopInset = 64f };
            overlay.SetIcon(Background.FromTexture2D(Texture2D.whiteTexture));
            yield return Mount(overlay);
            overlay.Show(CoachingMode.FindSurface, "Look around to find a surface", "Move the phone slowly over the floor or a table.");
            yield return WaitFrames(3);

            var cue = overlay.Q(className: CoachingOverlay.CueClass);
            Assert.That(cue.worldBound.yMin, Is.LessThan(overlay.worldBound.height / 3f), "near the top");
            Assert.That(cue.layout.width, Is.EqualTo(Root.layout.width - 32f).Within(1f), "full width inside the margins");

            var disc = overlay.Q(className: CoachingOverlay.DiscClass);
            var text = overlay.Q<Label>(className: CoachingOverlay.TextClass);
            var detail = overlay.Q<Label>(className: CoachingOverlay.DetailClass);
            Assert.That(disc.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(disc.layout.width, Is.EqualTo(40f));
            Assert.That(disc.resolvedStyle.backgroundImage.texture, Is.Not.Null, "primary gradient disc");
            Assert.That(overlay.Q(className: CoachingOverlay.VisualClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the disc replaces the device glyph");
            Assert.That(Mathf.Abs(text.worldBound.center.y - disc.worldBound.center.y), Is.LessThan(12f), "title beside the disc");
            Assert.That(detail.text, Does.StartWith("Move the phone"));
            Assert.That(detail.worldBound.yMin, Is.GreaterThanOrEqualTo(disc.worldBound.yMax - 0.5f), "detail below the header row");
            Assert.IsFalse(overlay.IsSwaying, "no sway without the device glyph");
        }
    }
}
