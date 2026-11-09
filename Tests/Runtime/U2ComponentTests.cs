/// Credit SimonDarksideJ

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class WorldAnchoredLabelTests : UIDocumentTestBase
    {
        private VisualElement layer;

        private IEnumerator MountLabel(WorldAnchoredLabel label)
        {
            Root.SetReduceMotion(true);
            layer = new VisualElement { pickingMode = PickingMode.Ignore };
            layer.style.position = Position.Absolute;
            layer.style.left = 0;
            layer.style.top = 0;
            layer.style.width = 390;
            layer.style.height = 844;
            layer.Add(label);
            yield return Mount(layer, 3);
        }

        [UnityTest]
        public IEnumerator PanelPosition_PlacesPivotAtAnchorPlusOffset()
        {
            var label = new WorldAnchoredLabel { Text = "Turbine cutaway", Detail = "2 m" };
            yield return MountLabel(label);

            label.SetPanelPosition(new Vector2(200f, 300f));
            yield return WaitFrames(2);

            Assert.IsFalse(label.IsOffscreen);
            Assert.That(label.layout.xMin, Is.EqualTo(212f).Within(1f), "left edge 12 px right of the anchor");
            Assert.That(label.layout.center.y, Is.EqualTo(300f).Within(1f), "vertically centred on the anchor");
            Assert.That(label.Q<Label>(className: WorldAnchoredLabel.DetailClass).text, Is.EqualTo("2 m"));
            Assert.That(label.Q<Label>(className: WorldAnchoredLabel.SeparatorClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(label.resolvedStyle.backgroundColor.a, Is.EqualTo(0.86f).Within(0.01f), "deep panel over the camera");
        }

        [UnityTest]
        public IEnumerator Offscreen_ClampsToSafeArea_OrHides()
        {
            var label = new WorldAnchoredLabel { Text = "Lantern" };
            label.SetSafeArea(0f, 47f, 0f, 34f);
            yield return MountLabel(label);

            label.SetPanelPosition(new Vector2(600f, -100f));
            yield return WaitFrames(2);
            Assert.IsTrue(label.IsOffscreen);
            Assert.That(label.layout.xMax, Is.LessThanOrEqualTo(390f - WorldAnchoredLabel.EdgeMargin + 0.5f));
            Assert.That(label.layout.yMin, Is.GreaterThanOrEqualTo(47f + WorldAnchoredLabel.EdgeMargin - 0.5f), "below the top safe inset");

            label.ClampToSafeArea = false;
            label.SetPanelPosition(new Vector2(600f, -100f));
            yield return WaitFrames(2);
            Assert.IsTrue(label.IsHidden);
            Assert.That(label.pickingMode, Is.EqualTo(PickingMode.Ignore));
        }

        [UnityTest]
        public IEnumerator Distance_FadesAndHides_AndFormats()
        {
            var label = new WorldAnchoredLabel { Text = "Statue", FadeStartDistance = 8f, MaxDistance = 16f };
            yield return MountLabel(label);

            label.SetPanelPosition(new Vector2(100f, 100f), 4f);
            Assert.That(label.style.opacity.value, Is.EqualTo(1f));
            label.SetPanelPosition(new Vector2(100f, 100f), 12f);
            Assert.That(label.style.opacity.value, Is.EqualTo(0.5f).Within(0.01f));
            label.SetPanelPosition(new Vector2(100f, 100f), 20f);
            Assert.IsTrue(label.IsHidden);

            Assert.That(WorldAnchoredLabel.FormatDistance(2.4f), Is.EqualTo("2 m"));
            Assert.That(WorldAnchoredLabel.FormatDistance(450f), Is.EqualTo("450 m"));
            Assert.That(WorldAnchoredLabel.FormatDistance(1234f), Is.EqualTo("1.2 km"));
        }

        [UnityTest]
        public IEnumerator Track_ProjectsThroughCamera_AndWritesDistance()
        {
            var cameraObject = new GameObject("TestCamera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var target = new GameObject("Target").transform;
            target.position = new Vector3(0f, 0f, 5f);

            var label = new WorldAnchoredLabel { Text = "Turbine cutaway", ShowDistance = true, ClampToSafeArea = false };
            label.style.position = Position.Absolute;
            Root.Add(label);
            yield return WaitFrames(2);

            label.Track(camera, target);
            yield return WaitFrames(3);

            var tree = Root.panel.visualTree.layout;
            var expected = Root.WorldToLocal(new Vector2(tree.width / 2f, tree.height / 2f));
            Assert.That(label.AnchorPosition.x, Is.EqualTo(expected.x).Within(2f), "straight ahead is the centre");
            Assert.That(label.AnchorPosition.y, Is.EqualTo(expected.y).Within(2f));
            Assert.That(label.Detail, Is.EqualTo("5 m"));

            target.position = new Vector3(0f, 0f, -5f);
            yield return WaitFrames(3);
            Assert.IsTrue(label.IsHidden, "behind the camera without clamping");

            label.StopTracking();
            Object.Destroy(cameraObject);
            Object.Destroy(target.gameObject);
        }

        [UnityTest]
        public IEnumerator Track_CameraRenderingToTexture_UsesTheTextureHeight()
        {
            var texture = new RenderTexture(390, 844, 16);
            var cameraObject = new GameObject("TextureCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = texture;
            cameraObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var target = new GameObject("Target").transform;
            target.position = new Vector3(0f, 0f, 5f);

            var label = new WorldAnchoredLabel { Text = "Reticle", ClampToSafeArea = false };
            Root.Add(label);
            yield return WaitFrames(2);

            label.Track(camera, target);
            yield return WaitFrames(3);

            var expected = Root.WorldToLocal(RuntimePanelUtils.ScreenToPanel(Root.panel, new Vector2(195f, 422f)));
            Assert.That(label.AnchorPosition.x, Is.EqualTo(expected.x).Within(2f), "straight ahead is the texture centre");
            Assert.That(label.AnchorPosition.y, Is.EqualTo(expected.y).Within(2f));

            target.position = new Vector3(0f, 1f, 5f);
            yield return WaitFrames(3);
            Assert.That(label.AnchorPosition.y, Is.LessThan(expected.y - 10f), "a point above the axis is higher on the panel");

            label.StopTracking();
            camera.targetTexture = null;
            Object.Destroy(texture);
            Object.Destroy(cameraObject);
            Object.Destroy(target.gameObject);
        }

        [UnityTest]
        public IEnumerator TonesAndTap()
        {
            var label = new WorldAnchoredLabel { Text = "Tap to place", Tone = AnchoredLabelTone.Action };
            var taps = 0;
            label.Tapped += () => taps++;
            yield return MountLabel(label);
            label.SetPanelPosition(new Vector2(100f, 200f));
            yield return WaitFrames(2);

            var dot = label.Q(className: WorldAnchoredLabel.DotClass);
            Assert.That(dot.resolvedStyle.backgroundColor.r, Is.EqualTo(0x7F / 255f).Within(0.01f), "--c-accent");
            label.Tone = AnchoredLabelTone.Success;
            yield return WaitFrames(2);
            Assert.That(dot.resolvedStyle.backgroundColor.g, Is.EqualTo(0x8B / 255f).Within(0.01f), "--c-success");

            Click(label);
            Assert.That(taps, Is.EqualTo(1));
        }
    }

    public class ToolRailTests : UIDocumentTestBase
    {
        private static List<ToolItem> Tools() => new()
        {
            new ToolItem("duplicate", "Duplicate"),
            new ToolItem("undo", "Undo"),
            new ToolItem("inspect", "Inspect"),
            new ToolItem("delete", "Delete", warn: true),
        };

        [UnityTest]
        public IEnumerator ActionRail_RaisesToolSelected_SkipsDisabled()
        {
            var rail = new ToolRail();
            rail.SetItems(Tools());
            var selected = new List<string>();
            rail.ToolSelected += selected.Add;
            var host = new VisualElement();
            host.style.width = 342;
            host.style.height = 500;
            host.Add(rail);
            yield return Mount(host, 3);

            Assert.That(rail.ResolvedOrientation, Is.EqualTo(ToolRailOrientation.Horizontal), "portrait parent");
            Assert.That(rail.layout.height, Is.EqualTo(60f).Within(1f));

            rail.SetItemEnabled("undo", false);
            Click(rail.ItemElement("undo"));
            Click(rail.ItemElement("delete"));
            Assert.That(selected, Is.EqualTo(new[] { "delete" }));
            Assert.IsTrue(rail.ItemElement("undo").ClassListContains(ToolRail.DisabledClass));
            Assert.IsNull(rail.ActiveId, "an action rail has no active tool");

            var deleteIcon = rail.ItemElement("delete").Q(className: ToolRail.IconClass);
            Assert.That(deleteIcon.resolvedStyle.unityBackgroundImageTintColor.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "--c-warn");
        }

        [UnityTest]
        public IEnumerator ModeRail_KeepsOneActive_AndGoesVerticalInLandscape()
        {
            var rail = new ToolRail { Selectable = true, ShowLabels = true };
            rail.SetItems(Tools());
            rail.ActiveId = "inspect";
            var host = new VisualElement();
            host.style.width = 800;
            host.style.height = 400;
            host.Add(rail);
            yield return Mount(host, 3);

            Assert.IsTrue(rail.ItemElement("inspect").ClassListContains(ToolRail.ActiveClass));
            Click(rail.ItemElement("duplicate"));
            Assert.That(rail.ActiveId, Is.EqualTo("duplicate"));
            Assert.IsFalse(rail.ItemElement("inspect").ClassListContains(ToolRail.ActiveClass));
            Assert.That(rail.ResolvedOrientation, Is.EqualTo(ToolRailOrientation.Vertical));
            Assert.That(rail.ItemElement("undo").Q<Label>(className: ToolRail.LabelClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }
    }

    public class GestureHintTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator Show_ThenFirstGestureDismisses_OnceOnly()
        {
            Root.SetReduceMotion(true);
            var hint = new GestureHint();
            var dismissed = 0;
            hint.Dismissed += () => dismissed++;
            yield return Mount(hint);

            Assert.That(hint.resolvedStyle.opacity, Is.EqualTo(0f));
            Assert.IsTrue(hint.Show());
            yield return WaitFrames(2);
            Assert.That(hint.resolvedStyle.opacity, Is.EqualTo(1f));

            var prefixes = hint.Query<Label>(className: GestureHint.PrefixClass).ToList();
            var verbs = hint.Query<Label>(className: GestureHint.VerbClass).ToList();
            Assert.That(prefixes[0].text, Is.EqualTo("1 FINGER"));
            Assert.That(verbs[2].text, Is.EqualTo("SCALE"));

            hint.NotifyGesture(Gesture.Rotate);
            Assert.IsFalse(hint.IsShown);
            Assert.IsTrue(hint.HasBeenDismissed);
            Assert.That(dismissed, Is.EqualTo(1));
            Assert.IsFalse(hint.Show(), "show once");

            hint.ShowOnce = false;
            Assert.IsTrue(hint.Show());
            Click(hint);
            Assert.That(dismissed, Is.EqualTo(2), "a tap dismisses");
        }

        [UnityTest]
        public IEnumerator Mouse_Overrides_AndInspector()
        {
            var hint = new GestureHint { Platform = GestureHintPlatform.Mouse, InspectorText = "Inspector" };
            var inspector = 0;
            hint.OpenInspector += () => inspector++;
            yield return Mount(hint);
            hint.Show();

            var prefixes = hint.Query<Label>(className: GestureHint.PrefixClass).ToList();
            Assert.That(prefixes[1].text, Is.EqualTo("RIGHT DRAG"));
            hint.SetHintText(Gesture.Move, "1 doigt", "déplacer");
            Assert.That(prefixes[0].text, Is.EqualTo("1 DOIGT"));

            Click(hint.Q<Label>(className: GestureHint.InspectorClass));
            Assert.That(inspector, Is.EqualTo(1));
            Assert.IsTrue(hint.IsShown, "the inspector link does not dismiss");
        }
    }

    public class AudioGuidePlayerTests : UIDocumentTestBase
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
        public IEnumerator State_DrivesTimeFillAndPlayIcon()
        {
            var player = new AudioGuidePlayer { TitleText = "Blade inspection", Duration = 190f };
            player.style.width = 340;
            var requests = new List<bool>();
            var replays = 0;
            player.PlayPauseRequested += requests.Add;
            player.ReplayRequested += () => replays++;
            yield return Mount(player, 3);

            player.Position = 42f;
            player.IsPlaying = true;
            yield return WaitFrames(2);
            Assert.That(player.Q<Label>(className: AudioGuidePlayer.TimeClass).text, Is.EqualTo("0:42 / 3:10"));
            var fill = player.Q(className: AudioGuidePlayer.FillClass);
            var track = player.Q(className: AudioGuidePlayer.TrackClass);
            Assert.That(fill.layout.width / track.layout.width, Is.EqualTo(42f / 190f).Within(0.01f));
            Assert.IsTrue(player.Q(className: AudioGuidePlayer.PlayIconClass).ClassListContains("audioGuidePlayer__playIcon--pause"));

            Click(player.Q(className: AudioGuidePlayer.PlayClass));
            Assert.That(requests, Is.EqualTo(new[] { false }), "playing: the button asks to pause");

            player.IsPlaying = false;
            player.Position = 190f;
            Assert.IsTrue(player.IsEnded);
            Assert.IsTrue(player.ClassListContains(AudioGuidePlayer.EndedClass));
            Click(player.Q(className: AudioGuidePlayer.PlayClass));
            Assert.That(replays, Is.EqualTo(1), "ended: the button replays");
        }

        [UnityTest]
        public IEnumerator Scrub_HoldsPositionUpdates_ThenRequestsSeek()
        {
            var player = new AudioGuidePlayer { Duration = 100f };
            player.style.width = 340;
            var seeks = new List<float>();
            player.SeekRequested += seeks.Add;
            yield return Mount(player, 3);

            var scrubber = player.Scrubber;
            var bounds = scrubber.worldBound;
            SendPointer<PointerDownEvent>(scrubber, new Vector2(bounds.xMin + (bounds.width * 0.25f), bounds.center.y));
            Assert.IsTrue(player.IsScrubbing);
            SendPointer<PointerMoveEvent>(scrubber, new Vector2(bounds.xMin + (bounds.width * 0.6f), bounds.center.y));

            player.Position = 10f;
            Assert.That(player.DisplayedPosition, Is.EqualTo(60f).Within(1f), "the thumb stays under the finger");

            SendPointer<PointerUpEvent>(scrubber, new Vector2(bounds.xMin + (bounds.width * 0.6f), bounds.center.y));
            Assert.IsFalse(player.IsScrubbing);
            Assert.That(seeks.Count, Is.EqualTo(1));
            Assert.That(seeks[0], Is.EqualTo(60f).Within(1f));
            Assert.That(player.Position, Is.EqualTo(60f).Within(1f), "shows the requested position until the owner reports");
        }

        [UnityTest]
        public IEnumerator SkipLoopRateAndCues()
        {
            var player = new AudioGuidePlayer { Duration = 60f, ShowSkip = true };
            var skips = new List<float>();
            var loops = new List<bool>();
            var rates = new List<float>();
            var cues = new List<string>();
            player.SkipRequested += skips.Add;
            player.LoopToggled += loops.Add;
            player.RateChanged += rates.Add;
            player.CueChanged += (_, text) => cues.Add(text);
            player.SetCues(new[] { new TranscriptCue(0f, "Welcome to the hall."), new TranscriptCue(10f, "The blades turn slowly.") });
            yield return Mount(player);

            var skipButtons = player.Query<Label>(className: AudioGuidePlayer.SkipClass).ToList();
            Click(skipButtons[0]);
            Click(skipButtons[1]);
            Assert.That(skips, Is.EqualTo(new[] { -15f, 15f }));

            Click(player.Q(className: AudioGuidePlayer.LoopClass));
            Assert.That(loops, Is.EqualTo(new[] { true }));
            Assert.IsTrue(player.ClassListContains(AudioGuidePlayer.LoopingClass));

            var rate = player.Q<Label>(className: AudioGuidePlayer.RateClass);
            Assert.That(rate.text, Is.EqualTo("1x"));
            Click(rate);
            Assert.That(rate.text, Is.EqualTo("1.25x"));
            Assert.That(rates, Is.EqualTo(new[] { 1.25f }));

            player.Position = 12f;
            Assert.That(cues, Is.EqualTo(new[] { "Welcome to the hall.", "The blades turn slowly." }));
            Assert.That(player.Q<Label>(className: AudioGuidePlayer.CueClass).text, Is.EqualTo("The blades turn slowly."));
        }

        [UnityTest]
        public IEnumerator MuteIsHiddenUntilShown_AndToggles()
        {
            var player = new AudioGuidePlayer { Duration = 60f };
            var mutes = new List<bool>();
            player.MuteToggled += mutes.Add;
            yield return Mount(player);

            var mute = player.Q(className: AudioGuidePlayer.MuteClass);
            Assert.IsFalse(player.ShowMute, "hidden by default, for sources without sound");
            Assert.That(mute.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            player.ShowMute = true;
            yield return null;
            Assert.That(mute.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Click(mute);
            Click(mute);
            Assert.That(mutes, Is.EqualTo(new[] { true, false }));
            Assert.IsFalse(player.ClassListContains(AudioGuidePlayer.MutedClass));

            player.IsMuted = true;
            Assert.IsTrue(player.ClassListContains(AudioGuidePlayer.MutedClass), "the owner sets the state from its source");
            Assert.That(mutes, Is.EqualTo(new[] { true, false }), "setting the state raises no request");
        }
    }

    public class ToastProgressAndCoachingActionTests : UIDocumentTestBase
    {
        [UnityTest]
        public IEnumerator ProgressToast_UpdatesInPlace_AndStaysWithZeroDuration()
        {
            Root.SetReduceMotion(true);
            var host = new ToastHost();
            yield return Mount(host);

            var handle = host.Show(new ToastOptions
            {
                Message = "Getting Turbine cutaway",
                Detail = "0 of 4.2 MB",
                Progress = 0f,
                Icon = Background.FromTexture2D(Texture2D.whiteTexture),
                DurationMs = 0,
            });
            yield return WaitFrames(2);

            Assert.IsTrue(handle.Element.ClassListContains(ToastHost.ProgressModifierClass));
            Assert.IsTrue(handle.Element.ClassListContains(ToastHost.HasIconClass));
            Assert.That(handle.Element.Q(className: ToastHost.IconClass).layout.width, Is.EqualTo(34f));

            handle.SetProgress(0.38f);
            handle.SetMessage("Getting Turbine cutaway", "1.6 of 4.2 MB");
            yield return WaitFrames(2);
            var track = handle.Element.Q(className: ToastHost.ProgressClass);
            var fill = handle.Element.Q(className: ToastHost.ProgressFillClass);
            Assert.That(fill.layout.width / track.layout.width, Is.EqualTo(0.38f).Within(0.02f));
            Assert.That(handle.Element.Q<Label>(className: ToastHost.DetailClass).text, Is.EqualTo("1.6 of 4.2 MB"));
            Assert.That(fill.resolvedStyle.backgroundImage.texture, Is.Not.Null, "primary to accent gradient");

            yield return WaitMs(host.DefaultDurationMs + 300);
            Assert.IsTrue(handle.IsVisible, "duration 0 stays until dismissed");

            handle.SetProgress(null);
            yield return WaitFrames(2);
            Assert.That(track.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            handle.Dismiss();
        }

        [UnityTest]
        public IEnumerator ToneDisc_AndGhostPillAction()
        {
            Root.SetReduceMotion(true);
            var host = new ToastHost();
            yield return Mount(host);

            var placed = host.Show(new ToastOptions
            {
                Message = "Placed. Tap it to adjust.",
                Tone = ToastTone.Success,
                Icon = Background.FromTexture2D(Texture2D.whiteTexture),
                ActionText = "Undo",
                DurationMs = 0,
            });
            yield return WaitFrames(2);

            var disc = placed.Element.Q(className: ToastHost.IconClass);
            Assert.That(disc.resolvedStyle.backgroundColor.g, Is.EqualTo(0x8B / 255f).Within(0.01f), "--c-success disc");
            Assert.That(placed.Element.Q(className: ToastHost.IconImageClass).resolvedStyle.unityBackgroundImageTintColor.r, Is.EqualTo(0xF2 / 255f).Within(0.01f), "white glyph");
            Assert.That(placed.Element.Q(className: ToastHost.CardClass).resolvedStyle.borderLeftWidth, Is.EqualTo(3f));

            var action = placed.Element.Q<Button>(className: ToastHost.ActionClass);
            Assert.That(action.layout.height, Is.EqualTo(34f));
            Assert.That(action.resolvedStyle.borderTopWidth, Is.EqualTo(1.5f).Within(0.5f), "ghost pill border");
            Assert.That(action.resolvedStyle.borderTopColor.r, Is.EqualTo(0x52 / 255f).Within(0.01f), "--surface-border-strong");

            var warning = host.Show(new ToastOptions { Message = "Weak tracking", Tone = ToastTone.Warning, Icon = Background.FromTexture2D(Texture2D.whiteTexture), DurationMs = 0 });
            yield return WaitFrames(2);
            Assert.That(warning.Element.Q(className: ToastHost.IconClass).resolvedStyle.backgroundColor.r, Is.EqualTo(0xFB / 255f).Within(0.01f), "--c-warn disc");
            host.DismissAll();
        }

        [UnityTest]
        public IEnumerator CoachingOverlay_ActionButton_RaisesActionInvoked()
        {
            var overlay = new CoachingOverlay { Placement = CoachingPlacement.Top, ActionText = "Cancel placing" };
            var actions = 0;
            overlay.ActionInvoked += () => actions++;
            yield return Mount(overlay);
            overlay.Show(CoachingMode.FindSurface, "Find a surface", "Move the phone slowly over the floor or a table.");
            yield return WaitFrames(2);

            Assert.That(overlay.ActionButton.Variant, Is.EqualTo(PillButtonVariant.Ghost));
            Assert.That(overlay.ActionButton.layout.height, Is.EqualTo(40f));
            var cue = overlay.Q(className: CoachingOverlay.CueClass);
            Assert.That(overlay.ActionButton.worldBound.xMax, Is.EqualTo(cue.worldBound.xMax - 16f).Within(2f), "bottom right of the card");

            Click(overlay.ActionButton.Q(className: PillButton.BackgroundClass));
            Assert.That(actions, Is.EqualTo(1));
            Assert.IsTrue(overlay.IsShown, "the owner decides what the action does");

            overlay.ActionText = null;
            yield return WaitFrames(2);
            Assert.That(overlay.Q(className: CoachingOverlay.ActionsClass).resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }
    }
}
