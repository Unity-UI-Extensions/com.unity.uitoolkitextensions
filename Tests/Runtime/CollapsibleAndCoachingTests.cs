/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class CollapsibleSectionTests : UIDocumentTestBase
    {
        private static void PointerDown(VisualElement target)
        {
            var imgui = new Event { type = EventType.MouseDown, mousePosition = target.worldBound.center, button = 0, clickCount = 1 };
            using var evt = PointerDownEvent.GetPooled(imgui);
            evt.target = target;
            target.SendEvent(evt);
        }

        [UnityTest]
        public IEnumerator Default_KeepsOriginalPointerDownToggle()
        {
            var section = new CollapsibleSection { TitleText = "Camera" };
            var changes = new List<bool>();
            section.OnExpandedChanged += changes.Add;
            yield return Mount(section);

            Assert.IsFalse(section.ClassListContains(CollapsibleSection.CardClass));
            Assert.That(section.Q(className: CollapsibleSection.ChevronClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            PointerDown(section.Header);
            Assert.IsTrue(section.IsExpanded);
            Click(section.Header);
            Assert.IsTrue(section.IsExpanded, "a click does not toggle a second time in pointer-down mode");
            Assert.That(changes, Is.EqualTo(new[] { true }));
        }

        [UnityTest]
        public IEnumerator ClickMode_StaticMode_AndSubmit()
        {
            var section = new CollapsibleSection { TitleText = "Location", ToggleOnPointerDown = false };
            yield return Mount(section);

            PointerDown(section.Header);
            Assert.IsFalse(section.IsExpanded, "pointer down alone does not toggle");
            Click(section.Header);
            Assert.IsTrue(section.IsExpanded);

            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = section.Header;
                section.Header.SendEvent(submit);
            }

            Assert.IsFalse(section.IsExpanded, "submit toggles");

            section.Interactive = false;
            Click(section.Header);
            Assert.IsFalse(section.IsExpanded, "static sections ignore taps");
            Assert.IsTrue(section.ClassListContains(CollapsibleSection.StaticClass));
            section.IsExpanded = true;
            Assert.IsTrue(section.IsExpanded, "the owner still drives it from state");
        }

        [UnityTest]
        public IEnumerator Slots_SubtitleAndChevronRule()
        {
            var section = new CollapsibleSection { TitleText = "Camera", SubtitleText = "REQUIRED" };
            yield return Mount(section);

            var tile = new VisualElement();
            tile.style.width = 40;
            tile.style.height = 40;
            section.SetLeading(tile);
            var badge = new Label("Skipped");
            section.SetTrailing(badge);
            yield return WaitFrames(2);

            Assert.That(tile.hierarchy.parent, Is.SameAs(section.HeaderLeading));
            Assert.That(badge.hierarchy.parent, Is.SameAs(section.HeaderTrailing));
            Assert.IsTrue(section.ClassListContains(CollapsibleSection.HasSubtitleClass));
            Assert.That(section.Q<Label>(className: CollapsibleSection.SubtitleClass).text, Is.EqualTo("REQUIRED"));
            var chevron = section.Q(className: CollapsibleSection.ChevronClass);
            Assert.That(chevron.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "trailing content replaces the chevron");

            section.SetTrailing(null);
            yield return WaitFrames(2);
            Assert.That(chevron.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            section.ShowChevron = false;
            yield return WaitFrames(2);
            Assert.That(chevron.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Card_ReadsTokens()
        {
            Root.SetReduceMotion(true);
            var section = new CollapsibleSection { TitleText = "Microphone", Variant = CollapsibleSectionVariant.Card, BodyText = "Used for speech to text." };
            yield return Mount(section);

            Assert.IsTrue(section.ClassListContains(ThemeTokens.ThemedClass));
            Assert.That(section.resolvedStyle.borderTopLeftRadius, Is.EqualTo(16f), "--radius-card");
            Assert.That(section.resolvedStyle.borderTopWidth, Is.EqualTo(1f));
            Assert.That(section.Header.resolvedStyle.paddingLeft, Is.EqualTo(16f));
            Assert.That(section.Header.resolvedStyle.paddingTop, Is.EqualTo(14f));
            var content = section.Q(className: CollapsibleSection.BodyContentClass);
            Assert.That(content.resolvedStyle.paddingBottom, Is.EqualTo(16f));
            Assert.That(content.resolvedStyle.paddingTop, Is.EqualTo(0f));
        }
    }

    public class CoachingOverlayTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Show_SetsModeTextAndWarnTone()
        {
            Root.SetReduceMotion(true);
            var overlay = new CoachingOverlay();
            yield return Mount(overlay);

            Assert.IsFalse(overlay.IsShown);
            Assert.That(overlay.resolvedStyle.opacity, Is.EqualTo(0f));
            Assert.That(overlay.pickingMode, Is.EqualTo(PickingMode.Ignore));

            overlay.Show(CoachingMode.FindSurface);
            yield return WaitFrames(2);
            Assert.IsTrue(overlay.IsShown);
            Assert.That(overlay.resolvedStyle.opacity, Is.EqualTo(1f));
            Assert.That(overlay.Text, Is.EqualTo(CoachingOverlay.DefaultText(CoachingMode.FindSurface)));
            Assert.IsTrue(overlay.ClassListContains("coachingOverlay--findSurface"));
            Assert.That(overlay.Q(className: CoachingOverlay.GroundClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            overlay.Show(CoachingMode.TooDark, "Zu dunkel");
            Assert.That(overlay.Text, Is.EqualTo("Zu dunkel"));
            Assert.IsTrue(overlay.ClassListContains(CoachingOverlay.WarnClass));
            Assert.IsFalse(overlay.ClassListContains("coachingOverlay--findSurface"));
        }

        [UnityTest]
        public IEnumerator Sway_RunsUnlessReduceMotion()
        {
            var overlay = new CoachingOverlay();
            yield return Mount(overlay);
            overlay.Show(CoachingMode.LookAround);

            var swayed = false;
            for (var i = 0; i < 12 && !swayed; i++)
            {
                yield return WaitMs(200);
                swayed |= overlay.ClassListContains(CoachingOverlay.SwayedClass);
            }

            Assert.IsTrue(swayed);

            Root.SetReduceMotion(true);
            yield return WaitMs(CoachingOverlay.SwayIntervalMs + 100);
            Assert.IsFalse(overlay.IsSwaying);
            Assert.IsFalse(overlay.ClassListContains(CoachingOverlay.SwayedClass));
        }

        [UnityTest]
        public IEnumerator Dismissible_TapHidesAndRaises()
        {
            var overlay = new CoachingOverlay { Dismissible = true };
            var dismissed = 0;
            overlay.Dismissed += () => dismissed++;
            yield return Mount(overlay);

            var cue = overlay.Q(className: CoachingOverlay.CueClass);
            Click(cue);
            Assert.That(dismissed, Is.EqualTo(0), "hidden overlays ignore taps");

            overlay.Show(CoachingMode.MoveSlower);
            Click(cue);
            Assert.That(dismissed, Is.EqualTo(1));
            Assert.IsFalse(overlay.IsShown);
        }
    }

    public class ModalDialogHeaderTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Header_ShowsAboveTitle_AndHidesWhenEmpty()
        {
            Root.SetReduceMotion(true);
            var dialog = new ModalDialog();
            yield return Mount(dialog);

            var disc = new VisualElement();
            disc.style.width = 48;
            disc.style.height = 48;
            dialog.Show(new DialogOptions { Title = "Sign out?", PrimaryText = "Sign out", SecondaryText = "Cancel", Header = disc });
            yield return WaitFrames(2);

            Assert.That(disc.hierarchy.parent, Is.SameAs(dialog.HeaderContent));
            Assert.IsTrue(dialog.ClassListContains(ModalDialog.HasHeaderClass));
            var title = dialog.Q<Label>(className: ModalDialog.TitleClass);
            Assert.That(dialog.HeaderContent.worldBound.yMax, Is.LessThanOrEqualTo(title.worldBound.yMin + 0.5f));

            dialog.Show(new DialogOptions { Title = "Plain" });
            yield return WaitFrames(2);
            Assert.That(dialog.HeaderContent.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(dialog.HeaderContent.childCount, Is.EqualTo(0));
        }
    }

    public class RoundedInputFieldInvalidTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Invalid_TurnsBorderWarn()
        {
            var field = new RoundedInputField();
            yield return Mount(field);

            field.Invalid = true;
            yield return WaitFrames(2);
            Assert.IsTrue(field.ClassListContains(RoundedInputField.InvalidClass));
            Assert.That(field.resolvedStyle.borderTopColor.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "--c-warn");

            field.Invalid = false;
            yield return WaitFrames(2);
            Assert.That(field.resolvedStyle.borderTopColor.r, Is.Not.EqualTo(0xFB / 255f).Within(0.01f));
        }
    }
}
