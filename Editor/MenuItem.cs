using UnityEditor;
using UnityEngine;

public class ModuleCreatorWindow : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private static ModuleCreatorSettings Settings;
    private const int MENU_PRIORITY = 49;

    private bool showAdvancedSettings = false;

    [MenuItem("GameObject/Module Creator/Create Module", false, MENU_PRIORITY)]
    private static void CreateModule(MenuCommand menuCommand)
    {
        GameObject sourceObject = menuCommand.context as GameObject;

        ModuleCreatorSettings settings = new ModuleCreatorSettings();
        ModuleCreator moduleCreator = new ModuleCreator(settings);

        moduleCreator.CheckAndCopyBones(sourceObject);
    }

    [MenuItem("Window/Module Creator")]
    public static void ShowWindow()
    {
        GetWindow<ModuleCreatorWindow>("Module Creator");
    }

    private void OnEnable()
    {
        Settings = new ModuleCreatorSettings();
    }

    private void OnGUI()
    {
        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

        //EditorGUILayout.Space(); 

        // Checkboxes
        Settings.IncludePhysBone = EditorGUILayout.Toggle("PhysBone ", Settings.IncludePhysBone);

        if (Settings.IncludePhysBone == false) Settings.IncludePhysBoneColider = false;
        GUI.enabled = Settings.IncludePhysBone;
        Settings.IncludePhysBoneColider = EditorGUILayout.Toggle("PhysBoneColider", Settings.IncludePhysBoneColider);
        GUI.enabled = true;

        EditorGUILayout.Space(); 

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        if (showAdvancedSettings)
        {
            if (Settings.IncludePhysBone == false) Settings.RemainAllPBTransforms = false;
            GUI.enabled = Settings.IncludePhysBone;
            GUIContent content_at = new GUIContent("Additional Transforms");
            content_at.tooltip = "Output Additional PhysBones Affected Transforms for exact PhysBone movement";
            Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, Settings.RemainAllPBTransforms);
            GUI.enabled = true;

            if (Settings.IncludePhysBone == false) Settings.RenameRootTransform = false;
            GUI.enabled = Settings.IncludePhysBone;
            GUIContent content_rr = new GUIContent("Rename RootTransform");
            content_rr.tooltip = "not recommended: Contrary to the specifications of modular avatar, where the physbone on the costume side is deleted when merging by merge armature in some cases, rename physbone RootTransform to ensure that the physbone on the costume side is integrated. May lead to duplication of physbone";
            Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, Settings.RenameRootTransform);
            GUI.enabled = true;
            
            Settings.RootObject = (GameObject)EditorGUILayout.ObjectField("Specify Root Object", Settings.RootObject, typeof(GameObject), true);
        }

        //settings.LogSettings();

        EditorGUILayout.Space(); 
        
        GUI.enabled = skinnedMeshRenderer != null;
        if (GUILayout.Button("Create Module"))
        {
            ModuleCreator moduleCreator = new ModuleCreator(Settings);
            moduleCreator.CheckAndCopyBones(skinnedMeshRenderer.gameObject);
        }
        GUI.enabled = true;
    }

}

