/// Credit SimonDarksideJ

using System;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// A repeating scheduled action that runs only while its element is displayed: attached to a panel, with no
    /// <c>display: none</c> on the element or any ancestor.
    /// </summary>
    /// <remarks>
    /// <para>UI Toolkit pauses a scheduled item only when its element leaves the panel, not when a screen hides it, so an
    /// animation inside a hidden screen would keep ticking and keep the panel restyling and laying out every frame.</para>
    /// <para>The timer pauses on the first tick or layout change that finds the element hidden. It resumes when the
    /// element is laid out again or re-attached: UI Toolkit zeroes the layout of a hidden subtree, so showing it raises
    /// <see cref="GeometryChangedEvent"/> on the element. <see cref="Stop"/> ends it for good.</para>
    /// </remarks>
    public sealed class DisplayedTimer
    {
        private readonly VisualElement owner;
        private readonly Action action;
        private readonly IVisualElementScheduledItem item;
        private bool stopped;

        private DisplayedTimer(VisualElement owner, Action action, long intervalMs, long delayMs)
        {
            this.owner = owner;
            this.action = action;
            item = owner.schedule.Execute(Tick).Every(intervalMs).StartingIn(delayMs);
            owner.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            owner.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        /// <summary>
        /// Runs <paramref name="action"/> every <paramref name="intervalMs"/> (0 is every frame), first after
        /// <paramref name="delayMs"/>, while <paramref name="owner"/> is displayed.
        /// </summary>
        public static DisplayedTimer Start(VisualElement owner, Action action, long intervalMs, long delayMs = 0)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return new DisplayedTimer(owner, action, intervalMs, delayMs);
        }

        /// <summary>
        /// True until <see cref="Stop"/> is called, even while the element is hidden.
        /// </summary>
        public bool IsRunning => !stopped;

        public bool IsTicking => !stopped && item.isActive;

        public void Stop()
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            item.Pause();
            owner.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            owner.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public static bool IsDisplayed(VisualElement element)
        {
            if (element?.panel == null)
            {
                return false;
            }

            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        private void Tick()
        {
            if (!IsDisplayed(owner))
            {
                item.Pause();
                return;
            }

            action();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => Refresh();

        private void OnAttachToPanel(AttachToPanelEvent evt) => Refresh();

        private void Refresh()
        {
            if (stopped)
            {
                return;
            }

            if (IsDisplayed(owner))
            {
                if (!item.isActive)
                {
                    item.Resume();
                }
            }
            else
            {
                item.Pause();
            }
        }
    }
}
