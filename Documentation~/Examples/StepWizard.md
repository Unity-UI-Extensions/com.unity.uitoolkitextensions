# Example: Step Wizard

## Overview

This example demonstrates a multi-step onboarding wizard that coordinates a `QuadrantStepper`, a `StepProgressBar`, and dynamically swapped content panels. It shows how to drive both navigation controls from a single step index and how to swap the Finish button in at the final step.

## Controls Featured

- [QuadrantStepper](../Controls/QuadrantStepper.md) — displays the current step number inside a quadrant arc graphic at the top of the screen
- [StepProgressBar](../Controls/StepProgressBar.md) — horizontal segmented bar below the stepper that fills segment by segment as steps are completed
- [PillButton](../Controls/PillButton.md) — pill-shaped navigation buttons for Back, Next, and Finish actions

## Scene Setup

1. Create a new Unity scene.
2. Add an empty GameObject named `StepWizardDemo` to the scene hierarchy.
3. Add a `UIDocument` component to the GameObject.
4. Create a `PanelSettings` asset (`Assets > Create > UI Toolkit > Panel Settings`) and configure it:
   - **Scale Mode:** Scale With Screen Size
   - **Reference Resolution:** 1080 × 1920
   - **Screen Match Mode:** Match Width Or Height, blended toward Height
5. Assign the `PanelSettings` asset to the `UIDocument` component's **Panel Settings** field.
6. Add the `StepWizardDemo` MonoBehaviour (found in `Examples~/StepWizard/`) to the same GameObject.
7. Press **Play**.

## What to Expect

The wizard presents four steps in sequence:

| Step | Title | Content |
| --- | --- | --- |
| 1 | About | Basic profile information panel |
| 2 | Goals | Goal-selection panel |
| 3 | Settings | Preference-selection panel |
| 4 | Done | Completion confirmation panel |

At the top of the screen the `QuadrantStepper` shows the current step number and total (e.g., "2 / 4"). Directly below it the `StepProgressBar` fills one additional segment each time the user advances.

- **Back** (`PillButton`) is hidden on step 1.
- **Next** (`PillButton`) is visible on steps 1–3 and hidden on step 4.
- **Finish** (`PillButton`) is hidden on steps 1–3 and visible on step 4.

Only the content panel for the current step is displayed; the others have `display: none`.

## Key Code Patterns

Advancing a step and updating both navigation controls:

```csharp
private void GoToStep(int stepIndex)
{
    _currentStep = Mathf.Clamp(stepIndex, 0, _totalSteps - 1);

    _stepper.SetSelectedIndex(_currentStep);
    _progressBar.SetProgress(_currentStep + 1, _totalSteps);

    for (int i = 0; i < _contentPanels.Count; i++)
    {
        _contentPanels[i].style.display =
            i == _currentStep ? DisplayStyle.Flex : DisplayStyle.None;
    }

    _backButton.style.display  = _currentStep > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    _nextButton.style.display  = _currentStep < _totalSteps - 1 ? DisplayStyle.Flex : DisplayStyle.None;
    _finishButton.style.display = _currentStep == _totalSteps - 1 ? DisplayStyle.Flex : DisplayStyle.None;
}
```
