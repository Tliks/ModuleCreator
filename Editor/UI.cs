using UnityEditor;
using UnityEngine;

public class ModuleCreatorWindow : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private static ModuleCreatorSettings settings;
    private const int MENU_PRIORITY = 49;

    private bool showAdvancedSettings = false;

    [MenuItem("GameObject/Module Creator/Create Module", false, MENU_PRIORITY)]
    private static void CreateModule(MenuCommand menuCommand)
    {
        GameObject sourceObject = menuCommand.context as GameObject;

        if (sourceObject == null)
        {
            Debug.LogError("Target object is not set.");
            return;
        }

        settings = new ModuleCreatorSettings();
        ModuleCreator moduleCreator = new ModuleCreator(settings);
        //settings.LogSettings();
        moduleCreator.CheckAndCopyBones(sourceObject);
    }

    [MenuItem("Window/ModuleCreator")]
    public static void ShowWindow()
    {
        GetWindow<ModuleCreatorWindow>("ModuleCreator");
    }

    private void OnEnable()
    {
        settings = new ModuleCreatorSettings();
    }

    private void OnGUI()
    {
        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

        // Checkboxes
        settings.IncludePhysBone = EditorGUILayout.Toggle("PhysBone ", settings.IncludePhysBone);

        if (settings.IncludePhysBone == false) settings.IncludePhysBoneColider = false;
        GUI.enabled = settings.IncludePhysBone;
        settings.IncludePhysBoneColider = EditorGUILayout.Toggle("PhysBoneColider", settings.IncludePhysBoneColider);
        GUI.enabled = true;

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        if (showAdvancedSettings)
        {
            if (settings.IncludePhysBone == false) settings.RenameRootTransform = false;
            GUI.enabled = settings.IncludePhysBone;
            settings.RenameRootTransform = EditorGUILayout.Toggle("Rename RootTransform", settings.RenameRootTransform);
            GUI.enabled = true;
        }

        //settings.LogSettings();
        
        GUI.enabled = skinnedMeshRenderer != null;
        if (GUILayout.Button("Create Module"))
        {
            ModuleCreator moduleCreator = new ModuleCreator(settings);
            moduleCreator.CheckAndCopyBones(skinnedMeshRenderer.gameObject);
        }
        GUI.enabled = true;
    }

}

