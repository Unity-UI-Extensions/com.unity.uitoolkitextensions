// Credit SimonDarksideJ

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUIToolkit.Extensions.Editor
{
    /// <summary>
    /// Adds the UI Toolkit Extensions menu options to the Unity Editor, the UI Toolkit
    /// counterpart of the uGUI Extensions "GameObject/UI/Extensions" menu.
    /// </summary>
    /// <remarks>
    /// Each "GameObject/UI Toolkit/Extensions" entry bootstraps the scene for UI Toolkit
    /// (creating a UIDocument and PanelSettings where needed) and instantiates a starter
    /// UXML template containing a basic, demo-content version of the chosen control that
    /// the user can open and tweak in UI Builder.
    /// The "Assets/Create/UI Toolkit/Extensions" entries copy just the starter UXML into
    /// the active project folder for use in existing documents.
    /// </remarks>
    internal static class UIToolkitExtensionsMenuOptions
    {
        private const string TemplateRoot = "Packages/com.unity.uitoolkitextensions/Editor/ControlTemplates/";
        private const string GeneratedAssetFolder = "Assets/UI Toolkit Extensions";
        private const string PanelSettingsAssetPath = GeneratedAssetFolder + "/UIToolkitExtensionsPanelSettings.asset";
        private const string ThemeAssetPath = GeneratedAssetFolder + "/UnityDefaultRuntimeTheme.tss";
        private const string DefaultThemeContent =
            "@import url(\"unity-theme://default\");\n" +
            "@import url(\"/Packages/com.unity.uitoolkitextensions/Runtime/Styles/UIToolkitExtensionsControlStyles.uss\");\n";

        #region GameObject menu entries
        [MenuItem("GameObject/UI Toolkit/Extensions/Circular Image Button", false, 10)]
        public static void AddCircularImageButton(MenuCommand menuCommand) => AddControl("CircularImageButton", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Collapsible Section", false, 10)]
        public static void AddCollapsibleSection(MenuCommand menuCommand) => AddControl("CollapsibleSection", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Color Toggle Button", false, 10)]
        public static void AddColorToggleButton(MenuCommand menuCommand) => AddControl("ColorToggleButton", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Color Toggle Group", false, 10)]
        public static void AddColorToggleGroup(MenuCommand menuCommand) => AddControl("ColorToggleGroup", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Coming Soon Message", false, 10)]
        public static void AddComingSoonMessage(MenuCommand menuCommand) => AddControl("ComingSoonMessage", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Drop Down Control", false, 10)]
        public static void AddDropDownControl(MenuCommand menuCommand) => AddControl("DropDownControl", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Drop Down Menu", false, 10)]
        public static void AddDropDownMenu(MenuCommand menuCommand)
        {
            // DropDownMenuControl is code-only (no UXML element), so this starter pairs its
            // UXML layout with a demo driver component that opens the menu from the "···" trigger.
            var go = AddControl("DropDownMenu", menuCommand);
            if (go != null)
            {
                go.AddComponent<DropDownMenuStarterController>();
            }
        }

        [MenuItem("GameObject/UI Toolkit/Extensions/Elastic List View", false, 10)]
        public static void AddElasticListView(MenuCommand menuCommand) => AddControl("ElasticListView", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Grayscale Image", false, 10)]
        public static void AddGrayscaleImage(MenuCommand menuCommand) => AddControl("GrayscaleImage", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Icon Label Button", false, 10)]
        public static void AddIconLabelButton(MenuCommand menuCommand) => AddControl("IconLabelButton", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Loading Icon", false, 10)]
        public static void AddLoadingIcon(MenuCommand menuCommand) => AddControl("LoadingIcon", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Notification Badge", false, 10)]
        public static void AddNotificationBadge(MenuCommand menuCommand) => AddControl("NotificationBadge", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Page Dot Indicator", false, 10)]
        public static void AddPageDotIndicator(MenuCommand menuCommand) => AddControl("PageDotIndicator", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Pill Button", false, 10)]
        public static void AddPillButton(MenuCommand menuCommand) => AddControl("PillButton", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Pill Input Field", false, 10)]
        public static void AddPillInputField(MenuCommand menuCommand) => AddControl("PillInputField", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Pill Selector", false, 10)]
        public static void AddPillSelector(MenuCommand menuCommand) => AddControl("PillSelector", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Rounded Input Field", false, 10)]
        public static void AddRoundedInputField(MenuCommand menuCommand) => AddControl("RoundedInputField", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Screen Header", false, 10)]
        public static void AddScreenHeader(MenuCommand menuCommand) => AddControl("ScreenHeader", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Scroll Snap", false, 10)]
        public static void AddScrollSnap(MenuCommand menuCommand) => AddControl("ScrollSnap", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Social Link Container", false, 10)]
        public static void AddSocialLinkContainer(MenuCommand menuCommand) => AddControl("SocialLinkContainer", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Quadrant Stepper", false, 10)]
        public static void AddQuadrantStepper(MenuCommand menuCommand) => AddControl("QuadrantStepper", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Step Progress Bar", false, 10)]
        public static void AddStepProgressBar(MenuCommand menuCommand) => AddControl("StepProgressBar", menuCommand);

        [MenuItem("GameObject/UI Toolkit/Extensions/Toggle Button", false, 10)]
        public static void AddToggleButton(MenuCommand menuCommand) => AddControl("ToggleButton", menuCommand);
        #endregion

        #region Assets/Create menu entries
        [MenuItem("Assets/Create/UI Toolkit/Extensions/Circular Image Button Starter", false, 601)]
        public static void CreateCircularImageButtonStarter() => CreateStarterAsset("CircularImageButton");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Collapsible Section Starter", false, 601)]
        public static void CreateCollapsibleSectionStarter() => CreateStarterAsset("CollapsibleSection");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Color Toggle Button Starter", false, 601)]
        public static void CreateColorToggleButtonStarter() => CreateStarterAsset("ColorToggleButton");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Color Toggle Group Starter", false, 601)]
        public static void CreateColorToggleGroupStarter() => CreateStarterAsset("ColorToggleGroup");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Coming Soon Message Starter", false, 601)]
        public static void CreateComingSoonMessageStarter() => CreateStarterAsset("ComingSoonMessage");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Drop Down Control Starter", false, 601)]
        public static void CreateDropDownControlStarter() => CreateStarterAsset("DropDownControl");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Drop Down Menu Starter", false, 601)]
        public static void CreateDropDownMenuStarter() => CreateStarterAsset("DropDownMenu");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Elastic List View Starter", false, 601)]
        public static void CreateElasticListViewStarter() => CreateStarterAsset("ElasticListView");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Grayscale Image Starter", false, 601)]
        public static void CreateGrayscaleImageStarter() => CreateStarterAsset("GrayscaleImage");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Icon Label Button Starter", false, 601)]
        public static void CreateIconLabelButtonStarter() => CreateStarterAsset("IconLabelButton");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Loading Icon Starter", false, 601)]
        public static void CreateLoadingIconStarter() => CreateStarterAsset("LoadingIcon");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Notification Badge Starter", false, 601)]
        public static void CreateNotificationBadgeStarter() => CreateStarterAsset("NotificationBadge");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Page Dot Indicator Starter", false, 601)]
        public static void CreatePageDotIndicatorStarter() => CreateStarterAsset("PageDotIndicator");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Pill Button Starter", false, 601)]
        public static void CreatePillButtonStarter() => CreateStarterAsset("PillButton");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Pill Input Field Starter", false, 601)]
        public static void CreatePillInputFieldStarter() => CreateStarterAsset("PillInputField");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Pill Selector Starter", false, 601)]
        public static void CreatePillSelectorStarter() => CreateStarterAsset("PillSelector");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Rounded Input Field Starter", false, 601)]
        public static void CreateRoundedInputFieldStarter() => CreateStarterAsset("RoundedInputField");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Screen Header Starter", false, 601)]
        public static void CreateScreenHeaderStarter() => CreateStarterAsset("ScreenHeader");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Scroll Snap Starter", false, 601)]
        public static void CreateScrollSnapStarter() => CreateStarterAsset("ScrollSnap");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Social Link Container Starter", false, 601)]
        public static void CreateSocialLinkContainerStarter() => CreateStarterAsset("SocialLinkContainer");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Quadrant Stepper Starter", false, 601)]
        public static void CreateQuadrantStepperStarter() => CreateStarterAsset("QuadrantStepper");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Step Progress Bar Starter", false, 601)]
        public static void CreateStepProgressBarStarter() => CreateStarterAsset("StepProgressBar");

        [MenuItem("Assets/Create/UI Toolkit/Extensions/Toggle Button Starter", false, 601)]
        public static void CreateToggleButtonStarter() => CreateStarterAsset("ToggleButton");
        #endregion

        /// <summary>
        /// Ensures the scene can display UI Toolkit content and adds a UIDocument showing
        /// the starter template for the requested control. Returns the created GameObject
        /// (or null if the template could not be loaded) so callers can attach extras.
        /// </summary>
        private static GameObject AddControl(string controlName, MenuCommand menuCommand)
        {
            var template = InstantiateTemplate(controlName, GeneratedAssetFolder);
            if (template == null)
            {
                return null;
            }

            var panelSettings = GetOrCreatePanelSettings();

            var go = new GameObject(ObjectNames.NicifyVariableName(controlName));
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            var document = go.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = template;

            Selection.activeGameObject = go;
            return go;
        }

        /// <summary>
        /// Copies the starter template for a control into the project (if not already present)
        /// so the user has an editable copy, and returns it.
        /// </summary>
        private static VisualTreeAsset InstantiateTemplate(string controlName, string destinationFolder)
        {
            var sourcePath = TemplateRoot + controlName + "Starter.uxml";
            if (AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(sourcePath) == null)
            {
                Debug.LogError($"UI Toolkit Extensions starter template not found at [{sourcePath}]");
                return null;
            }

            var destinationPath = destinationFolder + "/" + controlName + "Starter.uxml";
            var existing = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(destinationPath);
            if (existing != null)
            {
                if (System.IO.File.ReadAllText(sourcePath) == System.IO.File.ReadAllText(destinationPath))
                {
                    return existing;
                }

                if (!EditorUtility.DisplayDialog(
                        "Starter template has changed",
                        $"{controlName}Starter.uxml already exists in {destinationFolder} but differs from the packaged template (it may contain your edits, or come from an older package version).\n\nReplace it with the latest packaged template?",
                        "Replace",
                        "Keep existing"))
                {
                    return existing;
                }

                System.IO.File.Copy(sourcePath, destinationPath, true);
                AssetDatabase.ImportAsset(destinationPath);
                return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(destinationPath);
            }

            EnsureGeneratedAssetFolder();
            AssetDatabase.CopyAsset(sourcePath, destinationPath);
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(destinationPath);
        }

        /// <summary>
        /// Copies a starter template into the currently selected project folder.
        /// </summary>
        private static void CreateStarterAsset(string controlName)
        {
            var sourcePath = TemplateRoot + controlName + "Starter.uxml";
            if (AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(sourcePath) == null)
            {
                Debug.LogError($"UI Toolkit Extensions starter template not found at [{sourcePath}]");
                return;
            }

            var destinationPath = AssetDatabase.GenerateUniqueAssetPath(GetActiveFolderPath() + "/" + controlName + "Starter.uxml");
            AssetDatabase.CopyAsset(sourcePath, destinationPath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(destinationPath);
        }

        /// <summary>
        /// Finds a PanelSettings to render with, preferring one already in use in the open scene,
        /// then any project asset, before finally creating one configured with the default runtime theme.
        /// </summary>
        private static PanelSettings GetOrCreatePanelSettings()
        {
            var existingDocument = Object.FindFirstObjectByType<UIDocument>();
            if (existingDocument != null && existingDocument.panelSettings != null)
            {
                return existingDocument.panelSettings;
            }

            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            EnsureGeneratedAssetFolder();

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeAssetPath);
            if (theme == null)
            {
                System.IO.File.WriteAllText(ThemeAssetPath, DefaultThemeContent);
                AssetDatabase.ImportAsset(ThemeAssetPath);
                theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeAssetPath);
            }

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.themeStyleSheet = theme;
            AssetDatabase.CreateAsset(panelSettings, PanelSettingsAssetPath);
            AssetDatabase.SaveAssets();
            return panelSettings;
        }

        private static void EnsureGeneratedAssetFolder()
        {
            if (!AssetDatabase.IsValidFolder(GeneratedAssetFolder))
            {
                AssetDatabase.CreateFolder("Assets", GeneratedAssetFolder.Substring("Assets/".Length));
            }
        }

        private static string GetActiveFolderPath()
        {
            var obj = Selection.activeObject;
            if (obj == null)
            {
                return "Assets";
            }

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return "Assets";
            }

            return AssetDatabase.IsValidFolder(path) ? path : System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        }
    }
}
