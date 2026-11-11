/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ProgressRingTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Value_ClampsAndShowsPercentOrText()
        {
            var ring = new ProgressRing { Size = 120f, Label = "coverage" };
            yield return Mount(ring);

            ring.Value = 0.42f;
            var value = ring.Q<Label>(className: ProgressRing.ValueClass);
            Assert.That(value.text, Is.EqualTo("42%"));
            Assert.That(ring.layout.width, Is.EqualTo(120f));
            Assert.That(ring.Q<Label>(className: ProgressRing.LabelClass).text, Is.EqualTo("coverage"));

            ring.ValueText = "4 of 12";
            Assert.That(value.text, Is.EqualTo("4 of 12"));

            ring.Value = 3f;
            Assert.That(ring.Value, Is.EqualTo(1f));
            ring.Value = float.NaN;
            Assert.That(ring.Value, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator Colours_ComeFromTokens_AndCompleteTurnsSuccess()
        {
            var ring = new ProgressRing { Value = 0.5f };
            yield return Mount(ring);

            Assert.That(ring.FillColor.r, Is.EqualTo(0xA5 / 255f).Within(0.01f), "--surface-accent");
            Assert.That(ring.TrackColor.r, Is.EqualTo(0x3F / 255f).Within(0.01f), "--surface-border");

            ring.Value = 1f;
            yield return WaitFrames(2);
            Assert.IsTrue(ring.ClassListContains(ProgressRing.CompleteClass));
            Assert.That(ring.FillColor.g, Is.EqualTo(0xC5 / 255f).Within(0.01f), "--c-success");
        }

        [UnityTest]
        public IEnumerator ShowCompleteOff_KeepsTheAccentAtOne()
        {
            // A countdown that starts with a full ring keeps the accent.
            var ring = new ProgressRing { ShowComplete = false, Value = 1f, ValueText = "3" };
            yield return Mount(ring);

            Assert.IsFalse(ring.ClassListContains(ProgressRing.CompleteClass));
            Assert.That(ring.FillColor.r, Is.EqualTo(0xA5 / 255f).Within(0.01f), "--surface-accent");

            ring.ShowComplete = true;
            yield return WaitFrames(2);
            Assert.IsTrue(ring.ClassListContains(ProgressRing.CompleteClass), "turning it back on applies at once");
        }
    }

    public class StatusListTests : UIDocumentTestBase
    {
        private static List<StatusStep> Steps() => new()
        {
            new StatusStep("download", "Downloading experience"),
            new StatusStep("locate", "Finding your position"),
            new StatusStep("place", "Placing content"),
        };

        [UnityTest]
        public IEnumerator SetState_UpdatesRowsBarAndRaisesAllDone()
        {
            Root.SetReduceMotion(true);
            var list = new StatusList();
            list.SetSteps(Steps());
            list.style.width = 300;
            var changes = new List<(string, StepState)>();
            var allDone = 0;
            list.StepChanged += (id, state) => changes.Add((id, state));
            list.AllDone += () => allDone++;
            yield return Mount(list);

            Assert.IsTrue(list.SetState("download", StepState.Active, "12 of 40 MB"));
            yield return WaitFrames(2);
            var row = list.StepElement("download");
            Assert.IsTrue(row.ClassListContains("statusList__step--active"));
            Assert.That(row.Q<LoadingIcon>(className: StatusList.SpinnerClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(row.Q<Label>(className: StatusList.DetailClass).text, Is.EqualTo("12 of 40 MB"));

            list.SetState("download", StepState.Done);
            yield return WaitFrames(2);
            var fill = list.Q(className: StatusList.BarFillClass);
            var bar = list.Q(className: StatusList.BarClass);
            Assert.That(fill.layout.width, Is.EqualTo(bar.layout.width / 3f).Within(1f), "one of three done");

            list.SetState("locate", StepState.Done);
            list.SetState("place", StepState.Done);
            list.SetState("place", StepState.Done);

            Assert.That(allDone, Is.EqualTo(1));
            Assert.IsTrue(list.ClassListContains(StatusList.CompleteClass));
            Assert.That(changes.Count, Is.EqualTo(4), "repeating a state does not raise StepChanged");
            Assert.IsFalse(list.SetState("missing", StepState.Done));
        }

        [UnityTest]
        public IEnumerator Failed_MarksRootAndDetail_ExplicitProgressWins()
        {
            var list = new StatusList();
            list.SetSteps(Steps());
            yield return Mount(list);

            list.SetState("locate", StepState.Failed, "No match nearby. Move to the marked spot and try again.");
            list.Progress = 0.8f;
            yield return WaitFrames(2);

            Assert.IsTrue(list.ClassListContains(StatusList.FailedClass));
            var detail = list.StepElement("locate").Q<Label>(className: StatusList.DetailClass);
            Assert.That(detail.resolvedStyle.color.r, Is.EqualTo(0xDC / 255f).Within(0.01f), "--c-danger");
            Assert.That(list.Progress, Is.EqualTo(0.8f));

            list.Progress = -1f;
            Assert.That(list.Progress, Is.EqualTo(0f), "derived again: nothing done");
        }
    }
}
