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
    /// The row 14 options added after the flow screens: captions, hint states, prompt headers, announcements.
    /// </summary>
    public class FullSpecTests : UIDocumentTestBase
    {
        private static void Press(PillButton button) => Click(button.Q(className: PillButton.BackgroundClass));

        [UnityTest]
        public IEnumerator CaptionBar_SizesBackgroundsCuesAndAnnounce()
        {
            Root.SetReduceMotion(true);
            A11yAnnouncer.ResetRepeat();
            var spoken = new List<string>();
            void OnAnnounced(string text, A11yPoliteness _) => spoken.Add(text);
            A11yAnnouncer.Announced += OnAnnounced;

            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 600;
            var bar = new CaptionBar { Floating = true, BottomInset = 120f, Announce = true, CaptionSize = CaptionSize.Large };
            host.Add(bar);
            bar.SetCues(new[]
            {
                new CaptionCue(0f, 2f, "Mara", "Welcome to the hall."),
                new CaptionCue(2f, 4f, "Mara", "This turbine ran for forty years."),
            });
            var changes = new List<int>();
            bar.CueChanged += changes.Add;
            yield return Mount(host);

            bar.Position = 0.5f;
            yield return WaitFrames(2);
            Assert.That(bar.Text, Is.EqualTo("Welcome to the hall."));
            Assert.That(bar.TextLabel.resolvedStyle.fontSize, Is.EqualTo(16f), "Large uses --text-body");
            Assert.That(host.worldBound.yMax - bar.worldBound.yMax, Is.EqualTo(120f).Within(0.5f), "clear of the sheet");
            Assert.That(bar.worldBound.x, Is.EqualTo(16f).Within(0.5f));

            bar.Position = 2.5f;
            bar.Position = 5f;
            yield return WaitFrames(2);
            Assert.That(changes, Is.EqualTo(new[] { 1, -1 }), "cue 0 showed when the cues were set, at position 0");
            Assert.IsTrue(bar.IsEmpty, "cleared between cues");
            Assert.That(spoken, Is.EqualTo(new[] { "MARA: Welcome to the hall.", "MARA: This turbine ran for forty years." }));

            bar.Placement = CaptionPlacement.Top;
            bar.TopInset = 130f;
            bar.BackgroundStyle = CaptionBackground.None;
            bar.SetCue("Guide", "Look up.");
            yield return WaitFrames(2);
            Assert.That(bar.worldBound.y, Is.EqualTo(130f).Within(0.5f));
            Assert.That(bar.resolvedStyle.backgroundColor.a, Is.EqualTo(0f));

            bar.Placement = CaptionPlacement.Free;
            bar.FreeOffset = 100f;
            yield return WaitFrames(2);
            Assert.That(host.worldBound.yMax - bar.worldBound.yMax, Is.EqualTo(220f).Within(0.5f), "raised by the free offset");
            A11yAnnouncer.Announced -= OnAnnounced;
        }

        [UnityTest]
        public IEnumerator A11yAnnouncer_DropsPoliteRepeats()
        {
            A11yAnnouncer.ResetRepeat();
            Assert.IsTrue(A11yAnnouncer.Announce("Map ready"));
            Assert.IsFalse(A11yAnnouncer.Announce("Map ready"), "polite repeat dropped");
            Assert.IsTrue(A11yAnnouncer.Announce("Map ready", A11yPoliteness.Assertive));
            Assert.IsFalse(A11yAnnouncer.Announce("  "));
            yield return null;
        }

        [UnityTest]
        public IEnumerator HintImageCard_StateMachine()
        {
            var card = new HintImageCard("Face the big doors from the bottom of the steps.") { LocationName = "Main entrance", Distance = 24f, SkipText = "Start anyway" };
            card.style.width = 358;
            card.SetTips(new[] { "Step back to see more of the doors.", "Turn on more light." });
            var log = new List<string>();
            card.RetryRequested += () => log.Add("retry");
            card.SkipRequested += () => log.Add("skip");
            yield return Mount(card);

            Assert.That(card.Q<Label>(className: HintImageCard.DistanceClass).text, Is.EqualTo("24 m"));
            card.State = HintState.Loading;
            yield return WaitFrames(2);
            Assert.That(card.Q(className: HintImageCard.LoadingClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            card.State = HintState.Near;
            yield return WaitFrames(2);
            Assert.That(card.Proximity, Is.EqualTo(1));
            Assert.That(card.ProximityStrip.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(card.Q(className: HintImageCard.ActionsClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            card.State = HintState.Failed;
            yield return WaitFrames(2);
            Assert.That(card.Q(className: HintImageCard.TipsClass).childCount, Is.EqualTo(2));
            Assert.That(card.Q(className: HintImageCard.TipsClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Press(card.RetryButton);
            Press(card.SkipButton);
            Assert.That(log, Is.EqualTo(new[] { "retry", "skip" }));
        }

        [UnityTest]
        public IEnumerator PromptChoices_HeaderSpeechAndFeedback()
        {
            var prompts = new PromptChoices { Prompt = "Which part spins fastest?", AllowSpeech = true, DismissText = "Not now" };
            prompts.SetSpeaker("Mara Okafor", default);
            prompts.SetItems(new List<PromptItem> { new("a", "The fan"), new("b", "The turbine"), new("c", "The shaft") });
            prompts.style.width = 350;
            var log = new List<string>();
            prompts.ChoiceSelected += id => log.Add(id);
            prompts.Dismissed += () => log.Add("dismiss");
            yield return Mount(prompts);

            Assert.That(prompts.Q<Label>(className: PromptChoices.SpeakerClass).text, Is.EqualTo("MARA OKAFOR"));
            Assert.That(prompts.Avatar.Initials, Is.EqualTo("MO"));
            Assert.That(prompts.Speech.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(prompts.ItemElement("b").Q<Label>(className: PromptChoices.NumberClass).text, Is.EqualTo("2"));

            Click(prompts.ItemElement("b"));
            Click(prompts.Q<Label>(className: PromptChoices.DismissClass));
            Assert.That(log, Is.EqualTo(new[] { "b", "dismiss" }));

            prompts.SetFeedback("b", PromptFeedback.Correct);
            prompts.SetFeedback("a", PromptFeedback.Incorrect);
            yield return WaitFrames(2);
            Assert.That(prompts.ItemElement("b").resolvedStyle.borderTopColor.g, Is.EqualTo(0xC5 / 255f).Within(0.01f), "--c-success");
            Assert.IsTrue(prompts.ItemElement("a").ClassListContains(PromptChoices.IncorrectClass));
            prompts.ClearFeedback();
            Assert.IsFalse(prompts.ItemElement("b").ClassListContains(PromptChoices.CorrectClass));
        }

        [UnityTest]
        public IEnumerator PeerRequests_FormatterBadgeFitAndCustomSlot()
        {
            var bar = new StorageBar { SizeFormatter = bytes => $"{bytes / (1024 * 1024)} MiB" };
            bar.SetSegments(new[] { new StorageSegment("location", "Sample Location", 230L * 1024 * 1024) });
            var grid = new BadgeGrid();
            grid.SetBadges(new[] { new BadgeInfo("talk", "Conversationalist", "Unlocked", true), new BadgeInfo("a", "First steps", "Unlocked", true), new BadgeInfo("b", "Pathfinder", "Locked", false) });
            var row = new SettingsRow { TitleText = "Sample Tour", Trailing = SettingsRowTrailing.Custom };
            row.Add(new Switch());
            row.Add(new VisualElement { style = { width = 40, height = 40 } });
            var column = new VisualElement();
            column.style.width = 350;
            column.Add(bar);
            column.Add(grid);
            column.Add(row);
            yield return Mount(column);
            yield return WaitFrames(3);

            Assert.That(bar.Q<Label>(className: StorageBar.LegendLabelClass).text, Is.EqualTo("Sample Location 230 MiB"));

            var title = grid.TileElement("talk").TitleLabel;
            Assert.That(title.layout.height, Is.LessThan(title.resolvedStyle.fontSize * 2f), "one line, no mid-word break");
            Assert.That(title.resolvedStyle.fontSize, Is.LessThan(12f), "shrunk to fit");
            Assert.That(title.resolvedStyle.fontSize % 1f, Is.EqualTo(0f), "a whole point, as UI Toolkit draws it");
            Assert.That(grid.TileElement("a").TitleLabel.resolvedStyle.fontSize, Is.EqualTo(12f), "short words keep their size");

            var custom = row.Q(className: SettingsRow.CustomClass);
            Assert.That(custom.resolvedStyle.flexDirection, Is.EqualTo(FlexDirection.Row));
            Assert.That(custom[1].worldBound.x, Is.GreaterThan(custom[0].worldBound.x), "side by side");
        }
    }
}
