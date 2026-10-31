/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class EntryGateTests : UIDocumentTestBase
    {
        private static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;

        private static Background Image() => Background.FromTexture2D(Texture2D.whiteTexture);

        private static VisualElement BackgroundOf(PillButton button) => button.Q(className: PillButton.BackgroundClass);

        [UnityTest]
        public IEnumerator CouldNotLoad_MessageCardWithThreeActions()
        {
            var gate = new EntryGate
            {
                Backdrop = EntryGateBackdrop.Deep,
                IconTone = EntryGateIconTone.Warn,
                TitleText = "Could not load Sample Museum Tour",
                BodyText = "The phone is offline and only part of this tour is saved.",
                StartText = "Retry",
                SecondaryText = "Use the saved copy (7 of 12 stops)",
                LinkText = "Go to the home screen",
            };
            gate.SetLogo(Image());
            gate.SetIcon(Image());
            var events = new List<string>();
            gate.Started += () => events.Add("start");
            gate.SecondaryClicked += () => events.Add("secondary");
            gate.LinkClicked += () => events.Add("link");
            yield return Mount(gate, 3);

            Assert.IsTrue(gate.ClassListContains(EntryGate.MessageModifierClass));
            Assert.IsTrue(gate.ClassListContains(EntryGate.CardClass));
            var fill = gate.resolvedStyle.backgroundImage.texture;
            Assert.That(fill, Is.Not.Null, "gradient backdrop");
            Assert.That(fill.GetPixel(0, 255).r, Is.EqualTo(Hex("#18181B").r).Within(0.01f), "deep ground starts at --c-bg");

            var panel = gate.Q(className: EntryGate.PanelClass);
            Assert.That(panel.resolvedStyle.backgroundColor.a, Is.EqualTo(0.86f).Within(0.01f), "--c-panel card");
            Assert.That(gate.Q(className: EntryGate.ActionsClass).hierarchy.parent, Is.SameAs(panel), "message actions sit in the panel");
            Assert.That(gate.Q(className: EntryGate.IconClass).resolvedStyle.backgroundColor.r, Is.EqualTo(Hex("#FBBF24").r).Within(0.01f));
            Assert.That(gate.Q(className: EntryGate.HeroClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(panel.Q<Label>(className: EntryGate.TitleClass).text, Is.EqualTo("Could not load Sample Museum Tour"));
            Assert.That(gate.Q(className: EntryGate.FooterClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no footer content");

            Click(BackgroundOf(gate.StartButton));
            Click(BackgroundOf(gate.SecondaryButton));
            Click(gate.Q<Label>(className: EntryGate.LinkClass));
            Assert.That(events, Is.EqualTo(new[] { "start", "secondary", "link" }));
        }

        [UnityTest]
        public IEnumerator Entry_HeroLayout_ActionsAtBottom_StartWaitsUntilEnabled()
        {
            var gate = new EntryGate
            {
                Layout = EntryGateLayout.Hero,
                Backdrop = EntryGateBackdrop.Deep,
                PanelCard = false,
                Eyebrow = "OPENING EXPERIENCE",
                TitleText = "Sample Museum Tour",
                Subtitle = "12 stops · about 25 min",
                StartText = "Start the tour",
                StartEnabled = false,
                CancelText = "Cancel",
                FooterEndText = "STEP 2 OF 2",
            };
            gate.SetLogo(Image());
            gate.SetOrgLogo(Image());
            var status = new StatusList();
            gate.Add(status);
            var started = 0;
            var skipped = 0;
            gate.Started += () => started++;
            gate.Skipped += () => skipped++;
            yield return Mount(gate, 3);

            var hero = gate.Q(className: EntryGate.HeroClass);
            Assert.That(hero.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(hero.resolvedStyle.backgroundImage.texture, Is.Not.Null, "primary gradient hero");
            Assert.That(hero.Q<Label>(className: EntryGate.TitleClass).text, Is.EqualTo("Sample Museum Tour"));
            Assert.That(gate.Q(className: EntryGate.LogoClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "the logo moves into the hero tile");
            Assert.That(status.hierarchy.parent, Is.SameAs(gate.Q(className: EntryGate.ContentClass)));

            var actions = gate.Q(className: EntryGate.ActionsClass);
            var footer = gate.Q(className: EntryGate.FooterClass);
            Assert.That(actions.hierarchy.parent, Is.SameAs(gate), "hero actions sit at the bottom of the screen");
            Assert.That(actions.worldBound.yMax, Is.LessThanOrEqualTo(footer.worldBound.yMin + 0.5f));
            Assert.That(footer.worldBound.yMax, Is.GreaterThan(gate.worldBound.height * 0.8f), "footer at the bottom");

            Click(BackgroundOf(gate.StartButton));
            Assert.That(started, Is.EqualTo(0), "disabled while loading");
            gate.StartEnabled = true;
            Click(BackgroundOf(gate.StartButton));
            Assert.That(started, Is.EqualTo(1));

            Click(gate.Q<Label>(className: EntryGate.CancelClass));
            Assert.That(skipped, Is.EqualTo(1));
            Assert.That(gate.Q<Label>(className: EntryGate.FooterEndClass).text, Is.EqualTo("STEP 2 OF 2"));
        }

        [UnityTest]
        public IEnumerator CameraDenied_PlainPanel_RequirementsAndConsent()
        {
            var gate = new EntryGate
            {
                PanelCard = false,
                IconTone = EntryGateIconTone.WarnOutline,
                TitleText = "The camera is off for this app",
                BodyText = "This experience places content in the room around you, so it needs the camera.",
                StartText = "Allow the camera",
            };
            gate.SetIcon(Image());
            gate.SetRequirements(new[] { new EntryRequirement("Camera", Image()), new EntryRequirement("Motion") });
            gate.SetConsentLinks(new Dictionary<string, string> { ["privacy"] = "Privacy notice", ["terms"] = "Terms" });
            var consents = new List<string>();
            gate.ConsentLinkClicked += consents.Add;
            yield return Mount(gate, 3);

            Assert.That(gate.resolvedStyle.backgroundImage.texture.GetPixel(0, 255).b, Is.EqualTo(Hex("#202024").b).Within(0.01f), "deep ground starts at --c-navy");
            Assert.That(gate.Q(className: EntryGate.PanelClass).resolvedStyle.backgroundColor.a, Is.EqualTo(0f), "no card");
            var icon = gate.Q(className: EntryGate.IconClass);
            Assert.That(icon.layout.width, Is.EqualTo(64f));
            Assert.That(icon.resolvedStyle.borderTopColor.r, Is.EqualTo(Hex("#FBBF24").r).Within(0.01f));

            var rows = gate.Query(className: EntryGate.RequirementClass).ToList();
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[1].Q(className: EntryGate.RequirementIconClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            var links = gate.Query<Label>(className: EntryGate.ConsentLinkClass).ToList();
            Click(links[1]);
            Assert.That(consents, Is.EqualTo(new[] { "terms" }));
            Assert.That(gate.SecondaryButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }
    }
}
