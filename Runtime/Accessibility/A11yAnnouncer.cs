/// Credit SimonDarksideJ

using System;
using UnityEngine;
#if UITK_ACCESSIBILITY
using UnityEngine.Accessibility;
#endif

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Specifies how an <see cref="A11yAnnouncer"/> announcement is prioritized.
    /// </summary>
    public enum A11yPoliteness
    {
        /// <summary>
        /// Announces without interrupting the screen reader. An identical announcement within
        /// <see cref="A11yAnnouncer.RepeatWindowMs"/> of the previous announcement is ignored.
        /// </summary>
        Polite,

        /// <summary>
        /// Announces immediately and is not suppressed as a repeat. Use for urgent updates such as errors.
        /// </summary>
        Assertive,
    }

    /// <summary>
    /// Sends short announcements to the platform screen reader, such as TalkBack or VoiceOver.
    /// Examples include a completed step, a caption, or a status change.
    /// </summary>
    /// <remarks>
    /// <para>When Unity's accessibility module is present and a screen reader is enabled, announcements are sent
    /// through <c>AssistiveSupport</c>. The <see cref="Announced"/> event is raised for every accepted announcement,
    /// whether or not platform screen-reader support is available.</para>
    /// <para>Polite announcements with identical text are suppressed within <see cref="RepeatWindowMs"/> of the
    /// previous announcement. This prevents frequently refreshed statuses from being announced repeatedly.</para>
    /// <para><see cref="CaptionBar"/> announces each cue when its <c>announce</c> property is enabled. For toasts,
    /// status chips, and loading steps, the app can call <see cref="Announce"/> from their events.</para>
    /// </remarks>
    public static class A11yAnnouncer
    {
        private static string lastText;
        private static float lastTime = float.NegativeInfinity;

        /// <summary>
        /// Raised for every accepted announcement, whether or not a platform screen reader is enabled.
        /// </summary>
        public static event Action<string, A11yPoliteness> Announced;

        /// <summary>
        /// Gets or sets the time window, in milliseconds, during which an identical polite announcement is suppressed.
        /// </summary>
        public static int RepeatWindowMs { get; set; } = 1500;

        /// <summary>
        /// Gets a value indicating whether the platform reports that a screen reader is enabled.
        /// </summary>
        public static bool IsScreenReaderOn
        {
            get
            {
#if UITK_ACCESSIBILITY
                return AssistiveSupport.isScreenReaderEnabled;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Sends an announcement to the platform screen reader when one is available and raises
        /// <see cref="Announced"/>. Empty or whitespace-only text is ignored.
        /// </summary>
        /// <param name="text">The announcement text.</param>
        /// <param name="politeness">The priority of the announcement.</param>
        /// <returns>
        /// <see langword="true"/> if the announcement was accepted; otherwise, <see langword="false"/> if the text is
        /// empty or whitespace, or if a polite repeat was suppressed.
        /// </returns>
        public static bool Announce(string text, A11yPoliteness politeness = A11yPoliteness.Polite)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var now = Time.realtimeSinceStartup;
            if (politeness == A11yPoliteness.Polite && text == lastText && (now - lastTime) * 1000f < RepeatWindowMs)
            {
                return false;
            }

            lastText = text;
            lastTime = now;

#if UITK_ACCESSIBILITY
            if (AssistiveSupport.isScreenReaderEnabled)
            {
                AssistiveSupport.notificationDispatcher.SendAnnouncement(text);
            }
#endif

            Announced?.Invoke(text, politeness);
            return true;
        }

        /// <summary>
        /// Notifies the platform screen reader that the current screen has changed.
        /// </summary>
        public static void ScreenChanged()
        {
#if UITK_ACCESSIBILITY
            if (AssistiveSupport.isScreenReaderEnabled)
            {
                AssistiveSupport.notificationDispatcher.SendScreenChanged();
            }
#endif
        }

        /// <summary>
        /// Notifies the platform screen reader that part of the current screen has changed, such as when a sheet opens.
        /// </summary>
        public static void LayoutChanged()
        {
#if UITK_ACCESSIBILITY
            if (AssistiveSupport.isScreenReaderEnabled)
            {
                AssistiveSupport.notificationDispatcher.SendLayoutChanged();
            }
#endif
        }

        /// <summary>
        /// Clears the previous announcement used for polite-repeat suppression.
        /// </summary>
        public static void ResetRepeat()
        {
            lastText = null;
            lastTime = float.NegativeInfinity;
        }
    }
}
