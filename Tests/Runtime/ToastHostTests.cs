/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ToastHostTests : UIDocumentTestBase
    {
        private ToastHost host;
        private readonly List<ToastDismissReason> reasons = new();

        private IEnumerator MountHost(bool reduceMotion = true)
        {
            Root.SetReduceMotion(reduceMotion);
            host = new ToastHost();
            host.ToastDismissed += (_, reason) => reasons.Add(reason);
            reasons.Clear();
            yield return Mount(host);
        }

        private int ToastElements() => host.Query(className: ToastHost.ToastClass).ToList().Count;

        [UnityTest]
        public IEnumerator Show_AddsToast_AndTimesOut()
        {
            yield return MountHost();
            var shown = 0;
            host.ToastShown += _ => shown++;

            var handle = host.Show(new ToastOptions { Message = "Saved as a draft", DurationMs = 300 });
            yield return WaitFrames(2);

            Assert.That(shown, Is.EqualTo(1));
            Assert.IsTrue(handle.IsVisible);
            Assert.IsTrue(handle.Element.ClassListContains(ToastHost.ShownClass));
            Assert.That(handle.Element.Q<Label>(className: ToastHost.MessageClass).text, Is.EqualTo("Saved as a draft"));
            Assert.That(host.pickingMode, Is.EqualTo(PickingMode.Ignore), "the band passes taps through");

            yield return WaitMs(500);
            Assert.IsTrue(handle.IsDismissed);
            Assert.That(reasons, Is.EqualTo(new[] { ToastDismissReason.Timeout }));
            Assert.That(ToastElements(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Queue_HoldsToastsBeyondMaxVisible()
        {
            yield return MountHost();
            host.MaxVisible = 2;

            var first = host.Show("One", durationMs: 10000);
            var second = host.Show("Two", durationMs: 10000);
            var third = host.Show("Three", durationMs: 10000);
            yield return WaitFrames(2);

            Assert.That(host.Visible.Count, Is.EqualTo(2));
            Assert.That(host.QueuedCount, Is.EqualTo(1));
            Assert.IsNull(third.Element);

            first.Dismiss();
            yield return WaitFrames(2);

            Assert.That(host.QueuedCount, Is.EqualTo(0));
            Assert.IsTrue(third.IsVisible);
            Assert.That(host.Visible, Is.EqualTo(new[] { second, third }));
        }

        [UnityTest]
        public IEnumerator Action_InvokesAndDismisses()
        {
            yield return MountHost();
            var undone = 0;
            var handle = host.ShowUndo("Space deleted", () => undone++);
            yield return WaitFrames(2);

            Assert.IsTrue(handle.Element.ClassListContains(ToastHost.HasActionClass));
            Assert.IsTrue(handle.Element.ClassListContains(ToastHost.UndoClass));
            Assert.That(handle.Options.DurationMs, Is.EqualTo(ToastHost.UndoDurationMs));

            var action = handle.Element.Q<Button>(className: ToastHost.ActionClass);
            Assert.That(action.text, Is.EqualTo("Undo"));
            Click(action);

            Assert.That(undone, Is.EqualTo(1));
            Assert.That(reasons, Is.EqualTo(new[] { ToastDismissReason.Action }));
        }

        [UnityTest]
        public IEnumerator DismissAll_ClearsVisibleAndQueued()
        {
            yield return MountHost();
            host.MaxVisible = 1;
            var a = host.Show("A");
            var b = host.Show("B");
            var dismissedB = new List<ToastDismissReason>();
            b.Dismissed += dismissedB.Add;

            host.DismissAll();
            yield return WaitFrames(1);

            Assert.IsTrue(a.IsDismissed);
            Assert.IsTrue(b.IsDismissed);
            Assert.That(dismissedB, Is.EqualTo(new[] { ToastDismissReason.Programmatic }));
            Assert.That(host.Visible.Count, Is.EqualTo(0));
            Assert.That(host.QueuedCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Anchor_BottomStacksNewestLast_AndTonesApply()
        {
            yield return MountHost();
            host.Anchor = ToastAnchor.Bottom;
            host.Show(new ToastOptions { Message = "first", DurationMs = 10000 });
            var error = host.Show(new ToastOptions { Message = "Upload failed", Tone = ToastTone.Error, DurationMs = 10000 });
            yield return WaitFrames(2);

            Assert.IsTrue(host.ClassListContains(ToastHost.BottomClass));
            Assert.That(host.IndexOf(error.Element), Is.EqualTo(1), "newest at the bottom edge");
            Assert.IsTrue(error.Element.ClassListContains(ToastHost.ErrorClass));
            var card = error.Element.Q(className: ToastHost.CardClass);
            Assert.That(card.resolvedStyle.borderLeftWidth, Is.EqualTo(3f), "3 px tone edge, as on FS03_6");
            Assert.That(card.resolvedStyle.borderLeftColor, Is.EqualTo((Color)new Color32(0xDC, 0x26, 0x26, 0xFF)).Using(ColorEqualityComparer.Instance), "--c-danger");
        }

        [UnityTest]
        public IEnumerator Animated_ToastFadesInThenOut()
        {
            yield return MountHost(reduceMotion: false);
            var handle = host.Show("Hello", durationMs: 10000);
            yield return WaitMs(ThemeTokens.Motion.BaseMs + 100);
            Assert.That(handle.Element.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.01f));

            var element = handle.Element;
            handle.Dismiss();
            yield return WaitFrames(1);
            Assert.That(element.parent, Is.SameAs(host), "still fading out");
            yield return WaitMs(ThemeTokens.Motion.BaseMs + 150);
            Assert.That(element.parent, Is.Null);
        }
    }

    public class StatusChipTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Chip_StatesSetClassesAndSpinner()
        {
            var chip = new StatusChip("Uploading", StatusState.Busy);
            yield return Mount(chip);

            Assert.IsTrue(chip.ClassListContains(StatusChip.BusyClass));
            var spinner = chip.Q<LoadingIcon>(className: StatusChip.SpinnerClass);
            Assert.That(spinner.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.IsTrue(spinner.ClassListContains(LoadingIcon.AnimatingClass));

            chip.State = StatusState.Fail;
            yield return WaitFrames(2);
            Assert.IsFalse(chip.ClassListContains(StatusChip.BusyClass));
            Assert.IsTrue(chip.ClassListContains(StatusChip.FailClass));
            Assert.That(spinner.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(chip.resolvedStyle.borderTopColor, Is.EqualTo((Color)new Color32(0xDC, 0x26, 0x26, 0xFF)).Using(ColorEqualityComparer.Instance), "--c-danger");
        }

        [UnityTest]
        public IEnumerator Host_ShowUpdateResolve_ById()
        {
            Root.SetReduceMotion(true);
            var host = new StatusChipHost { LingerMs = 200 };
            var resolved = new List<string>();
            host.StatusResolved += resolved.Add;
            yield return Mount(host);

            var chip = host.ShowStatus("upload", "Uploading 1 of 3", StatusState.Busy);
            host.ShowStatus("network", "Offline", StatusState.Warn);
            Assert.That(host.childCount, Is.EqualTo(2));

            Assert.That(host.ShowStatus("upload", "Uploading 2 of 3", StatusState.Busy), Is.SameAs(chip), "same id updates in place");
            Assert.That(chip.Text, Is.EqualTo("Uploading 2 of 3"));

            Assert.IsTrue(host.Resolve("upload", StatusState.Ok, "Uploaded"));
            Assert.That(chip.State, Is.EqualTo(StatusState.Ok));
            Assert.That(resolved, Is.Empty, "lingers on the final state first");
            yield return WaitMs(350);
            Assert.That(resolved, Is.EqualTo(new[] { "upload" }));
            Assert.That(chip.parent, Is.Null);

            Assert.IsTrue(host.Resolve("network"));
            Assert.That(resolved, Is.EqualTo(new[] { "upload", "network" }), "no final state removes at once");
            Assert.IsFalse(host.Resolve("missing"));
        }

        [UnityTest]
        public IEnumerator Host_ClearStatuses_CancelsPendingRemovalsAndAllowsReuse()
        {
            Root.SetReduceMotion(true);
            var host = new StatusChipHost { LingerMs = 200 };
            var resolved = new List<string>();
            host.StatusResolved += resolved.Add;
            yield return Mount(host);

            host.ShowStatus("sync", "Syncing", StatusState.Busy);
            host.ShowStatus("network", "Offline", StatusState.Warn);
            host.Resolve("sync", StatusState.Ok, "Synced");
            host.ClearStatuses();
            host.ClearStatuses();

            Assert.That(host.Ids, Is.Empty);
            Assert.That(resolved, Is.EquivalentTo(new[] { "sync", "network" }));
            var replacement = host.ShowStatus("sync", "Syncing again", StatusState.Busy);
            yield return WaitMs(350);

            Assert.That(host.TryGetChip("sync", out var chip), Is.True);
            Assert.That(chip, Is.SameAs(replacement));
            Assert.That(resolved.Count, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Host_ShowDuringLinger_CancelsRemoval()
        {
            var host = new StatusChipHost { LingerMs = 200 };
            yield return Mount(host);

            host.ShowStatus("sync", "Syncing", StatusState.Busy);
            host.Resolve("sync", StatusState.Fail, "Sync failed");
            host.ShowStatus("sync", "Retrying", StatusState.Busy);
            yield return WaitMs(350);

            Assert.IsTrue(host.TryGetChip("sync", out var chip));
            Assert.That(chip.State, Is.EqualTo(StatusState.Busy));
        }
    }
}
