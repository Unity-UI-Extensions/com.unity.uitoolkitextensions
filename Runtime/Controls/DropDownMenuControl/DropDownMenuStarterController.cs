/// Credit SimonDarksideJ

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions
{
    /// <summary>
    /// Demo driver for the Drop Down Menu starter template. <see cref="DropDownMenuControl"/>
    /// is purely programmatic (it has no UXML element), so unlike the other starters this one
    /// pairs its UXML layout with a small component that constructs the menu and wires the
    /// "···" trigger. Replace it with your own controller in real use — the ActionMenu example
    /// shows the same wiring across multiple rows and placements.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DropDownMenuStarterController : MonoBehaviour
    {
        // Constructed in Start — VisualElements must not be created from a MonoBehaviour
        // constructor or field initializer.
        private DropDownMenuControl menu;

        private Label statusLabel;

        private void Start()
        {
            var document = GetComponent<UIDocument>();
            VisualElement root = document.rootVisualElement;

            menu = new DropDownMenuControl();

            statusLabel = root.Q<Label>("menu-status");
            var trigger = root.Q<VisualElement>("menu-trigger");
            if (trigger == null)
            {
                Debug.LogWarning("DropDownMenuStarterController: no element named 'menu-trigger' found in the starter UXML.", this);
                return;
            }

            trigger.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                OpenMenu(trigger);
            });
        }

        private void OpenMenu(VisualElement anchor)
        {
            menu.Open(anchor, new List<DropDownMenuControl.DropDownOption>
            {
                new DropDownMenuControl.DropDownOption("View", () => ReportAction("View")),
                new DropDownMenuControl.DropDownOption("Edit", () => ReportAction("Edit")),
                new DropDownMenuControl.DropDownOption("Remove", () => ReportAction("Remove")),
            },
            DropDownMenuControl.Placement.AnchorRight,
            onDismissed: () => ReportStatus("Menu dismissed"));
        }

        private void ReportAction(string action)
        {
            ReportStatus($"'{action}' selected");
        }

        private void ReportStatus(string status)
        {
            Debug.Log($"Drop Down Menu starter: {status}.");
            if (statusLabel != null)
            {
                statusLabel.text = status;
            }
        }
    }
}
