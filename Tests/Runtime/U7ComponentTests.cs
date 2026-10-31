/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class MapComponentTests : UIDocumentTestBase
    {
        private static List<MapStop> Fs09Stops() => new()
        {
            new("s1", "1", new Vector2(60f / 350f, 250f / 300f), MapStopState.Visited),
            new("s2", "2", new Vector2(100f / 350f, 180f / 300f), MapStopState.Visited),
            new("s3", "3", new Vector2(150f / 350f, 120f / 300f), MapStopState.Visited),
            new("s4", "4", new Vector2(220f / 350f, 90f / 300f), MapStopState.Visited),
            new("s5", "5", new Vector2(290f / 350f, 60f / 300f), MapStopState.Next),
            new("s6", "6", new Vector2(260f / 350f, 200f / 300f)),
        };

        [UnityTest]
        public IEnumerator PanZoomMapView_FitsPinsRouteTapAndFocus()
        {
            Root.SetReduceMotion(true);
            var map = new PanZoomMapView { ShowControls = true };
            map.style.width = 350;
            map.style.height = 300;
            var texture = new Texture2D(700, 600);
            map.SetImage(Background.FromTexture2D(texture));
            map.SetStops(Fs09Stops());
            map.SetRoute(new[] { "s1", "s2", "s3", "s4", "s5" });
            map.SetYouAreHere(new Vector2(125f / 350f, 150f / 300f), 30f);
            var tapped = new List<string>();
            map.StopTapped += tapped.Add;
            yield return Mount(map);

            // The 700 x 600 image fits the 350 x 300 view exactly at zoom 1.
            var pin5 = map.PinElement("s5");
            Assert.That(pin5.layout.width, Is.EqualTo(26f));
            var local5 = map.NormalisedToLocal(new Vector2(290f / 350f, 60f / 300f));
            Assert.That(local5.x, Is.EqualTo(290f).Within(0.5f));
            Assert.That(local5.y, Is.EqualTo(60f).Within(0.5f));
            Assert.That(pin5.resolvedStyle.translate.x, Is.EqualTo(277f).Within(0.5f), "pins centre on their point");
            Assert.IsTrue(map.PinElement("s1").ClassListContains(PanZoomMapView.VisitedPinClass));
            Assert.That(map.PinElement("s1").resolvedStyle.backgroundColor.b, Is.EqualTo(0x3A / 255f).Within(0.01f), "--c-navy-2 fill");
            Assert.IsTrue(pin5.ClassListContains(PanZoomMapView.NextPinClass));

            // A tap near a pin reports it; a tap in empty space does not.
            var origin = map.worldBound.position;
            SendPointerEvent<PointerDownEvent>(map, origin + new Vector2(293f, 62f));
            SendPointerEvent<PointerUpEvent>(map, origin + new Vector2(293f, 62f));
            SendPointerEvent<PointerDownEvent>(map, origin + new Vector2(20f, 20f));
            SendPointerEvent<PointerUpEvent>(map, origin + new Vector2(20f, 20f));
            Assert.That(tapped, Is.EqualTo(new[] { "s5" }));

            map.FocusOn("s6");
            yield return WaitFrames(2);
            Assert.That(map.Zoom, Is.EqualTo(2f));
            Assert.That(map.SelectedId, Is.EqualTo("s6"));
            Assert.IsTrue(map.PinElement("s6").ClassListContains(PanZoomMapView.SelectedPinClass));
            var local6 = map.NormalisedToLocal(map.Stops[5].Position);
            Assert.That(local6.x, Is.EqualTo(175f).Within(0.5f), "focused stop centred");
            Assert.That(local6.y, Is.EqualTo(150f).Within(0.5f));

            // A drag pans; a stop no longer under the pointer is not tapped.
            var start = origin + new Vector2(175f, 150f);
            SendPointerEvent<PointerDownEvent>(map, start);
            SendPointerEvent<PointerMoveEvent>(map, start + new Vector2(-40f, 0f));
            SendPointerEvent<PointerUpEvent>(map, start + new Vector2(-40f, 0f));
            Assert.That(map.NormalisedToLocal(map.Stops[5].Position).x, Is.LessThan(175f));
            Assert.That(tapped.Count, Is.EqualTo(1), "a drag is not a tap");

            map.Recentre();
            yield return WaitFrames(2);
            var you = map.NormalisedToLocal(map.YouAreHere);
            Assert.That(you.x, Is.EqualTo(175f).Within(0.5f), "recentre on you-are-here");

            map.ZoomBy(0.1f);
            Assert.That(map.Zoom, Is.EqualTo(1f), "clamped to min zoom");
            Object.Destroy(texture);
        }

        [UnityTest]
        public IEnumerator MapStopCard_LayoutAndEvents()
        {
            var card = new MapStopCard("5", "Turbine cutaway", "24 m · 3 min · not visited");
            card.style.width = 350;
            var log = new List<string>();
            card.ActionClicked += () => log.Add("go");
            card.Tapped += () => log.Add("card");
            yield return Mount(card);

            Assert.That(card.Q(className: MapStopCard.NumberClass).layout.width, Is.EqualTo(40f));
            Assert.That(card.ActionButton.layout.height, Is.EqualTo(44f));
            Assert.That(card.ActionButton.Text, Is.EqualTo("Go"));
            Assert.That(card.resolvedStyle.borderTopLeftRadius, Is.EqualTo(18f));

            Click(card);
            Assert.That(log, Is.EqualTo(new[] { "card" }));
        }

        [UnityTest]
        public IEnumerator TrailStrip_StatesTapAndCompact()
        {
            var strip = new TrailStrip();
            strip.style.width = 360;
            var stops = new List<TrailStop>();
            for (var i = 0; i < 8; i++)
            {
                stops.Add(new TrailStop("t" + i, "Stop " + (i + 1)));
            }

            strip.SetStops(stops, current: 3);
            var tapped = new List<string>();
            strip.StopTapped += tapped.Add;
            yield return Mount(strip);

            Assert.IsFalse(strip.IsCompact, "8 stops fit 360 px");
            Assert.IsTrue(strip.StopElement(2).ClassListContains(TrailStrip.DoneStopClass));
            Assert.IsTrue(strip.StopElement(3).ClassListContains(TrailStrip.CurrentStopClass));
            Assert.IsTrue(strip.StopElement(4).ClassListContains(TrailStrip.PendingStopClass));
            Assert.That(strip.StopElement(3).layout.width, Is.EqualTo(28f));
            Click(strip.StopElement(6));
            Assert.That(tapped, Is.EqualTo(new[] { "t6" }));

            strip.style.width = 200;
            yield return WaitFrames(3);
            Assert.IsTrue(strip.IsCompact);
            Assert.That(strip.Q<Label>(className: TrailStrip.CompactLabelClass).text, Is.EqualTo("4 of 8"));
            Assert.That(strip.Q<LinearProgress>().Value, Is.EqualTo(0.5f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator WayfinderChips_SortedCappedAndRotated()
        {
            var chips = new WayfinderChips { Max = 2 };
            chips.style.width = 360;
            chips.SetItems(new List<WayfinderChipItem>
            {
                new("far", "Fuel nozzle bench", 30f, 0f),
                new("near", "Turbine cutaway", 24f, -20f),
                new("farther", "Control room", 80f, 90f),
            });
            var tapped = new List<string>();
            chips.ItemTapped += tapped.Add;
            yield return Mount(chips);

            Assert.That(chips.Items.Count, Is.EqualTo(2));
            Assert.That(chips.Items[0].Id, Is.EqualTo("near"));
            var chip = chips.ChipElement("near");
            Assert.That(chip.Q<Label>(className: WayfinderChips.DistanceClass).text, Is.EqualTo("24 m"));
            Assert.That(chip.Q(className: WayfinderChips.ArrowClass).resolvedStyle.rotate.angle.value, Is.EqualTo(-20f).Within(0.01f));
            Assert.That(chip.layout.height, Is.EqualTo(40f));

            chips.UpdateItem("near", 12f, 45f);
            Assert.That(chip.Q<Label>(className: WayfinderChips.DistanceClass).text, Is.EqualTo("12 m"));
            Click(chip);
            Assert.That(tapped, Is.EqualTo(new[] { "near" }));
            Assert.That(chips.ChipElement("farther"), Is.Null, "capped at Max");
        }
    }
}
