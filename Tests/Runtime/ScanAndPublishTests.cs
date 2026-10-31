/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ScanAndPublishTests : UIDocumentTestBase
    {
        private static void Press(PillButton button) => Click(button.Q(className: PillButton.BackgroundClass));

        [UnityTest]
        public IEnumerator ScanControls_StagesMinimumAndMaximum()
        {
            Root.SetReduceMotion(true);
            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 844;
            var controls = new ScanControls { MinSeconds = 10f, MaxSeconds = 120f, TopInset = 130f, BottomInset = 0f };
            host.Add(controls);
            var log = new List<string>();
            controls.StartRequested += () => log.Add("start");
            controls.StopRequested += () => log.Add("stop");
            controls.HelpRequested += () => log.Add("help");
            controls.CancelRequested += () => log.Add("cancel");
            controls.MaxReached += () => log.Add("max");
            yield return Mount(host);

            Assert.That(controls.Ring.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no ring before scanning");
            Assert.That(controls.PrimaryButton.Text, Is.EqualTo("Start scan"));
            Assert.That(controls.PrimaryButton.layout.width, Is.EqualTo(controls.SecondaryButton.layout.width).Within(1f));
            var actions = controls.Q(className: ScanControls.ActionsClass);
            Assert.That(host.worldBound.yMax - actions.worldBound.yMax, Is.EqualTo(34f).Within(0.5f), "34 px from the bottom (FS04_2)");
            controls.BottomGap = 0f;
            controls.BottomInset = 16f;
            yield return WaitFrames(2);
            Assert.That(host.worldBound.yMax - actions.worldBound.yMax, Is.EqualTo(16f).Within(0.5f), "a settable gap: 0 plus a 16 px inset (FS04_3 review)");
            controls.BottomGap = ScanControls.DefaultBottomGap;
            controls.BottomInset = 0f;
            Press(controls.SecondaryButton);
            Press(controls.PrimaryButton);

            controls.State = ScanControlsState.Scanning;
            controls.Coverage = 0.62f;
            controls.ElapsedSeconds = 4f;
            controls.SetQuality("Good detail", StatusState.Ok);
            controls.ShowWarning("Slow down");
            controls.SetPath(new[] { new Vector2(0.1f, 0.9f), new Vector2(0.5f, 0.4f), new Vector2(0.9f, 0.2f) });
            yield return WaitFrames(2);
            Assert.That(controls.Ring.layout.width, Is.EqualTo(120f));
            Assert.That(controls.Ring.worldBound.y, Is.EqualTo(130f + 24f).Within(0.5f), "below the top inset");
            Assert.That(controls.Q<Label>(className: ScanControls.TimerClass).text, Is.EqualTo("0:04 / 2:00"));
            Assert.That(controls.PrimaryButton.Variant, Is.EqualTo(PillButtonVariant.Danger));
            Assert.IsFalse(controls.CanStop, "under the minimum");
            Assert.IsFalse(controls.PrimaryButton.enabledSelf);
            Assert.That(controls.Q(className: ScanControls.PathClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            controls.ElapsedSeconds = 12f;
            Press(controls.PrimaryButton);
            Press(controls.SecondaryButton);
            controls.ElapsedSeconds = 121f;
            controls.ElapsedSeconds = 122f;
            Assert.That(log, Is.EqualTo(new[] { "help", "start", "stop", "cancel", "max" }), "max raised once");

            controls.State = ScanControlsState.Saving;
            yield return WaitFrames(2);
            Assert.That(actions.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(controls.Ring.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no ring once the scan stops");
        }

        [UnityTest]
        public IEnumerator QRCodeCard_CaptionAndLinks()
        {
            var card = new QRCodeCard("https://example.com/go/sample/") { ShareText = "Share" };
            card.style.width = 350;
            var copied = new List<string>();
            var shared = new List<string>();
            card.CopyRequested += copied.Add;
            card.ShareRequested += shared.Add;
            yield return Mount(card);

            Assert.That(card.Q<Label>(className: QRCodeCard.CaptionClass).text, Is.EqualTo("example.com/go/sample"));
            Assert.That(card.Code.layout.width, Is.EqualTo(96f));
            Assert.That(card.Code.Code, Is.Not.Null);
            var links = card.Query<Label>(className: QRCodeCard.LinkClass).ToList();
            Click(links[0]);
            Click(links[1]);
            Assert.That(copied, Is.EqualTo(new[] { "https://example.com/go/sample/" }));
            Assert.That(shared.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PublishPanel_StagesStatesAndCode()
        {
            var sheet = new PublishPanel { Detail = "Map 18 MB · 6 items" };
            sheet.style.width = 350;
            sheet.style.height = 480;
            sheet.AddToClassList(ThemeTokens.LightSurfaceClass);
            sheet.SetStages(new List<PublishStage> { new("map", "Map · 18 MB"), new("content", "Content · 6 items, 31 MB") });
            sheet.SetVisibilityOptions(new[] { "Public", "Unlisted" });
            var log = new List<string>();
            sheet.PublishRequested += () => log.Add("publish");
            sheet.KeepDraftRequested += () => log.Add("draft");
            sheet.CloseRequested += () => log.Add("close");
            sheet.CopyRequested += url => log.Add("copy " + url);
            yield return Mount(sheet);

            Assert.That(sheet.Q<Label>(className: PublishPanel.EyebrowClass).text, Is.EqualTo("SPACE"));
            Assert.That(sheet.Q(className: PublishPanel.StagesClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no rows before publishing");
            Assert.That(sheet.CodeCard.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(sheet.Visibility.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(sheet.Q(className: PublishPanel.CloseClass).layout.width, Is.EqualTo(48f));
            var actions = sheet.Q(className: PublishPanel.ActionsClass);
            Assert.That(actions.worldBound.yMax, Is.EqualTo(sheet.worldBound.yMax).Within(0.5f), "actions at the foot");

            Press(sheet.PrimaryButton);
            sheet.State = PublishState.Uploading;
            sheet.SetStageProgress("map", 1f, "Map uploaded · 18 MB");
            sheet.SetStageProgress("content", 0.7f);
            yield return WaitFrames(2);
            Assert.That(sheet.Q(className: PublishPanel.StagesClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            var content = sheet.StageElement("content");
            Assert.That(content.Q<Label>(className: PublishPanel.StagePercentClass).text, Is.EqualTo("70%"));
            Assert.That(sheet.StageElement("map").Q<Label>(className: PublishPanel.StageLabelClass).text, Is.EqualTo("Map uploaded · 18 MB"));
            Assert.IsFalse(sheet.PrimaryButton.enabledSelf, "no second publish while uploading");
            Press(sheet.PrimaryButton);

            sheet.State = PublishState.Live;
            sheet.CodeUrl = "https://example.com/go/sample";
            yield return WaitFrames(2);
            Assert.That(sheet.CodeCard.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(sheet.CodeCard.Q<Label>(className: QRCodeCard.LinkClass));
            Click(sheet.Q(className: PublishPanel.CloseClass));
            Assert.That(log, Is.EqualTo(new[] { "publish", "copy https://example.com/go/sample", "close" }));

            sheet.State = PublishState.Failed;
            sheet.StatusText = "The upload stopped. Your draft is safe on this phone.";
            yield return WaitFrames(2);
            Assert.That(sheet.Q<Label>(className: PublishPanel.StatusClass).resolvedStyle.color.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "warn line");
        }
    }
}
