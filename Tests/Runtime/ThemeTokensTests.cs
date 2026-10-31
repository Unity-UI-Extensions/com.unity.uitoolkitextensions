/// Credit SimonDarksideJ

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ThemeTokensTests : UIDocumentTestBase
    {
        private const string ProbesPath = "Packages/com.unity.uitoolkitextensions/Tests/Runtime/TokenProbes.uss";
        private const string OverridesPath = "Packages/com.unity.uitoolkitextensions/Tests/Runtime/TokenOverrides.uss";

        private static readonly Color DeepPanel = new(22f / 255f, 24f / 255f, 31f / 255f, 0.86f);
        private static readonly Color PageGround = new(0xF4 / 255f, 0xF7 / 255f, 0xF8 / 255f, 1f);

        private VisualElement container;
        private VisualElement surfaceProbe;

        private static StyleSheet Load(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
#else
            return null;
#endif
        }

        private IEnumerator MountProbes()
        {
            Root.styleSheets.Add(Load(ProbesPath));
            container = new VisualElement();
            surfaceProbe = Probe(container, "probe-surface");
            yield return Mount(container);
        }

        private static VisualElement Probe(VisualElement parent, string className)
        {
            var probe = new VisualElement();
            probe.AddToClassList(className);
            parent.Add(probe);
            return probe;
        }

        private static void AssertColor(Color expected, Color actual, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.01f), message + " (r)");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.01f), message + " (g)");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.01f), message + " (b)");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.01f), message + " (a)");
        }

        [UnityTest]
        public IEnumerator Defaults_ResolveFromThemeSheet()
        {
            yield return MountProbes();
            var text = Probe(container, "probe-text");
            var radius = Probe(container, "probe-radius");
            var space = Probe(container, "probe-space");
            var motion = Probe(container, "probe-motion");
            yield return WaitFrames(2);

            AssertColor(DeepPanel, surfaceProbe.resolvedStyle.backgroundColor, "--surface-bg defaults to --c-panel");
            Assert.That(text.resolvedStyle.fontSize, Is.EqualTo(16f), "--text-body");
            Assert.That(radius.resolvedStyle.borderTopLeftRadius, Is.EqualTo(28f), "--radius-sheet");
            Assert.That(space.resolvedStyle.paddingLeft, Is.EqualTo(16f), "--space-4");
            Assert.That(motion.resolvedStyle.transitionDuration, Has.Some.Matches<TimeValue>(t => Mathf.Approximately(t.value, 240f) && t.unit == TimeUnit.Millisecond), "--motion-base");
        }

        [UnityTest]
        public IEnumerator BrandOverride_OnRoot_ReachesSurfaceAlias()
        {
            Root.styleSheets.Add(Load(OverridesPath));
            yield return MountProbes();

            AssertColor(Color.red, surfaceProbe.resolvedStyle.backgroundColor, "--surface-bg follows an overridden --c-panel");
        }

        [UnityTest]
        public IEnumerator LightSurface_RemapsSurfaceTokens()
        {
            yield return MountProbes();

            container.SetSurface(ThemeSurface.Light);
            yield return WaitFrames(2);
            AssertColor(PageGround, surfaceProbe.resolvedStyle.backgroundColor, "light surface");
            Assert.IsTrue(container.ClassListContains(ThemeTokens.LightSurfaceClass));

            container.SetSurface(ThemeSurface.Inherit);
            yield return WaitFrames(2);
            AssertColor(DeepPanel, surfaceProbe.resolvedStyle.backgroundColor, "inherit returns to deep");
        }

        [UnityTest]
        public IEnumerator ReduceMotion_ZeroesMotionTokens()
        {
            yield return MountProbes();
            var motion = Probe(container, "probe-motion");

            Root.SetReduceMotion(true);
            yield return WaitFrames(2);

            Assert.IsTrue(motion.IsReduceMotion());
            Assert.That(motion.resolvedStyle.transitionDuration, Has.All.Matches<TimeValue>(t => Mathf.Approximately(t.value, 0f)));
        }

        [UnityTest]
        public IEnumerator TextScale_SnapsToSupportedSteps()
        {
            yield return MountProbes();
            var text = Probe(container, "probe-text");

            Assert.That(Root.SetTextScale(1.2f), Is.EqualTo(1.15f));
            yield return WaitFrames(2);
            Assert.That(text.resolvedStyle.fontSize, Is.EqualTo(18f));

            Assert.That(Root.SetTextScale(1.6f), Is.EqualTo(1.3f));
            yield return WaitFrames(2);
            Assert.That(text.resolvedStyle.fontSize, Is.EqualTo(21f));
            Assert.IsFalse(Root.ClassListContains(ThemeTokens.TextScale115Class));

            Assert.That(Root.SetTextScale(1f), Is.EqualTo(1f));
            yield return WaitFrames(2);
            Assert.That(text.resolvedStyle.fontSize, Is.EqualTo(16f));
        }

        [UnityTest]
        public IEnumerator HighContrast_OnAncestor_ReachesSurfaceAlias()
        {
            yield return MountProbes();

            container.SetHighContrast(true);
            yield return WaitFrames(2);

            AssertColor(new Color(12f / 255f, 14f / 255f, 19f / 255f, 0.97f), surfaceProbe.resolvedStyle.backgroundColor, "high contrast panel");
        }
    }
}
