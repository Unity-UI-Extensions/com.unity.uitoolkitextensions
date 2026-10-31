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
    /// The visual defects found in the app's design-against-build captures: elliptical pill ends, translucent fills that
    /// render too bright in a linear colour space project, and the switch's off state.
    /// </summary>
    public class PillAndSurfaceTests : UIDocumentTestBase
    {
        private static void AssertPill(VisualElement element, string what)
        {
            var expected = Mathf.Min(element.layout.width, element.layout.height) / 2f;
            Assert.That(element.layout.height, Is.GreaterThan(0f), what + " laid out");
            Assert.That(element.resolvedStyle.borderTopLeftRadius, Is.EqualTo(expected).Within(0.01f), what + ": half the shorter side");
            Assert.That(element.resolvedStyle.borderBottomRightRadius, Is.EqualTo(expected).Within(0.01f), what);
        }

        [UnityTest]
        public IEnumerator MakePill_HalvesTheShorterSide_AndFollowsResizes()
        {
            var box = new VisualElement();
            box.style.width = 300;
            box.style.height = 44;
            box.style.borderTopLeftRadius = 50;
            box.MakePill();
            yield return Mount(box, 3);

            AssertPill(box, "300 x 44");
            Assert.That(box.resolvedStyle.borderTopLeftRadius, Is.EqualTo(22f));

            box.style.height = 30;
            yield return WaitFrames(3);
            Assert.That(box.resolvedStyle.borderTopLeftRadius, Is.EqualTo(15f), "follows a resize");

            box.ClearPill();
            box.style.borderTopLeftRadius = 6;
            yield return WaitFrames(2);
            Assert.That(box.resolvedStyle.borderTopLeftRadius, Is.EqualTo(6f), "back to the stylesheet");
            Assert.IsFalse(box.ClassListContains(PillShape.PillClass));
        }

        [UnityTest]
        public IEnumerator Controls_DrawTruePills()
        {
            Root.SetReduceMotion(true);
            var column = new VisualElement();
            column.style.width = 360;
            var primary = new PillButton { Variant = PillButtonVariant.Primary, Text = "Sign in" };
            var ghost = new PillButton { Variant = PillButtonVariant.Ghost, Compact = true, Text = "Cancel scan" };
            var prompts = new PromptChoices();
            prompts.SetItems(new List<PromptItem> { new("a", "Tell me about yourself") });
            var chip = new StatusChip("Offline", StatusState.Warn);
            var segmented = new SegmentedControl(new[] { "AR surfaces", "VPS scan" });
            var toggle = new Switch();
            var tabs = new ScrollingTabs { Variant = ScrollingTabsVariant.Chips };
            tabs.SetItems(new List<TabItem> { new("all", "All"), new("3d", "3D") });
            column.Add(primary);
            column.Add(ghost);
            column.Add(prompts);
            column.Add(chip);
            column.Add(segmented);
            column.Add(toggle);
            column.Add(tabs);
            yield return Mount(column, 4);

            AssertPill(primary.Q(className: PillButton.BackgroundClass), "primary pill (52 px)");
            Assert.That(primary.Q(className: PillButton.BackgroundClass).resolvedStyle.borderTopLeftRadius, Is.EqualTo(26f));
            AssertPill(ghost.Q(className: PillButton.BackgroundClass), "compact ghost pill");
            AssertPill(prompts.ItemElement("a"), "prompt row (48 px)");
            AssertPill(chip, "status chip (32 px)");
            AssertPill(segmented, "segmented track");
            AssertPill(segmented.Q(className: QuadrantStepper.OverlayClass), "segmented indicator");
            AssertPill(toggle.Q(className: Switch.TrackClass), "switch track (46 x 28)");
            AssertPill(tabs.TabElement(0), "category chip");

            tabs.Variant = ScrollingTabsVariant.Tabs;
            yield return WaitFrames(2);
            Assert.IsFalse(tabs.TabElement(0).ClassListContains(PillShape.PillClass), "text tabs are not pills");

            primary.Variant = PillButtonVariant.Custom;
            yield return WaitFrames(2);
            Assert.IsFalse(primary.Q(className: PillButton.BackgroundClass).ClassListContains(PillShape.PillClass), "the Custom variant keeps its own radius");
        }

        [UnityTest]
        public IEnumerator SurfaceFills_AreOpaque_SoLinearBlendingCannotBrightenThem()
        {
            var column = new VisualElement();
            column.style.width = 350;
            var field = new RoundedInputField { Variant = RoundedInputFieldVariant.Themed, Placeholder = "Email" };
            var prompts = new PromptChoices();
            prompts.SetItems(new List<PromptItem> { new("a", "Tell me about yourself") });
            var light = new VisualElement();
            light.AddToClassList(ThemeTokens.LightSurfaceClass);
            var row = new SettingsRow { TitleText = "Captions", Icon = Texture2D.whiteTexture };
            light.Add(row);
            column.Add(field);
            column.Add(prompts);
            column.Add(light);
            yield return Mount(column, 3);

            var fieldStyle = field.resolvedStyle;
            Assert.That(fieldStyle.backgroundColor.a, Is.EqualTo(1f), "opaque input fill");
            Assert.That(fieldStyle.backgroundColor.r, Is.EqualTo(0x20 / 255f).Within(0.01f), "opaque input surface");
            Assert.That(fieldStyle.borderTopColor.a, Is.EqualTo(1f), "opaque input border");
            var input = field.Q(className: RoundedInputField.InputClass);
            Assert.That(input.resolvedStyle.backgroundColor.a, Is.EqualTo(0f), "the inner input stays transparent under the default theme");

            var promptRow = prompts.ItemElement("a");
            Assert.That(promptRow.resolvedStyle.backgroundColor.a, Is.EqualTo(1f), "opaque prompt row");
            Assert.That(promptRow.resolvedStyle.backgroundColor.r, Is.EqualTo(0x27 / 255f).Within(0.01f));

            var tile = row.Q(className: SettingsRow.IconTileClass);
            Assert.That(tile.resolvedStyle.backgroundColor.a, Is.EqualTo(1f), "opaque tile on light");
            Assert.That(tile.resolvedStyle.backgroundColor.r, Is.EqualTo(0xE4 / 255f).Within(0.01f), "neutral highlight on a light surface");
        }

        [UnityTest]
        public IEnumerator LetterSpacing_IsHundredthsOfAnEm()
        {
            // Pins the unit the package's letter-spacing values assume. If this fails, Unity has changed it and every
            // letter-spacing in the package needs converting back to pixels.
            var labels = new List<Label>();
            foreach (var (fontSize, spacing) in new[] { (10f, 0f), (10f, 22f), (20f, 0f), (20f, 22f) })
            {
                var label = new Label("ABCDEFGHIJ");
                label.style.fontSize = fontSize;
                label.style.letterSpacing = spacing;
                label.style.alignSelf = Align.FlexStart;
                labels.Add(label);
            }

            var column = new VisualElement();
            labels.ForEach(column.Add);
            yield return Mount(column, 3);

            // Ten glyphs at .22em gain about 2.2 font sizes of width: 22 px at 10, 44 px at 20.
            var gainAt10 = labels[1].contentRect.width - labels[0].contentRect.width;
            var gainAt20 = labels[3].contentRect.width - labels[2].contentRect.width;
            Assert.That(gainAt10, Is.EqualTo(22f).Within(3f), "10 px font");
            Assert.That(gainAt20, Is.EqualTo(44f).Within(5f), "20 px font: it scales with the font, so it is not pixels");
        }

        [UnityTest]
        public IEnumerator FontSize_DrawsAtTheNextWholePoint()
        {
            // Pins the rounding the package's whole-point font sizes assume. If this fails, Unity draws fractional sizes
            // now, and the designs' half points (10.5, 11.5, 13.5) can go back in.
            var labels = new List<Label>();
            foreach (var size in new[] { 13f, 13.5f, 14f })
            {
                var label = new Label("Point your camera at the floor and move slowly");
                label.style.fontSize = size;
                label.style.alignSelf = Align.FlexStart;
                label.style.whiteSpace = WhiteSpace.NoWrap;
                labels.Add(label);
            }

            var column = new VisualElement();
            labels.ForEach(column.Add);
            yield return Mount(column, 3);

            var at13 = labels[0].contentRect.width;
            var at13Half = labels[1].contentRect.width;
            var at14 = labels[2].contentRect.width;
            Assert.That(at13Half, Is.EqualTo(at14).Within(0.01f), "13.5 draws at 14");
            Assert.That(at13Half, Is.GreaterThan(at13 + 5f), "not at 13");
        }

#if UNITY_EDITOR
        [Test]
        public void Icons_KeepTheirViewBox()
        {
            // Imported cropped to the drawn shape, a glyph fills its box and draws larger than the boards; with the
            // viewBox kept, its padding scales with the box as in a browser. The kept viewport imports at the SVG's
            // width, or 100 units when it has none; a cropped glyph imports at its drawn size (about 14 for Close).
            const string folder = "Packages/com.unity.uitoolkitextensions/Runtime/Assets/Icons";
            var guids = UnityEditor.AssetDatabase.FindAssets("t:VectorImage", new[] { folder });
            Assert.That(guids.Length, Is.EqualTo(20));
            foreach (var guid in guids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var image = UnityEditor.AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
                var svg = System.Text.RegularExpressions.Regex.Match(System.IO.File.ReadAllText(System.IO.Path.GetFullPath(assetPath)), "<svg[^>]*>").Value;
                var width = System.Text.RegularExpressions.Regex.Match(svg, " width=\"([0-9.]+)\"");
                var expected = width.Success ? float.Parse(width.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 100f;
                Assert.That(image.width, Is.EqualTo(expected).Within(0.01f), assetPath + ": the whole viewport");
                Assert.That(image.height, Is.EqualTo(image.width).Within(0.01f), assetPath + ": square, as every viewBox here");
            }
        }
#endif

        [UnityTest]
        public IEnumerator IconBoxes_MatchTheBoards()
        {
            var white = Background.FromTexture2D(Texture2D.whiteTexture);
            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 844;
            var bar = new OrnamentBar { ShowLabels = false };
            bar.SetItems(new List<OrnamentItem> { new("explore", "Explore", white), new("add", "Add content", white) }, primary: 1);
            var header = new ScreenHeader { Variant = ScreenHeaderVariant.Compact };
            var talk = new HoldToTalk();
            var row = new SettingsRow { TitleText = "About" };
            var drawer = new SideDrawer { Variant = DrawerVariant.Permanent };
            var drawerRow = new SettingsRow { TitleText = "Account" };
            drawer.Add(drawerRow);
            var badge = new BadgeTile("First steps", "Unlocked", true);
            host.Add(header);
            host.Add(talk);
            host.Add(row);
            host.Add(badge);
            host.Add(drawer);
            host.Add(bar);
            yield return Mount(host, 4);

            Assert.That(bar.ItemElement(0).Q(className: OrnamentBar.IconClass).resolvedStyle.width, Is.EqualTo(22f), "bar icon");
            Assert.That(bar.ItemElement(1).Q(className: OrnamentBar.IconClass).resolvedStyle.width, Is.EqualTo(20f), "icon in the labelled primary pill");
            Assert.That(header.Q(className: ScreenHeader.Action1ButtonClass).Q(className: "toggleButton__image").resolvedStyle.width, Is.EqualTo(22f), "back chevron");
            Assert.That(talk.Q(className: HoldToTalk.MicIconClass).resolvedStyle.width, Is.EqualTo(26f), "mic in the 56 px button");
            Assert.That(row.Q(className: SettingsRow.ChevronClass).resolvedStyle.width, Is.EqualTo(20f), "settings row chevron");
            Assert.That(drawerRow.Q(className: SettingsRow.ChevronClass).resolvedStyle.width, Is.EqualTo(18f), "drawer row chevron");
            Assert.That(badge.Q(className: BadgeTile.IconClass).layout.width, Is.EqualTo(30f), "star in a 64 px disc");
            Assert.That(BadgeTile.StarBoxFor(96f), Is.EqualTo(48f), "star in a 96 px disc");
        }

        [UnityTest]
        public IEnumerator Switch_OffIsGreyTrackWhiteThumb_OnIsGradient()
        {
            Root.SetReduceMotion(true);
            var light = new VisualElement();
            light.AddToClassList(ThemeTokens.LightSurfaceClass);
            var toggle = new Switch();
            light.Add(toggle);
            yield return Mount(light, 3);

            var track = toggle.Q(className: Switch.TrackClass);
            var thumb = toggle.Q(className: Switch.ThumbClass);
            var fill = toggle.Q(className: Switch.FillClass);
            Assert.That(track.resolvedStyle.backgroundColor.r, Is.EqualTo(0xA1 / 255f).Within(0.01f), "light off track");
            Assert.That(thumb.resolvedStyle.backgroundColor.r, Is.EqualTo(1f).Within(0.01f), "white thumb when off");
            Assert.That(fill.resolvedStyle.opacity, Is.EqualTo(0f));
            Assert.That(track.resolvedStyle.borderTopWidth, Is.EqualTo(0f), "no border");
            Assert.That(track.layout.width, Is.EqualTo(46f), "FS09_5 track 46 x 28");
            Assert.That(track.layout.height, Is.EqualTo(28f));
            Assert.That(thumb.layout.width, Is.EqualTo(22f), "22 px thumb");
            Assert.That(thumb.layout.x, Is.EqualTo(3f), "inset 3 px");

            toggle.value = true;
            yield return WaitFrames(2);
            Assert.That(fill.resolvedStyle.opacity, Is.EqualTo(1f), "primary gradient when on");
            Assert.That(thumb.resolvedStyle.translate.x, Is.EqualTo(18f), "slides to 3 px from the right edge");
            Assert.That(fill.resolvedStyle.backgroundImage.texture, Is.Not.Null);
            Assert.That(thumb.resolvedStyle.backgroundColor.r, Is.EqualTo(1f).Within(0.01f), "white thumb when on");
        }
    }
}
