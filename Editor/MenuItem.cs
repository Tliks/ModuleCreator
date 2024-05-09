using UnityEditor;
using UnityEngine;

public class ModuleCreatorWindow : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private static ModuleCreatorSettings Settings;
    private const int MENU_PRIORITY = 49;

    private bool showAdvancedOptions = false;

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

        //if (Settings.IncludePhysBone == false) Settings.IncludePhysBoneColider = false;
        GUI.enabled = Settings.IncludePhysBone;
        Settings.IncludePhysBoneColider = EditorGUILayout.Toggle("PhysBoneColider", Settings.IncludePhysBoneColider);
        GUI.enabled = true;

        EditorGUILayout.Space(); 

        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {
            GUI.enabled = Settings.IncludePhysBone;

            //if (Settings.IncludePhysBone == false) Settings.RemainAllPBTransforms = false;
            GUIContent content_at = new GUIContent("Additional Transforms", "Output Additional PhysBones Affected Transforms for exact PhysBone movement");
            Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, Settings.RemainAllPBTransforms);

            //if (Settings.IncludePhysBone == false) Settings.IncludeIgnoreTransforms = false;
            GUIContent content_ii = new GUIContent("Include IgnoreTransforms", "Output PhysBone's IgnoreTransforms");
            Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, Settings.IncludeIgnoreTransforms);

            //if (Settings.IncludePhysBone == false) Settings.RenameRootTransform = false;
            GUIContent content_rr = new GUIContent(
                "Rename RootTransform", 
                "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.");
            Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, Settings.RenameRootTransform);

            GUI.enabled = true;
            
            GUIContent content_sr = new GUIContent("Specify Root Object", "The default root object is the parent object of the specified skinned mesh rendrer object");
            Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, Settings.RootObject, typeof(GameObject), true);
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

