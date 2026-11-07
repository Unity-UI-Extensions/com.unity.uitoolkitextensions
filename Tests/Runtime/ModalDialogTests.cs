/// Credit SimonDarksideJ

using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Tests
{
    public class ModalDialogTests : UIDocumentTestBase
    {
        private ModalDialog dialog;

        private IEnumerator MountDialog(bool reduceMotion = true)
        {
            Root.SetReduceMotion(reduceMotion);
            dialog = new ModalDialog();
            yield return Mount(dialog);
        }

        private static DialogOptions Confirm(string secondary = "Cancel") => new()
        {
            Title = "Delete this space?",
            Body = "The anchors and placed content are removed from this device.",
            PrimaryText = "Delete",
            SecondaryText = secondary,
        };

        [UnityTest]
        public IEnumerator StartsClosed_AndHidden()
        {
            yield return MountDialog();

            Assert.IsFalse(dialog.IsOpen);
            Assert.That(dialog.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.IsTrue(dialog.ClassListContains(ThemeTokens.ThemedClass));
        }

        [UnityTest]
        public IEnumerator Show_PrimaryClick_CompletesTaskAndCloses()
        {
            yield return MountDialog();
            var opened = 0;
            DialogResult? closed = null;
            dialog.Opened += () => opened++;
            dialog.Closed += r => closed = r;

            var task = dialog.Show(Confirm());
            yield return WaitFrames(2);

            Assert.IsTrue(dialog.IsOpen);
            Assert.That(opened, Is.EqualTo(1));
            Assert.IsTrue(dialog.ClassListContains(ModalDialog.OpenClass));
            Assert.That(dialog.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(dialog.Q<Label>(className: ModalDialog.TitleClass).text, Is.EqualTo("Delete this space?"));
            Assert.That(dialog.PrimaryButton.focusController.focusedElement, Is.SameAs(dialog.Card), "the card takes focus, so no ring shows on an action");
            Assert.IsTrue(dialog.PrimaryButton.ClassListContains(GradientFill.RootClass), "primary gradient on the primary action");
            Assert.IsFalse(task.IsCompleted);

            Click(dialog.PrimaryButton);
            yield return WaitFrames(2);

            Assert.IsTrue(task.IsCompleted);
            Assert.That(task.Result.Action, Is.EqualTo(DialogAction.Primary));
            Assert.IsTrue(task.Result.Confirmed);
            Assert.That(closed?.Action, Is.EqualTo(DialogAction.Primary));
            Assert.IsFalse(dialog.IsOpen);
            Assert.That(dialog.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "reduce motion hides at once");
        }

        [UnityTest]
        public IEnumerator SingleAction_HidesSecondary()
        {
            yield return MountDialog();
            dialog.Show(Confirm(secondary: null));
            yield return WaitFrames(2);

            Assert.That(dialog.SecondaryButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.IsTrue(dialog.ClassListContains(ModalDialog.SingleActionClass));

            Click(dialog.SecondaryButton);
            dialog.Show(Confirm());
            yield return WaitFrames(2);
            Assert.That(dialog.SecondaryButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator TertiaryAction_IsOptional_AndClosesWithTertiary()
        {
            yield return MountDialog();
            dialog.Show(Confirm());
            yield return WaitFrames(2);
            Assert.That(dialog.TertiaryButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.None), "No third action unless asked for");

            var options = Confirm();
            options.TertiaryText = "Move it instead";
            var task = dialog.Show(options);
            yield return WaitFrames(2);
            Assert.That(dialog.TertiaryButton.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.AreEqual("Move it instead", dialog.TertiaryText);

            Click(dialog.TertiaryButton);
            yield return WaitFrames(2);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(DialogAction.Tertiary, task.Result.Action);
        }

        [UnityTest]
        public IEnumerator Destructive_UsesDangerColour()
        {
            yield return MountDialog();
            var options = Confirm();
            options.Variant = DialogVariant.Destructive;
            dialog.Show(options);
            yield return WaitFrames(2);

            Assert.IsTrue(dialog.ClassListContains(ModalDialog.DestructiveClass));
            Assert.IsFalse(dialog.PrimaryButton.ClassListContains(GradientFill.RootClass), "destructive keeps the solid danger colour");
            var background = dialog.PrimaryButton.resolvedStyle.backgroundColor;
            Assert.That(background.r, Is.EqualTo(0xDC / 255f).Within(0.01f), "--c-danger red");
            Assert.That(background.g, Is.EqualTo(0x26 / 255f).Within(0.01f), "--c-danger green");
            Assert.That(background.b, Is.EqualTo(0x26 / 255f).Within(0.01f), "--c-danger blue");
        }

        [UnityTest]
        public IEnumerator Scrim_DismissesOnlyWhenDismissible()
        {
            yield return MountDialog();
            var scrim = dialog.Q(className: ModalDialog.ScrimClass);

            var options = Confirm();
            options.Dismissible = false;
            var locked = dialog.Show(options);
            yield return WaitFrames(2);
            Click(scrim);
            Assert.IsTrue(dialog.IsOpen, "a non-dismissible dialog ignores the scrim");

            var open = dialog.Show(Confirm());
            Assert.IsTrue(locked.IsCompleted, "a new Show supersedes the previous one");
            Assert.That(locked.Result.Action, Is.EqualTo(DialogAction.None));
            yield return WaitFrames(2);

            Click(scrim);
            Assert.IsTrue(open.IsCompleted);
            Assert.That(open.Result.Action, Is.EqualTo(DialogAction.None));
        }

        [UnityTest]
        public IEnumerator HandleBack_FollowsNavigationRule()
        {
            yield return MountDialog();
            Assert.IsFalse(dialog.HandleBack(), "closed: back is the caller's");

            var options = Confirm();
            options.Dismissible = false;
            dialog.Show(options);
            Assert.IsTrue(dialog.HandleBack(), "non-dismissible swallows back");
            Assert.IsTrue(dialog.IsOpen);

            var task = dialog.Show(Confirm());
            yield return WaitFrames(2);
            PressKey(dialog.PrimaryButton, KeyCode.Escape);
            Assert.IsTrue(task.IsCompleted, "Escape cancels");
            Assert.That(task.Result.Action, Is.EqualTo(DialogAction.None));
        }

        [UnityTest]
        public IEnumerator InputAndChoices_ReturnValues()
        {
            yield return MountDialog();
            var task = dialog.Show(new DialogOptions
            {
                Title = "Name this space",
                PrimaryText = "Save",
                SecondaryText = "Cancel",
                InputLabel = "Space name",
                InputValue = "Lobby",
                Choices = new[] { "Private", "Unlisted", "Public" },
                ChoicesLabel = "Visibility",
                ChoiceIndex = 1,
            });
            yield return WaitFrames(2);

            Assert.IsTrue(dialog.ClassListContains(ModalDialog.HasInputClass));
            Assert.IsTrue(dialog.ClassListContains(ModalDialog.HasChoicesClass));
            Assert.That(dialog.InputField.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(dialog.ChoicesField.value, Is.EqualTo("Unlisted"));
            Assert.IsTrue(dialog.InputField.focusController.focusedElement == dialog.InputField
                          || dialog.InputField.Contains(dialog.InputField.focusController.focusedElement as VisualElement), "the field takes focus");

            dialog.InputField.value = "Main hall";
            dialog.ChoicesField.index = 2;
            Click(dialog.PrimaryButton);

            Assert.IsTrue(task.IsCompleted, "never read Result on a pending task: it blocks the main thread");
            Assert.That(task.Result.InputValue, Is.EqualTo("Main hall"));
            Assert.That(task.Result.ChoiceIndex, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator FocusTrap_CyclesInsideCard()
        {
            yield return MountDialog();
            var outside = new Button { text = "Behind" };
            Root.Insert(0, outside);
            dialog.Show(Confirm());
            yield return WaitFrames(2);

            dialog.PrimaryButton.Focus();
            Navigate(dialog.PrimaryButton, NavigationMoveEvent.Direction.Next);
            Assert.That(dialog.focusController.focusedElement, Is.SameAs(dialog.SecondaryButton), "wraps to the first action");
            Navigate(dialog.SecondaryButton, NavigationMoveEvent.Direction.Previous);
            Assert.That(dialog.focusController.focusedElement, Is.SameAs(dialog.PrimaryButton));
            Assert.That(dialog.focusController.focusedElement, Is.Not.SameAs(outside));
        }

        [UnityTest]
        public IEnumerator CustomContent_GoesBetweenBodyAndActions()
        {
            yield return MountDialog();
            var field = new Label("custom");
            dialog.Add(field);

            Assert.That(field.hierarchy.parent, Is.SameAs(dialog.Q(className: ModalDialog.ContentClass)));
            Assert.That(field.parent, Is.SameAs(dialog), "the logical parent is the dialog");
        }

        [UnityTest]
        public IEnumerator Card_ReadsTokens_AndKeepsMaxWidth()
        {
            Root.style.width = 820;
            yield return MountDialog();
            dialog.Show(Confirm());
            yield return WaitFrames(3);

            Assert.That(dialog.Card.resolvedStyle.borderTopLeftRadius, Is.EqualTo(22f), "--radius-dialog");
            Assert.That(dialog.Card.layout.width, Is.EqualTo(480f).Within(0.5f), "480 pt maximum on tablets");
        }

        [UnityTest]
        public IEnumerator Animated_CloseHidesAfterTransition()
        {
            yield return MountDialog(reduceMotion: false);
            dialog.Show(Confirm());
            // The scheduled open class and its styles must resolve before timing the transition.
            yield return WaitFrames(2);
            yield return WaitMs(ThemeTokens.Motion.BaseMs + 100);
            Assert.IsTrue(dialog.ClassListContains(ModalDialog.OpenClass));
            Assert.That(dialog.Card.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.01f));

            dialog.Close(DialogAction.Secondary);
            yield return WaitFrames(1);
            Assert.That(dialog.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex), "still fading out");

            yield return WaitMs(ThemeTokens.Motion.BaseMs + 150);
            Assert.That(dialog.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator ReopeningDuringClose_DoesNotHideTheNewDialog()
        {
            yield return MountDialog(reduceMotion: false);
            dialog.Show(Confirm());
            yield return WaitFrames(2);
            yield return WaitMs(ThemeTokens.Motion.BaseMs + 100);
            dialog.Close(DialogAction.Secondary);
            yield return WaitFrames(1);
            var reopened = dialog.Show(Confirm());
            yield return WaitFrames(2);
            yield return WaitMs(ThemeTokens.Motion.BaseMs + 150);
            Assert.IsTrue(dialog.IsOpen);
            Assert.IsFalse(reopened.IsCompleted);
            Assert.That(dialog.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(dialog.Card.resolvedStyle.opacity, Is.EqualTo(1f).Within(0.01f));
        }
    }
}
