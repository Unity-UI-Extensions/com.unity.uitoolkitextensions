/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class SmallU5ComponentTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator LinearProgress_FillFollowsValue()
        {
            Root.SetReduceMotion(true);
            var bar = new LinearProgress(0.66f);
            bar.style.width = 300;
            yield return Mount(bar);

            Assert.That(bar.layout.height, Is.EqualTo(4f));
            Assert.That(bar.Fill.layout.width / bar.layout.width, Is.EqualTo(0.66f).Within(0.01f));
            Assert.That(bar.Fill.resolvedStyle.backgroundImage.texture, Is.Not.Null, "primary to accent gradient");

            bar.Value = 3f;
            Assert.That(bar.Value, Is.EqualTo(1f));
            Assert.IsTrue(bar.ClassListContains(LinearProgress.CompleteClass));
        }

        [UnityTest]
        public IEnumerator AvatarPortrait_InitialsSizesAndImage()
        {
            Assert.That(AvatarPortrait.InitialsOf("Mara Okafor"), Is.EqualTo("MO"));
            Assert.That(AvatarPortrait.InitialsOf("mara"), Is.EqualTo("M"));
            Assert.That(AvatarPortrait.InitialsOf("  "), Is.EqualTo(string.Empty));

            var small = new AvatarPortrait("Mara Okafor", AvatarSize.Small);
            var large = new AvatarPortrait("Test Visitor", AvatarSize.Large);
            var row = new VisualElement();
            row.Add(small);
            row.Add(large);
            yield return Mount(row);

            Assert.That(small.layout.width, Is.EqualTo(40f));
            Assert.That(large.layout.width, Is.EqualTo(56f));
            Assert.That(small.Initials, Is.EqualTo("MO"));
            Assert.That(small.resolvedStyle.borderTopColor.r, Is.EqualTo(0x4F / 255f).Within(0.01f), "--c-primary ring");

            small.SetImage(Background.FromTexture2D(Texture2D.whiteTexture));
            yield return WaitFrames(2);
            Assert.IsTrue(small.ClassListContains(AvatarPortrait.HasImageClass));
            Assert.That(small.Q<Label>(className: AvatarPortrait.InitialsClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator ShutterButton_RingDiscAndPress()
        {
            var shutter = new ShutterButton();
            var presses = 0;
            shutter.Pressed += () => presses++;
            yield return Mount(shutter);

            Assert.That(shutter.layout.width, Is.EqualTo(84f));
            var disc = shutter.Q(className: ShutterButton.DiscClass);
            Assert.That(disc.layout.width, Is.EqualTo(66f));
            Assert.That(disc.resolvedStyle.backgroundImage.texture, Is.Not.Null);
            Assert.That(shutter.resolvedStyle.borderTopWidth, Is.EqualTo(4f));

            Click(shutter);
            shutter.SetEnabled(false);
            Click(shutter);
            Assert.That(presses, Is.EqualTo(1), "disabled during a countdown");
        }

        [UnityTest]
        public IEnumerator AchievementRow_TitleAndOptionalProgress()
        {
            var row = new AchievementRow("Conversationalist · 2 of 3 characters met");
            row.style.width = 320;
            yield return Mount(row);

            Assert.That(row.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no bar by default");
            Assert.That(row.Progress, Is.EqualTo(-1f));

            row.Progress = 0.66f;
            yield return WaitFrames(2);
            Assert.That(row.ProgressBar.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(row.Progress, Is.EqualTo(0.66f).Within(0.001f));
            Assert.That(row.resolvedStyle.backgroundColor.r, Is.EqualTo(0x30 / 255f).Within(0.01f), "--surface-highlight");
            Assert.That(row.Q(className: AchievementRow.BadgeClass).layout.width, Is.EqualTo(34f));
        }
    }

    public class CaptionBarTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator SetCue_ShowsSpeakerAndText_ClearHides()
        {
            Root.SetReduceMotion(true);
            var bar = new CaptionBar();
            bar.style.width = 358;
            yield return Mount(bar);

            Assert.IsTrue(bar.IsEmpty);
            bar.SetCue("Mara", "Welcome to the gallery.");
            yield return WaitFrames(2);

            Assert.IsFalse(bar.IsEmpty);
            Assert.That(bar.Q<Label>(className: CaptionBar.SpeakerClass).text, Is.EqualTo("MARA"));
            Assert.That(bar.Text, Is.EqualTo("Welcome to the gallery."));
            Assert.That(bar.resolvedStyle.borderLeftWidth, Is.EqualTo(3f));
            Assert.That(bar.resolvedStyle.borderLeftColor.r, Is.EqualTo(0x4F / 255f).Within(0.01f), "--c-primary edge");

            bar.SetCue(null, "Narration without a speaker.");
            Assert.IsTrue(bar.ClassListContains(CaptionBar.NoSpeakerClass));

            bar.ClearCue();
            yield return WaitFrames(2);
            Assert.IsTrue(bar.IsEmpty);
            Assert.That(bar.resolvedStyle.visibility, Is.EqualTo(Visibility.Hidden));

            bar.SetCue("Mara", "Next cue.");
            Assert.That(bar.Text, Is.EqualTo("Next cue."), "clearing keeps the caption elements available");
        }

        [UnityTest]
        public IEnumerator NewCue_CrossFades_UnlessReduceMotion()
        {
            var bar = new CaptionBar();
            bar.style.width = 358;
            yield return Mount(bar);
            bar.SetCue("Mara", "First line.");
            yield return WaitFrames(2);

            bar.SetCue("Mara", "Second line.");
            Assert.IsTrue(bar.ClassListContains(CaptionBar.ChangingClass), "fading out");
            Assert.That(bar.Text, Is.EqualTo("First line."), "the old line stays until the fade-out ends");
            yield return WaitMs(ThemeTokens.Motion.FastMs + 100);
            Assert.That(bar.Text, Is.EqualTo("Second line."));
            Assert.IsFalse(bar.ClassListContains(CaptionBar.ChangingClass));

            Root.SetReduceMotion(true);
            yield return WaitFrames(2);
            bar.SetCue("Mara", "Third line.");
            Assert.That(bar.Text, Is.EqualTo("Third line."), "instant with reduce motion");
        }

        [UnityTest]
        public IEnumerator LongCue_IsClampedToThreeLines_AndScalesWithText()
        {
            Root.SetReduceMotion(true);
            var bar = new CaptionBar();
            bar.style.width = 358;
            yield return Mount(bar);

            var text = string.Join(" ", System.Linq.Enumerable.Repeat("This caption runs on and on so it wraps across many lines.", 6));
            bar.SetCue("Mara", text);
            yield return WaitFrames(3);

            var label = bar.TextLabel;
            var clamped = label.layout.height;

            // Three explicit lines fit without clipping, and are as tall as the clamp.
            bar.SetCue("Mara", "One\nTwo\nThree");
            yield return WaitFrames(3);
            var threeLines = label.layout.height;
            bar.MaxLines = 0;
            yield return WaitFrames(3);
            Assert.That(threeLines, Is.EqualTo(label.layout.height).Within(0.5f), "three lines are not clipped");
            Assert.That(clamped, Is.EqualTo(threeLines).Within(1.5f), "a long cue stops at three lines");

            bar.MaxLines = 3;
            bar.SetCue("Mara", text);
            Root.SetTextScale(1.3f);
            yield return WaitFrames(4);
            Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(18f), "--text-label at 1.3");
            var bigClamped = label.layout.height;
            bar.SetCue("Mara", "One\nTwo\nThree");
            yield return WaitFrames(3);
            Assert.That(bigClamped, Is.EqualTo(label.layout.height).Within(1.5f), "still three whole lines when larger");
        }

        [UnityTest]
        public IEnumerator ThreeLineCue_AtPhoneWidth_IsNotClipped()
        {
            // The FS07_2 cue in a bar 12 px in from each side of a 390 px panel.
            Root.SetReduceMotion(true);
            var bar = new CaptionBar();
            bar.style.position = Position.Absolute;
            bar.style.left = 12;
            bar.style.right = 12;
            bar.style.bottom = 166;
            yield return Mount(bar);

            bar.SetCue("Mara", "Welcome to the gallery. I look after the stage-two line. Ask me anything about what you can see.");
            yield return WaitFrames(3);
            var clamped = bar.TextLabel.layout.height;
            bar.MaxLines = 0;
            yield return WaitFrames(3);
            var natural = bar.TextLabel.layout.height;
            var oneLine = bar.TextLabel.MeasureTextSize("Ag", 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).y;
            if (natural <= oneLine * 3.5f)
            {
                Assert.That(clamped, Is.EqualTo(natural).Within(0.5f), "up to three lines show in full");
            }
            else
            {
                Assert.That(clamped, Is.LessThan(natural));
            }
        }

        [UnityTest]
        public IEnumerator SyncTo_FollowsAnyClock_StopsOnNullOrThrow()
        {
            // The bar needs no audio: any clock in seconds drives the timed cues.
            Root.SetReduceMotion(true);
            var bar = new CaptionBar();
            bar.style.width = 358;
            yield return Mount(bar);
            bar.SetCues(new[]
            {
                new CaptionCue(0f, 2f, "Mara", "First line."),
                new CaptionCue(2f, 4f, "Mara", "Second line."),
            });

            var clock = 0.5f;
            bar.SyncTo(() => clock);
            yield return WaitMs(CaptionBar.SyncIntervalMs * 3);
            Assert.That(bar.Text, Is.EqualTo("First line."));

            clock = 2.5f;
            yield return WaitMs(CaptionBar.SyncIntervalMs * 3);
            Assert.That(bar.Text, Is.EqualTo("Second line."), "follows the clock");

            bar.SyncTo(null);
            clock = 0.5f;
            yield return WaitMs(CaptionBar.SyncIntervalMs * 3);
            Assert.That(bar.Text, Is.EqualTo("Second line."), "null stops following");

            // A clock whose source has gone throws; the bar logs it once and stops instead of throwing every tick.
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("source gone"));
            bar.SyncTo(() => throw new System.InvalidOperationException("source gone"));
            yield return WaitMs(CaptionBar.SyncIntervalMs * 4);
            Assert.That(bar.Text, Is.EqualTo("Second line."));
        }
    }

    public class PromptAndSpeechTests : UIDocumentTestBase
    {
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
        public IEnumerator PromptChoices_NumbersSelectionDisabledAndKeys()
        {
            var prompts = new PromptChoices();
            prompts.SetItems(new List<PromptItem>
            {
                new("about", "Tell me about yourself"),
                new("here", "What is going on here?"),
                new("cool", "Show me something cool", enabled: false),
                new("photo", "Pose for a photo"),
            });
            var chosen = new List<string>();
            prompts.ChoiceSelected += chosen.Add;
            prompts.style.width = 350;
            yield return Mount(prompts);

            var row = prompts.ItemElement("photo");
            Assert.That(row.Q<Label>(className: PromptChoices.NumberClass).text, Is.EqualTo("4"));
            Assert.That(row.layout.height, Is.EqualTo(48f));
            Assert.That(row.resolvedStyle.borderTopColor.r, Is.EqualTo(0x52 / 255f).Within(0.01f), "--surface-border-strong");

            Click(prompts.ItemElement("cool"));
            Click(prompts.ItemElement("about"));
            PressKey(prompts, KeyCode.Alpha2);
            PressKey(prompts, KeyCode.Alpha3);
            Assert.That(chosen, Is.EqualTo(new[] { "about", "here" }), "disabled prompts ignore taps and keys");
            Assert.IsTrue(prompts.ItemElement("cool").ClassListContains(PromptChoices.DisabledClass));
        }

        [UnityTest]
        public IEnumerator HoldToTalk_TapVersusHold()
        {
            var hold = new HoldToTalk { TitleText = "Hold to ask in your own words", Subtitle = "or type a question", HoldThresholdMs = 150 };
            var log = new List<string>();
            hold.Tapped += () => log.Add("tap");
            hold.PressStarted += () => log.Add("start");
            hold.PressEnded += released => log.Add(released ? "end" : "cancel");
            hold.style.width = 350;
            yield return Mount(hold);

            var centre = hold.worldBound.center;
            SendPointer<PointerDownEvent>(hold, centre);
            SendPointer<PointerUpEvent>(hold, centre);
            Assert.That(log, Is.EqualTo(new[] { "tap" }));

            SendPointer<PointerDownEvent>(hold, centre);
            yield return WaitMs(300);
            Assert.IsTrue(hold.IsHolding);
            Assert.IsTrue(hold.ClassListContains(HoldToTalk.HoldingClass));
            SendPointer<PointerUpEvent>(hold, centre);
            Assert.That(log, Is.EqualTo(new[] { "tap", "start", "end" }));
            Assert.IsFalse(hold.IsHolding);
        }

        [UnityTest]
        public IEnumerator SpeechInputPanel_StatesTranscriptAndMatch()
        {
            Root.SetReduceMotion(true);
            var panel = new SpeechInputPanel { PrimaryText = "Ask Mara", SecondaryText = "Cancel", Footnote = "Speech is recognised on the phone." };
            var events = new List<string>();
            panel.PrimaryClicked += () => events.Add("primary");
            panel.SecondaryClicked += () => events.Add("secondary");
            panel.style.width = 354;
            yield return Mount(panel);

            Assert.IsTrue(panel.ClassListContains("speechInputPanel--listening"));
            Assert.That(panel.StatusText, Is.EqualTo("Listening"));

            panel.SetTranscript("What's going on over", isFinal: false);
            yield return WaitFrames(2);
            var transcript = panel.Q<Label>(className: SpeechInputPanel.TranscriptClass);
            Assert.That(transcript.text, Is.EqualTo("“What's going on over”"));
            Assert.IsTrue(panel.ClassListContains(SpeechInputPanel.PartialClass));
            Assert.That(transcript.resolvedStyle.color.r, Is.EqualTo(0xA1 / 255f).Within(0.01f), "partial is muted");

            panel.SetTranscript("What's going on over here?", isFinal: true);
            panel.SetMatch("2 · What is going on here?");
            panel.SetState(SpeechState.Matched);
            yield return WaitFrames(2);
            var match = panel.Q(className: SpeechInputPanel.MatchClass);
            Assert.That(match.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(match.Q<Label>(className: SpeechInputPanel.MatchEyebrowClass).text, Is.EqualTo("MATCHED"));
            Assert.IsFalse(panel.ClassListContains(SpeechInputPanel.PartialClass));

            panel.SetState(SpeechState.NoMatch);
            yield return WaitFrames(2);
            Assert.That(match.Q<Label>(className: SpeechInputPanel.MatchEyebrowClass).text, Is.EqualTo("NO MATCH"));
            Assert.That(panel.Q<Label>(className: SpeechInputPanel.StatusClass).resolvedStyle.color.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "warn");

            panel.SetState(SpeechState.Unavailable, "Microphone is off");
            Assert.That(panel.StatusText, Is.EqualTo("Microphone is off"));

            Click(panel.PrimaryButton.Q(className: PillButton.BackgroundClass));
            Click(panel.SecondaryButton.Q(className: PillButton.BackgroundClass));
            Assert.That(events, Is.EqualTo(new[] { "primary", "secondary" }));
        }

        [UnityTest]
        public IEnumerator SpeechInputPanel_MeterAnimatesWhileListening_OnlyWithoutReduceMotion()
        {
            var panel = new SpeechInputPanel();
            yield return Mount(panel);

            Assert.IsTrue(panel.IsAnimating);
            var bar = panel.Query(className: SpeechInputPanel.BarClass).ToList()[2];
            var heights = new HashSet<float>();
            for (var i = 0; i < 8; i++)
            {
                yield return WaitMs(SpeechInputPanel.MeterIntervalMs + 20);
                heights.Add(Mathf.Round(bar.style.height.value.value));
            }

            Assert.That(heights.Count, Is.GreaterThan(2), "the meter moves by itself");

            Root.SetReduceMotion(true);
            yield return WaitMs((SpeechInputPanel.MeterIntervalMs * 2) + 50);
            Assert.IsFalse(panel.IsAnimating);
            var still = bar.style.height.value.value;
            yield return WaitMs((SpeechInputPanel.MeterIntervalMs * 2) + 50);
            Assert.That(bar.style.height.value.value, Is.EqualTo(still), "holds still with reduce motion");
        }
    }
}
