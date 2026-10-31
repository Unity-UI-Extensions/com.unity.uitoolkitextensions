/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class CaptureComponentTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ShutterButton_TapVersusHoldAndRecording()
        {
            Root.SetReduceMotion(true);
            var shutter = new ShutterButton { Size = 58f, HoldEnabled = true, HoldThresholdMs = 150 };
            var log = new List<string>();
            shutter.Tapped += () => log.Add("tap");
            shutter.Pressed += () => log.Add("pressed");
            shutter.HoldStarted += () => log.Add("start");
            shutter.HoldEnded += released => log.Add(released ? "end" : "cancel");
            yield return Mount(shutter);

            Assert.That(shutter.resolvedStyle.borderTopWidth, Is.EqualTo(3f), "small ring");
            var disc = shutter.Q(className: ShutterButton.DiscClass);
            Assert.That(disc.layout.width, Is.EqualTo(44f), "FS08_1 disc");

            var centre = shutter.worldBound.center;
            SendPointerEvent<PointerDownEvent>(shutter, centre);
            SendPointerEvent<PointerUpEvent>(shutter, centre);
            Assert.That(log, Is.EqualTo(new[] { "tap", "pressed" }));

            log.Clear();
            SendPointerEvent<PointerDownEvent>(shutter, centre);
            yield return WaitMs(300);
            Assert.IsTrue(shutter.IsHolding);
            Assert.IsTrue(shutter.ClassListContains(ShutterButton.HoldingClass));

            shutter.Recording = true;
            shutter.Progress = 0.4f;
            yield return WaitFrames(2);
            Assert.That(disc.layout.width, Is.EqualTo(26f), "recording square");
            Assert.That(shutter.resolvedStyle.borderTopColor.r, Is.EqualTo(0xC0 / 255f).Within(0.01f), "--c-record ring");
            Assert.That(disc.resolvedStyle.backgroundColor.r, Is.EqualTo(0xC0 / 255f).Within(0.01f));

            // Sliding off and releasing still ends the hold: the shutter keeps the pointer.
            SendPointerEvent<PointerUpEvent>(shutter, centre + new Vector2(200f, 0f));
            Assert.That(log, Is.EqualTo(new[] { "start", "cancel" }));
            Assert.IsFalse(shutter.IsHolding);

            shutter.Recording = false;
            yield return WaitFrames(2);
            Assert.That(disc.layout.width, Is.EqualTo(44f));
            Assert.That(disc.resolvedStyle.backgroundImage.texture, Is.Not.Null, "gradient back");
        }

        [UnityTest]
        public IEnumerator ShutterButton_WithoutHold_ClickStillTaps()
        {
            var shutter = new ShutterButton();
            var taps = 0;
            shutter.Tapped += () => taps++;
            yield return Mount(shutter);

            Click(shutter);
            Assert.That(taps, Is.EqualTo(1));
            Assert.That(shutter.layout.width, Is.EqualTo(84f));
        }

        [UnityTest]
        public IEnumerator OrnamentBar_SlotSurvivesRebuildMessageAndActive()
        {
            Root.SetReduceMotion(true);
            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 844;
            var bar = new OrnamentBar { ShowLabels = false, Stretch = true, EdgeInset = 34f };
            host.Add(bar);
            var items = new List<OrnamentItem> { new("explore", "Explore"), new("map", "Map"), new("captions", "Captions"), new("photo", "Photo") };
            bar.SetItems(items);
            var shutter = new ShutterButton { Size = 58f, HoldEnabled = true, HoldThresholdMs = 100 };
            var detaches = 0;
            shutter.RegisterCallback<DetachFromPanelEvent>(_ => detaches++);
            bar.SetCentreElement(shutter);
            yield return Mount(host);

            var slot = bar.Q(className: OrnamentBar.SlotClass);
            Assert.That(bar.Bar.IndexOf(slot), Is.EqualTo(3), "message, two items, then the slot");
            Assert.That(bar.Bar.layout.width, Is.EqualTo(342f).Within(0.5f), "FS08_1: 24 px in from each side");
            Assert.That(bar.Bar.layout.height, Is.EqualTo(70f).Within(0.5f));
            Assert.That(shutter.worldBound.center.x, Is.EqualTo(195f).Within(1f), "shutter centred");

            SendPointerEvent<PointerDownEvent>(shutter, shutter.worldBound.center);
            yield return WaitMs(250);
            Assert.IsTrue(shutter.IsHolding);

            bar.SetItems(items);
            bar.Message = "Release to stop · 30 s max";
            yield return WaitFrames(2);
            Assert.That(detaches, Is.EqualTo(0), "the slot never leaves the bar");
            Assert.IsTrue(shutter.IsHolding, "a held press survives a rebuild");
            Assert.That(bar.ItemElement(0).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "items hidden in message mode");
            var message = bar.Q<Label>(className: OrnamentBar.MessageClass);
            Assert.That(message.text, Is.EqualTo("RELEASE TO STOP · 30 S MAX"));
            Assert.That(message.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(shutter.worldBound.center.x, Is.EqualTo(195f).Within(1f), "shutter stays centred");
            SendPointerEvent<PointerUpEvent>(shutter, shutter.worldBound.center);

            bar.Message = null;
            bar.SetItemActive("map", true);
            yield return WaitFrames(2);
            var map = bar.ItemElement(bar.IndexOf("map"));
            Assert.IsTrue(map.ClassListContains(OrnamentBar.ActiveItemClass));
            Assert.IsTrue(map.ClassListContains(OrnamentBar.PrimaryItemClass), "active items take the filled pill");
            Assert.That(map.resolvedStyle.backgroundImage.texture, Is.Not.Null);
            Assert.That(map.Q<Label>().resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "label shown");

            bar.SetItemActive("map", false);
            yield return WaitFrames(2);
            Assert.That(map.resolvedStyle.backgroundImage.texture, Is.Null);
        }

        [UnityTest]
        public IEnumerator OrnamentBar_StretchCapsAtMaxLengthAndCentres_OnATablet()
        {
            var host = new VisualElement();
            host.style.width = 820;
            host.style.height = 1180;
            var bar = new OrnamentBar { ShowLabels = false, Stretch = true, Anchor = OrnamentAnchor.Bottom };
            bar.SetItems(new List<OrnamentItem> { new("explore", "Explore"), new("map", "Map"), new("captions", "Captions"), new("photo", "Photo") });
            host.Add(bar);
            yield return Mount(host, 3);

            var rect = bar.Bar.worldBound;
            Assert.That(rect.width, Is.EqualTo(480f).Within(0.5f), "capped at MaxLength");
            Assert.That(rect.center.x - host.worldBound.x, Is.EqualTo(410f).Within(0.5f), "centred on the tablet");

            host.style.width = 390;
            yield return WaitFrames(3);
            Assert.That(bar.Bar.worldBound.width, Is.EqualTo(342f).Within(0.5f), "24 px in from each side on a phone");

            host.style.width = 1180;
            host.style.height = 820;
            bar.Anchor = OrnamentAnchor.Right;
            yield return WaitFrames(3);
            rect = bar.Bar.worldBound;
            Assert.That(rect.height, Is.EqualTo(480f).Within(0.5f), "capped on the right edge too");
            Assert.That(rect.center.y - host.worldBound.y, Is.EqualTo(410f).Within(0.5f), "centred vertically");
        }

        [UnityTest]
        public IEnumerator OrnamentBar_PrimaryIconOnly_RoundDiscLabelKeptAsName()
        {
            Root.SetReduceMotion(true);
            var host = new VisualElement();
            host.style.width = 390;
            host.style.height = 844;
            var bar = new OrnamentBar { ShowLabels = false, Stretch = true, PrimaryIconOnly = true };
            var white = Background.FromTexture2D(Texture2D.whiteTexture);
            bar.SetItems(new List<OrnamentItem>
            {
                new("explore", "Explore", white), new("map", "Map", white), new("add", "Add content", white),
                new("captions", "Captions", white), new("photo", "Photo", white),
            }, primary: 2);
            bar.SetItemActive("map", true);
            host.Add(bar);
            yield return Mount(host, 4);

            var add = bar.ItemElement(2);
            Assert.That(add.layout.width, Is.EqualTo(48f), "in line with the 48 px bar icons");
            Assert.That(add.layout.height, Is.EqualTo(48f));
            Assert.That(add.resolvedStyle.borderTopLeftRadius, Is.EqualTo(24f), "a round disc");
            Assert.That(add.resolvedStyle.backgroundImage.texture, Is.Not.Null, "primary gradient");
            Assert.That(add.Q(className: OrnamentBar.IconClass).layout.width, Is.EqualTo(28f), "large icon");
            var label = add.Q<Label>(className: OrnamentBar.LabelClass);
            Assert.That(label.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "no visible text");
            Assert.That(label.text, Is.EqualTo("Add content"), "the label is still the item's text");
            Assert.That(add.tooltip, Is.EqualTo("Add content"), "and its tooltip");

            var map = bar.ItemElement(1);
            Assert.That(map.Q<Label>(className: OrnamentBar.LabelClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "active items keep their labelled pill");
            Assert.IsFalse(map.ClassListContains(OrnamentBar.PrimaryActionClass));

            bar.PrimaryIconOnly = false;
            yield return WaitFrames(3);
            Assert.That(label.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "back to the labelled pill");
            Assert.That(add.layout.width, Is.GreaterThan(48f));
        }

        [UnityTest]
        public IEnumerator StatusChip_RecordingAndFilledWarn()
        {
            var rec = new StatusChip("REC 00:12", StatusState.Recording);
            var offline = new StatusChip("Offline · showing saved content", StatusState.Warn) { Filled = true };
            offline.SetIcon(Background.FromTexture2D(Texture2D.whiteTexture));
            var column = new VisualElement();
            column.Add(rec);
            column.Add(offline);
            yield return Mount(column);

            Assert.That(rec.resolvedStyle.backgroundColor.r, Is.EqualTo(0xC0 / 255f).Within(0.01f), "--c-record fill");
            Assert.That(rec.Q(className: StatusChip.IndicatorClass).layout.width, Is.EqualTo(10f));
            Assert.That(rec.Q<Label>().resolvedStyle.color.g, Is.EqualTo(1f).Within(0.01f), "white text");

            Assert.That(offline.resolvedStyle.backgroundColor.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "--c-warn fill");
            Assert.That(offline.Q<Label>().resolvedStyle.color.r, Is.EqualTo(0x16 / 255f).Within(0.01f), "--c-on-warn text");
            Assert.That(offline.Q(className: StatusChip.IconClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(offline.Q(className: StatusChip.IndicatorClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "icon replaces the dot");
        }

        [UnityTest]
        public IEnumerator Switch_TrackHoldsItsSizeUnderAThemeFlexBasis()
        {
#if UNITY_EDITOR
            Root.styleSheets.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.uitoolkitextensions/Tests/Runtime/HostileBaseField.uss"));
#endif
            var row = new SettingsRow { TitleText = "Captions", SubtitleText = "Always show what a guide says", Trailing = SettingsRowTrailing.Switch };
            row.style.width = 350;
            yield return Mount(row);

            var track = row.Q(className: Switch.TrackClass);
            Assert.That(track.layout.width, Is.EqualTo(46f), "not squeezed to its padding");
        }

        [UnityTest]
        public IEnumerator ToastHost_ThumbnailAndIconAction()
        {
            Root.SetReduceMotion(true);
            var host = new ToastHost();
            host.style.width = 390;
            host.style.height = 844;
            yield return Mount(host);

            var shared = 0;
            var white = Background.FromTexture2D(Texture2D.whiteTexture);
            var handle = host.Show(new ToastOptions
            {
                Message = "Saved to App Captures",
                Detail = "Photo · 12 MB · with the tour watermark",
                Thumbnail = white,
                Icon = white,
                ActionText = "Share",
                ActionIcon = white,
                OnAction = () => shared++,
                Tone = ToastTone.Success,
            });
            yield return WaitFrames(3);

            var toast = handle.Element;
            Assert.IsTrue(toast.ClassListContains(ToastHost.HasThumbnailClass));
            Assert.IsFalse(toast.ClassListContains(ToastHost.HasIconClass), "the thumbnail replaces the icon disc");
            Assert.That(toast.Q(className: ToastHost.ThumbnailClass).layout.width, Is.EqualTo(56f));
            var action = toast.Q<Button>(className: ToastHost.ActionClass);
            Assert.That(action.layout.width, Is.EqualTo(48f));
            Assert.That(action.text, Is.Empty);
            Assert.That(action.tooltip, Is.EqualTo("Share"));
            Assert.That(action.Q(className: ToastHost.ActionIconClass), Is.Not.Null);

            Click(action);
            Assert.That(shared, Is.EqualTo(1));
            Assert.IsTrue(handle.IsDismissed);
        }
    }
}
