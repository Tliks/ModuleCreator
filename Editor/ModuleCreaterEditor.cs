using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ModuleCreater))]
public class ModuleCreaterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // 既定のインスペクタUIを描画

        ModuleCreater script = (ModuleCreater)target;

        if (GUILayout.Button("Create Module"))
        {
            script.main();
        }

        if (GUILayout.Button("Create All Module"))
        {
            script.main_All();
        }

    
    }
}
