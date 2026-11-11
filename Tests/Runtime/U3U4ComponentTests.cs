/// Credit SimonDarksideJ

using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ContextPillTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ContextPill_HugsContent_SubtitleInCapitals_LogoSetsWidth()
        {
            Root.SetReduceMotion(true);
            var pill = new ContextPill("Sample Tour", "Stop 4 of 12");
            yield return Mount(pill);

            Assert.That(pill.layout.height, Is.EqualTo(56f));
            Assert.That(pill.layout.width, Is.LessThan(300f), "hugs its content");
            Assert.That(pill.Subtitle, Is.EqualTo("Stop 4 of 12"));
            Assert.That(pill.Q<Label>(className: ContextPill.SubtitleClass).text, Is.EqualTo("STOP 4 OF 12"));
            Assert.IsTrue(pill.ClassListContains(ContextPill.NoLogoClass));
            Assert.That(pill.Q(className: ContextPill.DividerClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            var logo = new Texture2D(44, 22);
            pill.SetLogo(Background.FromTexture2D(logo));
            yield return WaitFrames(2);
            Assert.That(pill.Logo.layout.width, Is.EqualTo(44f), "width follows the aspect at 22 px high");
            Assert.That(pill.Logo.layout.height, Is.EqualTo(ContextPill.LogoHeight));
            Assert.That(pill.Q(className: ContextPill.DividerClass).layout.height, Is.EqualTo(26f));
            Assert.IsFalse(pill.ClassListContains(ContextPill.NoLogoClass));
            Object.Destroy(logo);

            pill.Subtitle = null;
            yield return WaitFrames(2);
            Assert.IsTrue(pill.ClassListContains(ContextPill.NoSubtitleClass));
            Assert.That(pill.Q<Label>(className: ContextPill.SubtitleClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            pill.Title = "Sample Location · Stage two, the long name of a large and well-used space";
            yield return WaitFrames(2);
            Assert.That(pill.layout.width, Is.LessThanOrEqualTo(Root.layout.width), "a long title ellipsises inside the pill");
        }

        [UnityTest]
        public IEnumerator ContextPill_NoText_ShowsTheLogoAlone()
        {
            // The HUD before an experience or space is open.
            Root.SetReduceMotion(true);
            var pill = new ContextPill();
            var logo = new Texture2D(26, 22);
            pill.SetLogo(Background.FromTexture2D(logo));
            yield return Mount(pill);

            var divider = pill.Q(className: ContextPill.DividerClass);
            var texts = pill.Q(className: ContextPill.TextsClass);
            Assert.IsTrue(pill.IsLogoOnly);
            Assert.That(divider.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(texts.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(pill.layout.width, Is.EqualTo(56f), "26 px logo plus 14 px each side, held at the 56 px minimum");
            Assert.That(pill.Logo.worldBound.center.x, Is.EqualTo(pill.worldBound.center.x).Within(0.5f), "centred");

            pill.Title = "Sample Tour";
            yield return WaitFrames(2);
            Assert.IsFalse(pill.IsLogoOnly);
            Assert.That(divider.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(texts.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            pill.Title = string.Empty;
            pill.Subtitle = "Stop 4 of 12";
            yield return WaitFrames(2);
            Assert.IsFalse(pill.IsLogoOnly, "a subtitle alone still shows the text block");
            Assert.That(divider.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Object.Destroy(logo);
        }

        [UnityTest]
        public IEnumerator ContextPill_Tapped_AndDimmedIgnoresTaps()
        {
            Root.SetReduceMotion(true);
            var pill = new ContextPill("Scan a code", "Posters, signs, markers");
            var taps = 0;
            pill.Tapped += () => taps++;
            yield return Mount(pill);

            Click(pill);
            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = pill;
                pill.SendEvent(submit);
            }

            Assert.That(taps, Is.EqualTo(2), "click and keyboard submit");

            pill.Dimmed = true;
            yield return WaitFrames(3);
            Click(pill);
            Assert.That(taps, Is.EqualTo(2), "a dimmed pill ignores taps");
            Assert.That(pill.resolvedStyle.opacity, Is.EqualTo(0.45f).Within(0.01f));
            Assert.That(pill.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.IsFalse(pill.focusable);

            pill.Dimmed = false;
            Click(pill);
            Assert.That(taps, Is.EqualTo(3));
        }
    }

    public class CoachingOverlayAdditionsTests : UIDocumentTestBase
    {
        private static VisualElement BackgroundOf(PillButton button) => button.Q(className: PillButton.BackgroundClass);

        [UnityTest]
        public IEnumerator CoachingOverlay_TwoActions_ShareTheRow_GhostThenPrimary()
        {
            Root.SetReduceMotion(true);
            var overlay = new CoachingOverlay
            {
                Placement = CoachingPlacement.Top,
                ActionText = "Rescan",
                SecondaryActionText = "Keep trying",
            };
            var first = 0;
            var second = 0;
            overlay.ActionInvoked += () => first++;
            overlay.SecondaryActionInvoked += () => second++;
            overlay.Show(CoachingMode.LookAround, "Still looking for your position", "Walk to where you started scanning.");
            yield return Mount(overlay, 3);

            Assert.IsTrue(overlay.ClassListContains(CoachingOverlay.TwoActionsClass));
            Assert.That(overlay.ActionButton.Variant, Is.EqualTo(PillButtonVariant.Ghost));
            Assert.That(overlay.SecondaryActionButton.Variant, Is.EqualTo(PillButtonVariant.Primary));
            Assert.That(overlay.SecondaryActionButton.layout.width, Is.EqualTo(overlay.ActionButton.layout.width).Within(1f), "equal halves");
            Assert.That(overlay.SecondaryActionButton.layout.height, Is.EqualTo(44f));
            Assert.That(overlay.SecondaryActionButton.worldBound.xMin, Is.GreaterThan(overlay.ActionButton.worldBound.xMax), "primary on the right");

            Click(BackgroundOf(overlay.SecondaryActionButton));
            Assert.That(second, Is.EqualTo(1));
            Assert.That(first, Is.EqualTo(0));
            Click(BackgroundOf(overlay.ActionButton));
            Assert.That(first, Is.EqualTo(1));

            overlay.ActionText = null;
            yield return WaitFrames(2);
            Assert.IsFalse(overlay.ClassListContains(CoachingOverlay.TwoActionsClass));
            Assert.That(overlay.ActionButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(overlay.SecondaryActionButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(overlay.SecondaryActionButton.layout.height, Is.EqualTo(40f), "a single action is the compact 40 px pill");

            overlay.SecondaryActionText = null;
            yield return WaitFrames(2);
            Assert.That(overlay.Q(className: CoachingOverlay.ActionsClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator CoachingOverlay_Progress_UnderTheDetail_NegativeHides()
        {
            Root.SetReduceMotion(true);
            var overlay = new CoachingOverlay { Placement = CoachingPlacement.Top, ActionText = "Rescan instead" };
            overlay.Show(CoachingMode.LookAround, "Finding your position", "Stand near where you started the scan.");
            yield return Mount(overlay, 3);

            Assert.That(overlay.Progress, Is.LessThan(0f));
            Assert.That(overlay.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "hidden by default");

            overlay.Progress = 0.45f;
            yield return WaitFrames(2);
            var detail = overlay.Q<Label>(className: CoachingOverlay.DetailClass);
            var actions = overlay.Q(className: CoachingOverlay.ActionsClass);
            Assert.That(overlay.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(overlay.ProgressBar.Value, Is.EqualTo(0.45f));
            Assert.That(overlay.ProgressBar.layout.height, Is.EqualTo(4f));
            Assert.That(overlay.ProgressBar.worldBound.yMin, Is.GreaterThanOrEqualTo(detail.worldBound.yMax), "under the detail");
            Assert.That(overlay.ProgressBar.worldBound.yMax, Is.LessThanOrEqualTo(actions.worldBound.yMin), "above the actions");

            overlay.Progress = -1f;
            yield return WaitFrames(2);
            Assert.That(overlay.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            overlay.ProgressIndeterminate = true;
            yield return WaitFrames(2);
            Assert.That(overlay.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "indeterminate shows the bar");
            Assert.IsTrue(overlay.ProgressBar.Indeterminate);
            Assert.IsFalse(overlay.ProgressBar.IsSweeping, "rests with reduce motion");

            overlay.ProgressIndeterminate = false;
            yield return WaitFrames(2);
            Assert.That(overlay.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator CoachingOverlay_BottomPlacement_SitsAboveTheInset()
        {
            // FS04_3: the card sits above the scan actions so the coverage ring stays clear.
            Root.SetReduceMotion(true);
            var overlay = new CoachingOverlay { Placement = CoachingPlacement.Bottom, BottomInset = 92f };
            overlay.SetIcon(Background.FromTexture2D(Texture2D.whiteTexture));
            overlay.Show(CoachingMode.LookAround, "Good so far", "Walk slowly along the walls.");
            yield return Mount(overlay, 3);

            var cue = overlay.Q(className: CoachingOverlay.CueClass);
            Assert.IsTrue(overlay.ClassListContains(CoachingOverlay.CardClass));
            Assert.IsTrue(overlay.ClassListContains(CoachingOverlay.BottomClass));
            Assert.IsFalse(overlay.ClassListContains(CoachingOverlay.TopClass));
            Assert.That(overlay.BottomInset, Is.EqualTo(92f));
            Assert.That(cue.worldBound.yMax, Is.EqualTo(Root.worldBound.yMax - 92f - 16f).Within(1f), "inset plus the 16 px card margin");
            Assert.That(cue.worldBound.width, Is.EqualTo(Root.worldBound.width - 32f).Within(1f), "full width, like the top card");
            var disc = overlay.Q(className: CoachingOverlay.DiscClass);
            var text = overlay.Q<Label>(className: CoachingOverlay.TextClass);
            Assert.That(text.worldBound.xMin, Is.GreaterThan(disc.worldBound.xMax), "icon and title on one row");

            overlay.Placement = CoachingPlacement.Top;
            yield return WaitFrames(2);
            Assert.IsFalse(overlay.ClassListContains(CoachingOverlay.BottomClass));
            Assert.IsTrue(overlay.ClassListContains(CoachingOverlay.CardClass));
            Assert.That(cue.worldBound.yMin, Is.LessThan(Root.worldBound.center.y), "back at the top");

            overlay.Placement = CoachingPlacement.Centre;
            yield return WaitFrames(2);
            Assert.IsFalse(overlay.ClassListContains(CoachingOverlay.CardClass));
        }

        [UnityTest]
        public IEnumerator PillButton_LeadingIcon_CentredWithTheLabel()
        {
            Root.SetReduceMotion(true);
            var button = new PillButton { Variant = PillButtonVariant.Primary, Text = "Stop and save" };
            button.style.width = 240;
            button.SetIcon(Background.FromTexture2D(Texture2D.whiteTexture));
            yield return Mount(button, 3);

            var label = button.Q<Label>(className: PillButton.LabelClass);
            Assert.IsTrue(button.ClassListContains(PillButton.HasIconClass));
            Assert.That(button.Icon.layout.width, Is.EqualTo(20f));
            Assert.That(button.Icon.layout.height, Is.EqualTo(20f));
            Assert.That(label.worldBound.xMin - button.Icon.worldBound.xMax, Is.EqualTo(8f).Within(0.5f), "8 px gap");
            var groupCentre = (button.Icon.worldBound.xMin + label.worldBound.xMax) / 2f;
            Assert.That(groupCentre, Is.EqualTo(button.worldBound.center.x).Within(1f), "icon and label centre together");
            Assert.That(button.Icon.resolvedStyle.unityBackgroundImageTintColor.r, Is.GreaterThan(0.9f), "--c-on-primary");

            button.Variant = PillButtonVariant.Warn;
            yield return WaitFrames(2);
            Assert.That(button.Icon.resolvedStyle.unityBackgroundImageTintColor.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "--c-warn");

            button.SetIcon(default);
            yield return WaitFrames(2);
            Assert.IsFalse(button.ClassListContains(PillButton.HasIconClass));
            Assert.That(button.Icon.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(label.worldBound.center.x, Is.EqualTo(button.worldBound.center.x).Within(1f), "the label alone is centred again");
            Assert.That(label.resolvedStyle.paddingLeft, Is.EqualTo(16f), "with its variant padding back");
        }

        [UnityTest]
        public IEnumerator PillButton_Danger_FillsWithTheDangerGradient()
        {
            // FS04_3: the red filled "Stop and save".
            Root.SetReduceMotion(true);
            var button = new PillButton { Variant = PillButtonVariant.Danger, Text = "Stop and save" };
            button.SetIcon(Background.FromTexture2D(Texture2D.whiteTexture));
            yield return Mount(button, 3);

            Assert.IsTrue(button.ClassListContains(PillButton.DangerClass));
            Assert.That(button.layout.height, Is.EqualTo(52f));
            var fill = button.Q(className: PillButton.FillClass);
            var texture = fill.resolvedStyle.backgroundImage.texture;
            Assert.That(texture, Is.Not.Null, "GradientFill painted the danger fill");
            var start = texture.GetPixel(0, 0);
            var end = texture.GetPixel(texture.width - 1, 0);
            Assert.That(start.r, Is.EqualTo(0xDC / 255f).Within(0.01f), "--grad-danger-start (--c-danger)");
            Assert.That(start.g, Is.EqualTo(0x26 / 255f).Within(0.01f));
            Assert.That(end.r, Is.EqualTo(0x5A / 255f).Within(0.01f), "--grad-danger-end");
            Assert.That(end.g, Is.EqualTo(0x22 / 255f).Within(0.01f));
            var label = button.Q<Label>(className: PillButton.LabelClass);
            Assert.That(label.resolvedStyle.color, Is.EqualTo(Color.white), "--c-on-danger");
            Assert.That(button.Icon.resolvedStyle.unityBackgroundImageTintColor, Is.EqualTo(Color.white), "icon on-danger");

            button.SetEnabled(false);
            yield return WaitFrames(2);
            Assert.That(fill.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "disabled is an outline");
            Assert.That(label.resolvedStyle.color.r, Is.EqualTo(0xA1 / 255f).Within(0.01f), "--surface-muted");
        }

        [UnityTest]
        public IEnumerator LinearProgress_Indeterminate_SweepsASegment()
        {
            var bar = new LinearProgress(0.8f);
            bar.style.width = 300;
            yield return Mount(bar);

            bar.Indeterminate = true;
            yield return WaitFrames(2);
            Assert.That(bar.Fill.layout.width / bar.layout.width, Is.EqualTo(LinearProgress.SegmentPercent / 100f).Within(0.01f));
            Assert.IsFalse(bar.ClassListContains(LinearProgress.CompleteClass));
            Assert.IsTrue(bar.IsSweeping);

            yield return WaitMs(LinearProgress.SweepIntervalMs + 200);
            Assert.IsTrue(bar.ClassListContains(LinearProgress.SweptClass), "the first sweep phase");

            Root.SetReduceMotion(true);
            Assert.IsFalse(bar.IsSweeping);

            bar.Indeterminate = false;
            yield return WaitFrames(2);
            Assert.IsFalse(bar.ClassListContains(LinearProgress.SweptClass));
            Assert.That(bar.Fill.style.width.value.value, Is.EqualTo(80f).Within(0.01f), "back to the value");
        }
    }

    public class ScanFrameTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ScanFrame_CentredSquare_CornersAndTones()
        {
            Root.SetReduceMotion(true);
            var scan = new ScanFrame();
            yield return Mount(scan);

            var frame = scan.Frame.worldBound;
            Assert.That(frame.width, Is.EqualTo(ScanFrame.DefaultSize));
            Assert.That(frame.height, Is.EqualTo(ScanFrame.DefaultSize));
            Assert.That(frame.center.x, Is.EqualTo(Root.worldBound.center.x).Within(1f));
            Assert.That(frame.center.y, Is.EqualTo(Root.worldBound.center.y).Within(1f));

            var corners = scan.Query(className: ScanFrame.CornerClass).ToList();
            Assert.That(corners.Count, Is.EqualTo(4));
            var topLeft = scan.Q(className: $"{ScanFrame.CornerClass}--topLeft");
            var bottomRight = scan.Q(className: $"{ScanFrame.CornerClass}--bottomRight");
            Assert.That(topLeft.layout.width, Is.EqualTo(26f));
            Assert.That(topLeft.resolvedStyle.borderTopWidth, Is.EqualTo(3f));
            Assert.That(topLeft.resolvedStyle.borderRightWidth, Is.EqualTo(0f));
            Assert.That(topLeft.worldBound.x, Is.EqualTo(frame.x).Within(0.5f));
            Assert.That(topLeft.worldBound.y, Is.EqualTo(frame.y).Within(0.5f));
            Assert.That(bottomRight.worldBound.max.x, Is.EqualTo(frame.max.x).Within(0.5f));
            Assert.That(bottomRight.worldBound.max.y, Is.EqualTo(frame.max.y).Within(0.5f));
            Assert.That(topLeft.resolvedStyle.borderTopColor.b, Is.EqualTo(0xFC / 255f).Within(0.01f), "accent");

            scan.Tone = ScanFrameTone.Success;
            yield return WaitFrames(3);
            Assert.That(topLeft.resolvedStyle.borderTopColor.g, Is.EqualTo(0xC5 / 255f).Within(0.01f), "success");

            scan.Tone = ScanFrameTone.Warn;
            yield return WaitFrames(3);
            Assert.That(topLeft.resolvedStyle.borderTopColor.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "warn");

            scan.Size = 200;
            yield return WaitFrames(2);
            Assert.That(scan.Frame.layout.width, Is.EqualTo(200f));
        }

        [UnityTest]
        public IEnumerator ScanFrame_SetTargetRect_HugsWithPadding_ClearReturnsToCentre()
        {
            Root.SetReduceMotion(true);
            var scan = new ScanFrame();
            yield return Mount(scan);

            // The marker card in FS06_6: brackets 20 px outside it.
            scan.SetTargetRect(new Rect(Root.worldBound.x + 110, Root.worldBound.y + 230, 170, 210));
            yield return WaitFrames(2);
            Assert.IsTrue(scan.IsTargeted);
            var frame = scan.Frame.worldBound;
            Assert.That(frame.x - Root.worldBound.x, Is.EqualTo(90f).Within(0.5f));
            Assert.That(frame.y - Root.worldBound.y, Is.EqualTo(210f).Within(0.5f));
            Assert.That(frame.width, Is.EqualTo(210f).Within(0.5f));
            Assert.That(frame.height, Is.EqualTo(250f).Within(0.5f));

            scan.ClearTarget();
            yield return WaitFrames(2);
            Assert.IsFalse(scan.IsTargeted);
            Assert.That(scan.Frame.worldBound.width, Is.EqualTo(ScanFrame.DefaultSize));
            Assert.That(scan.Frame.worldBound.center.y, Is.EqualTo(Root.worldBound.center.y).Within(1f));
        }

        [UnityTest]
        public IEnumerator ScanFrame_Searching_PulsesUnlessReduceMotion()
        {
            var scan = new ScanFrame { Searching = true };
            yield return Mount(scan);
            Assert.IsTrue(scan.IsPulsing);

            yield return WaitMs(ScanFrame.PulseIntervalMs + 200);
            Assert.IsTrue(scan.ClassListContains(ScanFrame.PulsedClass));

            Root.SetReduceMotion(true);
            Assert.IsFalse(scan.IsPulsing);
            yield return WaitMs(ScanFrame.PulseIntervalMs + 200);
            Assert.IsFalse(scan.ClassListContains(ScanFrame.PulsedClass), "holds still with reduce motion");

            scan.Searching = false;
            Assert.IsFalse(scan.ClassListContains(ScanFrame.SearchingClass));
        }
    }

    public class QRCodeViewTests : UIDocumentTestBase
    {
        private const string Link = "https://example.com/go/sample";

        // ZXing.Net's encoder output for Link at level M (version 3, mask 6), the independent reference.
        private static readonly string[] ZxingReference =
        {
            "#######.##.#####..#.#.#######",
            "#.....#.#.##.###.#.#..#.....#",
            "#.###.#.#....##.....#.#.###.#",
            "#.###.#..##...##.#.##.#.###.#",
            "#.###.#.#.##.#.#.#.#..#.###.#",
            "#.....#..##.###...#.#.#.....#",
            "#######.#.#.#.#.#.#.#.#######",
            ".........##...#..............",
            "#..######.##.#####.#.#..#.###",
            "#.###..###..#.#...##.#.##.##.",
            ".###..###.#...#..##..#..#.#..",
            "#.#.#..#..##..##.#.##.#..#..#",
            "#..#####.##..##.....#.##....#",
            "....##.#...##....##.#.#######",
            "..#..####..##.##...##..##.#.#",
            "#..#...##..#.##.#.....#.#.#.#",
            "#.#.#.#....#...#...##..#.#...",
            "#..#.....###.....#.##...#.##.",
            "##.########..##.####..####..#",
            "####.#..#.##.#.##.#..#...##..",
            "#######...####...#..########.",
            "........#.##.#..###.#...##...",
            "#######.#...##.###.##.#.##...",
            "#.....#.#.##.#..#####...#..##",
            "#.###.#.###.##.##..#######...",
            "#.###.#.#.#...#.#.#....#...#.",
            "#.###.#....####.#..#.#.##.###",
            "#.....#..##.####.....#.####.#",
            "#######.#...###..######.#....",
        };

        [Test]
        public void QrEncoder_MatchesZxingReference()
        {
            var code = QrEncoder.Encode(Link, QrErrorCorrection.Medium);
            Assert.That(code.Version, Is.EqualTo(3));
            Assert.That(code.Mask, Is.EqualTo(6), "the lowest penalty mask");
            Assert.That(code.Size, Is.EqualTo(ZxingReference.Length));
            for (var y = 0; y < code.Size; y++)
            {
                var row = new char[code.Size];
                for (var x = 0; x < code.Size; x++)
                {
                    row[x] = code[x, y] ? '#' : '.';
                }

                Assert.That(new string(row), Is.EqualTo(ZxingReference[y]), $"row {y}");
            }
        }

        [Test]
        public void QrEncoder_PicksModeAndSmallestVersion()
        {
            // Version 1 capacities from the specification: numeric, alphanumeric, byte.
            Assert.That(QrEncoder.Encode(new string('7', 41), QrErrorCorrection.Low).Version, Is.EqualTo(1));
            Assert.That(QrEncoder.Encode(new string('7', 42), QrErrorCorrection.Low).Version, Is.EqualTo(2));
            Assert.That(QrEncoder.Encode(new string('A', 25), QrErrorCorrection.Low).Version, Is.EqualTo(1));
            Assert.That(QrEncoder.Encode(new string('A', 26), QrErrorCorrection.Low).Version, Is.EqualTo(2));
            Assert.That(QrEncoder.Encode(new string('a', 17), QrErrorCorrection.Low).Version, Is.EqualTo(1));
            Assert.That(QrEncoder.Encode(new string('a', 18), QrErrorCorrection.Low).Version, Is.EqualTo(2));
            Assert.That(QrEncoder.Encode(new string('a', 7), QrErrorCorrection.High).Version, Is.EqualTo(1));
            Assert.That(QrEncoder.Encode(new string('a', 8), QrErrorCorrection.High).Version, Is.EqualTo(2));
            Assert.That(QrEncoder.Encode(new string('a', 2331), QrErrorCorrection.Medium).Version, Is.EqualTo(40));
            Assert.Throws<System.ArgumentException>(() => QrEncoder.Encode(new string('a', 2332), QrErrorCorrection.Medium));
            Assert.That(QrEncoder.AlignmentPatternPositions(32), Is.EqualTo(new[] { 6, 34, 60, 86, 112, 138 }));
        }

        [UnityTest]
        public IEnumerator QRCodeView_DrawsWithQuietZone_DarkOnLight()
        {
            var view = new QRCodeView(Link, 220f);
            yield return Mount(view, 3);

            Assert.That(view.layout.width, Is.EqualTo(220f));
            Assert.That(view.layout.height, Is.EqualTo(220f));
            Assert.That(view.Code, Is.Not.Null);
            Assert.That(view.ModulesPerSide, Is.EqualTo(29 + 2 * QRCodeView.DefaultQuietZone));

            var texture = view.Texture;
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.width, Is.EqualTo(view.ModulesPerSide));
            Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), "crisp module edges");
            Assert.That(view.resolvedStyle.backgroundImage.texture, Is.SameAs(texture));

            // The top-left finder's corner module sits inside the quiet zone; texture rows run bottom up.
            var top = texture.height - 1;
            Color32 quiet = texture.GetPixel(0, top);
            Color32 finder = texture.GetPixel(4, top - 4);
            Assert.That(quiet, Is.EqualTo((Color32)Color.white));
            Assert.That(finder.r, Is.EqualTo(0x34), "--c-navy-2 modules");
            Assert.That(finder.g, Is.EqualTo(0x34));
            Assert.That(finder.b, Is.EqualTo(0x3A));
            Assert.That(view.DarkColor.r, Is.EqualTo(0x34 / 255f).Within(0.01f));

            var shared = view.CreateTexture(8);
            Assert.That(shared.width, Is.EqualTo(view.ModulesPerSide * 8));
            Assert.IsTrue(shared.isReadable, "can be saved as a PNG");
            Assert.That(ImageConversion.EncodeToPNG(shared).Length, Is.GreaterThan(0));
            Object.Destroy(shared);

            view.QuietZone = 2;
            yield return WaitFrames(2);
            Assert.That(view.Texture.width, Is.EqualTo(29 + 4));

            view.RemoveFromHierarchy();
            Assert.That(view.Texture, Is.Null, "released when it leaves the panel");
        }

        [UnityTest]
        public IEnumerator QRCodeView_EmptyAndTooLong()
        {
            var view = new QRCodeView();
            yield return Mount(view);

            Assert.IsTrue(view.ClassListContains(QRCodeView.EmptyClass));
            Assert.That(view.Code, Is.Null);
            Assert.That(view.Texture, Is.Null);
            Assert.That(view.CreateTexture(), Is.Null);
            Assert.That(view.layout.width, Is.EqualTo(200f), "USS default size");

            view.Text = "4471";
            yield return WaitFrames(2);
            Assert.IsFalse(view.ClassListContains(QRCodeView.EmptyClass));
            Assert.That(view.Code.Version, Is.EqualTo(1));

            LogAssert.Expect(LogType.Warning, new Regex("QRCodeView: The text is too long"));
            view.Text = new string('x', 3000);
            yield return WaitFrames(2);
            Assert.That(view.Code, Is.Null);
            Assert.IsTrue(view.ClassListContains(QRCodeView.EmptyClass));
            Assert.That(view.resolvedStyle.backgroundImage.texture, Is.Null);
        }
    }

    public class HintImageCardTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator HintImageCard_ImageTagAndText()
        {
            var card = new HintImageCard("Face the big doors like the picture and hold still. Closer is better.");
            card.style.width = 358;
            yield return Mount(card, 3);

            var image = card.Q(className: HintImageCard.ImageClass);
            Assert.That(image.layout.width, Is.EqualTo(110f));
            Assert.That(image.layout.height, Is.EqualTo(80f));
            Assert.That(card.Q<Label>(className: HintImageCard.TagClass).text, Is.EqualTo("HINT"));
            Assert.That(card.Q(className: HintImageCard.PlaceholderClass).resolvedStyle.backgroundImage.texture, Is.Not.Null, "gradient placeholder");
            Assert.That(card.ProximityStrip.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the strip is optional");
            var text = card.Q<Label>(className: HintImageCard.TextClass);
            Assert.That(text.worldBound.xMin, Is.GreaterThan(image.worldBound.xMax), "text beside the picture");

            card.SetImage(Background.FromTexture2D(Texture2D.whiteTexture));
            yield return WaitFrames(2);
            Assert.IsTrue(card.ClassListContains(HintImageCard.HasImageClass));
            Assert.That(card.Q(className: HintImageCard.PlaceholderClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            card.TagText = null;
            yield return WaitFrames(2);
            Assert.That(card.Q<Label>(className: HintImageCard.TagClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            card.Embedded = true;
            yield return WaitFrames(2);
            Assert.That(image.layout.width, Is.EqualTo(84f));
            Assert.That(image.layout.height, Is.EqualTo(62f));
            Assert.That(card.resolvedStyle.backgroundColor.a, Is.EqualTo(0f), "no card chrome inside another card");
            Assert.That(card.resolvedStyle.borderTopWidth, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator HintImageCard_ProximityStrip()
        {
            var card = new HintImageCard("Face the big doors.");
            card.style.width = 358;
            yield return Mount(card, 3);

            card.SetProximity(1);
            yield return WaitFrames(2);
            Assert.That(card.ProximityStrip.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "setting proximity shows the strip");
            var segments = card.ProximitySegments;
            Assert.That(segments.Count, Is.EqualTo(3));
            Assert.That(segments[0].layout.width, Is.EqualTo(segments[2].layout.width).Within(1f), "equal segments");
            Assert.That(segments[0].Q<Label>().text, Is.EqualTo("FAR"));
            Assert.That(segments[1].Q<Label>().text, Is.EqualTo("NEAR"));
            Assert.That(segments[2].Q<Label>().text, Is.EqualTo("FOUND"));
            Assert.IsTrue(segments[1].ClassListContains(HintImageCard.CurrentSegmentClass));
            Assert.IsFalse(segments[0].ClassListContains(HintImageCard.CurrentSegmentClass));
            Assert.That(segments[1].Q(className: HintImageCard.SegmentFillClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(segments[0].Q(className: HintImageCard.SegmentFillClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(segments[1].Q<Label>().resolvedStyle.color.r, Is.GreaterThan(0.9f), "on-primary text on the filled segment");

            card.SetProximityLabels("Cold", "Warm", "Hot", "Found");
            yield return WaitFrames(2);
            Assert.That(card.ProximitySegments.Count, Is.EqualTo(4));
            Assert.That(card.Proximity, Is.EqualTo(1), "keeps the current segment");
            Assert.IsTrue(card.ProximitySegments[1].ClassListContains(HintImageCard.CurrentSegmentClass));
            Assert.That(card.ProximityLabels, Is.EqualTo("Cold,Warm,Hot,Found"));

            card.SetProximity(9);
            Assert.That(card.Proximity, Is.EqualTo(-1), "out of range clears");
            foreach (var segment in card.ProximitySegments)
            {
                Assert.IsFalse(segment.ClassListContains(HintImageCard.CurrentSegmentClass));
            }
        }
    }

    public class WayfinderCardTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator WayfinderCard_DiscTitleDetail_BearingAndArrival()
        {
            var card = new WayfinderCard("Stop 3 · 18 m", "Ahead on the left, past the test cell. Appears within 10 m.");
            card.style.width = 358;
            yield return Mount(card, 3);

            var disc = card.Q(className: WayfinderCard.DiscClass);
            Assert.That(disc.layout.width, Is.EqualTo(52f));
            Assert.That(disc.resolvedStyle.backgroundImage.texture, Is.Not.Null, "gradient disc");
            Assert.That(card.Arrow.layout.width, Is.EqualTo(26f));
            Assert.That(card.Q<Label>(className: WayfinderCard.TitleClass).text, Is.EqualTo("Stop 3 · 18 m"));
            Assert.That(card.Q<Label>(className: WayfinderCard.DetailClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            card.SetBearing(35f);
            yield return WaitFrames(2);
            Assert.That(card.Arrow.resolvedStyle.rotate.angle.ToDegrees(), Is.EqualTo(35f).Within(0.01f));

            card.SetBearing(370f);
            Assert.That(card.Bearing, Is.EqualTo(10f).Within(0.01f), "normalised");
            card.SetBearing(-190f);
            Assert.That(card.Bearing, Is.EqualTo(170f).Within(0.01f), "the short way round");
            card.SetBearing(float.NaN);
            Assert.That(card.Bearing, Is.EqualTo(0f));

            card.Arrived = true;
            yield return WaitFrames(2);
            Assert.That(card.Arrow.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(card.Q(className: WayfinderCard.CheckClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            card.Detail = null;
            yield return WaitFrames(2);
            Assert.That(card.Q<Label>(className: WayfinderCard.DetailClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }
    }
}
