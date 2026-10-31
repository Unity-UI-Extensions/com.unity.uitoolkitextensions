/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    /// <summary>
    /// Timers inside hidden screens. On the S24's idle HUD, package animations in hidden screens kept ticking, so the
    /// panel restyled and laid out every frame (heat work U1).
    /// </summary>
    public class DisplayedTimerTests : UIDocumentTestBase
    {
        private static DisplayedTimer TimerOf(object control, string field) =>
            (DisplayedTimer)control.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(control);

        [UnityTest]
        public IEnumerator PausesUnderAHiddenAncestor_ResumesWhenShown()
        {
            var screen = new VisualElement();
            var inner = new VisualElement();
            var element = new VisualElement();
            element.style.width = 20;
            element.style.height = 20;
            inner.Add(element);
            screen.Add(inner);
            var ticks = 0;
            var timer = DisplayedTimer.Start(element, () => ticks++, 0);
            yield return Mount(screen, 3);

            Assert.That(ticks, Is.GreaterThan(0), "ticks while displayed");
            Assert.IsTrue(timer.IsTicking);

            screen.style.display = DisplayStyle.None;
            yield return WaitFrames(3);
            var hiddenAt = ticks;
            yield return WaitFrames(6);
            Assert.That(ticks, Is.EqualTo(hiddenAt), "no ticks while an ancestor is display: none");
            Assert.IsFalse(timer.IsTicking);
            Assert.IsTrue(timer.IsRunning, "paused, not stopped");

            screen.style.display = DisplayStyle.Flex;
            yield return WaitFrames(4);
            Assert.That(ticks, Is.GreaterThan(hiddenAt), "resumes when shown");
            Assert.IsTrue(timer.IsTicking);

            timer.Stop();
            var stoppedAt = ticks;
            screen.style.display = DisplayStyle.None;
            yield return WaitFrames(2);
            screen.style.display = DisplayStyle.Flex;
            yield return WaitFrames(4);
            Assert.That(ticks, Is.EqualTo(stoppedAt), "Stop is final");
            Assert.IsFalse(timer.IsRunning);
        }

        [UnityTest]
        public IEnumerator HiddenFromTheStart_NeverTicks_UntilShown()
        {
            var screen = new VisualElement();
            screen.style.display = DisplayStyle.None;
            var element = new VisualElement();
            element.style.width = 20;
            element.style.height = 20;
            screen.Add(element);
            var ticks = 0;
            var timer = DisplayedTimer.Start(element, () => ticks++, 0);
            yield return Mount(screen, 6);

            Assert.That(ticks, Is.EqualTo(0));
            Assert.IsFalse(timer.IsTicking);

            screen.style.display = DisplayStyle.Flex;
            yield return WaitFrames(4);
            Assert.That(ticks, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator PackageTimers_RestInAHiddenScreen()
        {
            var camera = new GameObject("DisplayedTimerTests camera").AddComponent<Camera>();
            var target = new GameObject("DisplayedTimerTests target").transform;
            target.position = camera.transform.position + camera.transform.forward * 3f;
            var audio = new GameObject("DisplayedTimerTests audio").AddComponent<AudioSource>();
            try
            {
                var screen = new VisualElement();
                screen.style.width = 390;
                screen.style.height = 844;
                screen.style.display = DisplayStyle.None;

                var spinner = new LoadingIcon();
                var skeleton = new SkeletonTile(SkeletonShape.Tile, 2);
                var progress = new LinearProgress { Indeterminate = true };
                var frame = new ScanFrame { Searching = true };
                var coaching = new CoachingOverlay();
                var speech = new SpeechInputPanel { State = SpeechState.Listening };
                var captions = new CaptionBar();
                var safeArea = new SafeAreaRoot();
                var label = new WorldAnchoredLabel { Text = "Lantern" };
                var tabs = new ScrollingTabs();
                tabs.style.width = 200;
                var items = new List<TabItem>();
                for (var i = 0; i < 8; i++)
                {
                    items.Add(new TabItem("t" + i, "Category " + i));
                }

                tabs.SetItems(items);
                foreach (var control in new VisualElement[] { spinner, skeleton, progress, frame, coaching, speech, captions, safeArea, label, tabs })
                {
                    screen.Add(control);
                }

                yield return Mount(screen, 2);
                spinner.PlayLoading();
                coaching.Show(CoachingMode.LookAround);
                captions.SyncTo(audio);
                label.Track(camera, target);
                tabs.SelectedId = "t7";
                yield return WaitMs(1100);

                var timers = new Dictionary<string, DisplayedTimer>
                {
                    ["LoadingIcon"] = TimerOf(spinner, "animationScheduledItem"),
                    ["SkeletonTile"] = TimerOf(skeleton, "pulse"),
                    ["LinearProgress"] = TimerOf(progress, "sweep"),
                    ["ScanFrame"] = TimerOf(frame, "pulse"),
                    ["CoachingOverlay"] = TimerOf(coaching, "sway"),
                    ["SpeechInputPanel"] = TimerOf(speech, "animation"),
                    ["CaptionBar"] = TimerOf(captions, "syncTimer"),
                    ["SafeAreaRoot"] = TimerOf(safeArea, "poll"),
                    ["WorldAnchoredLabel"] = TimerOf(label, "trackItem"),
                };
                foreach (var pair in timers)
                {
                    Assert.That(pair.Value, Is.Not.Null, pair.Key + " started");
                    Assert.IsTrue(pair.Value.IsRunning, pair.Key + " still wanted");
                    Assert.IsFalse(pair.Value.IsTicking, pair.Key + " rests while hidden");
                }

                Assert.IsTrue(tabs.HasPendingScroll, "the tab waits for a layout instead of polling");

                screen.style.display = DisplayStyle.Flex;
                yield return WaitFrames(4);
                foreach (var pair in timers)
                {
                    Assert.IsTrue(pair.Value.IsTicking, pair.Key + " runs again when shown");
                }

                Assert.IsFalse(tabs.HasPendingScroll, "scrolled on the first layout");
                Assert.That(tabs.Q<ScrollView>().scrollOffset.x, Is.GreaterThan(0f), "the last tab scrolled into view");
            }
            finally
            {
                Object.Destroy(camera.gameObject);
                Object.Destroy(target.gameObject);
                Object.Destroy(audio.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator IdleControls_ScheduleNothing()
        {
            // An idle speech panel or a still skeleton has nothing to animate, so it starts no timer at all.
            var speech = new SpeechInputPanel { State = SpeechState.Processing };
            var skeleton = new SkeletonTile(SkeletonShape.Row, 1) { Animate = false };
            var column = new VisualElement();
            column.Add(speech);
            column.Add(skeleton);
            yield return Mount(column, 3);

            Assert.That(TimerOf(speech, "animation"), Is.Null, "no meter timer when not listening");
            Assert.That(TimerOf(skeleton, "pulse"), Is.Null, "no pulse timer when not animated");

            speech.State = SpeechState.Listening;
            skeleton.Animate = true;
            yield return WaitFrames(2);
            Assert.IsTrue(TimerOf(speech, "animation").IsTicking, "starts when listening");
            Assert.IsTrue(TimerOf(skeleton, "pulse").IsTicking, "starts when animated");

            var meter = TimerOf(speech, "animation");
            speech.State = SpeechState.Processing;
            Assert.IsFalse(meter.IsRunning, "stops when listening ends");
            Assert.That(TimerOf(speech, "animation"), Is.Null);
        }
    }
}
