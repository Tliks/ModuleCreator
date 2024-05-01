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

        settings = new ModuleCreatorSettings();
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
        settings = new ModuleCreatorSettings();
    }

    private void OnGUI()
    {
        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

        //EditorGUILayout.Space(); 

        // Checkboxes
        settings.IncludePhysBone = EditorGUILayout.Toggle("PhysBone ", settings.IncludePhysBone);

        if (settings.IncludePhysBone == false) settings.IncludePhysBoneColider = false;
        GUI.enabled = settings.IncludePhysBone;
        settings.IncludePhysBoneColider = EditorGUILayout.Toggle("PhysBoneColider", settings.IncludePhysBoneColider);
        GUI.enabled = true;

        EditorGUILayout.Space(); 

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
        if (showAdvancedSettings)
        {
            if (settings.IncludePhysBone == false) settings.RenameRootTransform = false;
            GUI.enabled = settings.IncludePhysBone;
            GUIContent content_rr = new GUIContent("Rename RootTransform");
            content_rr.tooltip = "Contrary to the specifications of modular avatar, where the physbone on the costume side is deleted when merging by merge armature in some cases, rename physbone RootTransform to ensure that the physbone on the costume side is integrated. May lead to duplication of physbone";
            settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, settings.RenameRootTransform);
            GUI.enabled = true;
            
            if (settings.IncludePhysBone == false) settings.RemainAllPBTransforms = false;
            GUI.enabled = settings.IncludePhysBone;
            GUIContent content_at = new GUIContent("Additional Transforms");
            content_at.tooltip = "Output Additional PhysBones Affected Transforms for exact PhysBone movement";
            settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, settings.RemainAllPBTransforms);
            GUI.enabled = true;

            settings.RootObject = (GameObject)EditorGUILayout.ObjectField("Specify Root Object", settings.RootObject, typeof(GameObject), true);
        }

        //settings.LogSettings();

        EditorGUILayout.Space(); 
        
        GUI.enabled = skinnedMeshRenderer != null;
        if (GUILayout.Button("Create Module"))
        {
            ModuleCreator moduleCreator = new ModuleCreator(settings);
            moduleCreator.CheckAndCopyBones(skinnedMeshRenderer.gameObject);
        }
        GUI.enabled = true;
    }

}

