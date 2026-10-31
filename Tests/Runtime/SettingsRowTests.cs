/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class SwitchTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Click_TogglesAndRaisesEvents()
        {
            var toggle = new Switch("Captions");
            var changes = new List<bool>();
            var changeEvents = 0;
            toggle.ValueChanged += changes.Add;
            toggle.RegisterValueChangedCallback(_ => changeEvents++);
            yield return Mount(toggle);

            Assert.IsFalse(toggle.value);
            Assert.That(toggle.label, Is.EqualTo("Captions"));
            Assert.IsTrue(toggle.ClassListContains(Switch.RootClass));

            Click(toggle);
            Assert.IsTrue(toggle.value);
            Assert.IsTrue(toggle.ClassListContains(Switch.OnClass));

            Click(toggle);
            Assert.That(changes, Is.EqualTo(new[] { true, false }));
            Assert.That(changeEvents, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator SetValueWithoutNotify_UpdatesLookOnly()
        {
            var toggle = new Switch();
            var changes = 0;
            toggle.ValueChanged += _ => changes++;
            yield return Mount(toggle);

            toggle.SetValueWithoutNotify(true);
            Assert.IsTrue(toggle.ClassListContains(Switch.OnClass));
            Assert.That(changes, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Submit_Toggles_AndThumbMovesWithTokens()
        {
            Root.SetReduceMotion(true);
            var toggle = new Switch("Reduce motion");
            yield return Mount(toggle);

            using (var evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = toggle;
                toggle.SendEvent(evt);
            }

            yield return WaitFrames(2);
            Assert.IsTrue(toggle.value);
            var thumb = toggle.Q(className: Switch.ThumbClass);
            Assert.That(thumb.resolvedStyle.translate.x, Is.EqualTo(18f), "FS09_5 travel");
            var track = toggle.Q(className: Switch.TrackClass);
            Assert.That(track.resolvedStyle.backgroundColor.r, Is.EqualTo(0x3D / 255f).Within(0.01f), "--c-primary when on");
        }

        [UnityTest]
        public IEnumerator Disabled_IgnoresClicks()
        {
            var toggle = new Switch();
            yield return Mount(toggle);

            toggle.SetEnabled(false);
            Click(toggle);
            Assert.IsFalse(toggle.value);
        }
    }

    public class SettingsRowTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ChevronRow_ShowsTitleSubtitleAndRaisesClicked()
        {
            var row = new SettingsRow("Language", "English (UK)");
            var clicks = 0;
            row.Clicked += () => clicks++;
            yield return Mount(row);

            Assert.That(row.Q<Label>(className: SettingsRow.TitleClass).text, Is.EqualTo("Language"));
            Assert.IsTrue(row.ClassListContains(SettingsRow.HasSubtitleClass));
            Assert.IsTrue(row.ClassListContains(SettingsRow.ChevronModeClass));
            Assert.That(row.Q(className: SettingsRow.ChevronClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(row.Switch.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(row.layout.height, Is.GreaterThanOrEqualTo(56f), "44 pt target with room for two lines");

            Click(row);
            Assert.That(clicks, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SwitchRow_RowTapTogglesSwitch()
        {
            var row = new SettingsRow("Captions", trailingMode: SettingsRowTrailing.Switch);
            var switched = new List<bool>();
            row.SwitchChanged += switched.Add;
            yield return Mount(row);

            Assert.IsFalse(row.ClassListContains(SettingsRow.HasSubtitleClass));
            Assert.That(row.Switch.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(row.Q(className: SettingsRow.ChevronClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            Click(row);
            Assert.IsTrue(row.SwitchValue);
            Assert.That(switched, Is.EqualTo(new[] { true }));

            row.SwitchValue = false;
            Assert.That(switched, Is.EqualTo(new[] { true }), "setting the value from code does not notify");
        }

        [UnityTest]
        public IEnumerator ValueRow_ShowsValueWithoutChevron()
        {
            var row = new SettingsRow("Text size") { Trailing = SettingsRowTrailing.Value, ValueText = "Large" };
            yield return Mount(row);

            var value = row.Q<Label>(className: SettingsRow.ValueClass);
            Assert.That(value.text, Is.EqualTo("Large"));
            Assert.That(value.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(row.Q(className: SettingsRow.ChevronClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the design shows the value alone");
        }

        [UnityTest]
        public IEnumerator CustomRow_ChildrenGoToTrailingSlot_AndIconShows()
        {
            var row = new SettingsRow("Storage") { Trailing = SettingsRowTrailing.Custom };
            var badge = new NotificationBadge { Count = 3 };
            row.Add(badge);
            row.SetIcon(Background.FromTexture2D(Texture2D.whiteTexture));
            yield return Mount(row);

            Assert.That(badge.hierarchy.parent.hierarchy.parent, Is.SameAs(row.Q(className: SettingsRow.TrailingClass)));
            Assert.IsTrue(row.ClassListContains(SettingsRow.HasIconClass));
            Assert.That(row.Q(className: SettingsRow.IconTileClass).resolvedStyle.width, Is.EqualTo(40f), "40 px icon tile");
            Assert.That(row.Q(className: SettingsRow.IconClass).resolvedStyle.width, Is.EqualTo(20f));
            Assert.That(row.Q(className: SettingsRow.IconTileClass).resolvedStyle.backgroundColor.r, Is.EqualTo(0x30 / 255f).Within(0.01f), "--surface-tile");
        }

        [UnityTest]
        public IEnumerator LightSurface_UsesPageText()
        {
            var row = new SettingsRow("Account");
            row.SetSurface(ThemeSurface.Light);
            yield return Mount(row);

            Assert.That(row.Q<Label>(className: SettingsRow.TitleClass).resolvedStyle.color.r, Is.EqualTo(0x14 / 255f).Within(0.01f), "--c-page-fg");
        }
    }
}
