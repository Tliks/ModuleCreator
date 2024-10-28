using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public class MenuItems : EditorWindow
    {
        private const int MENU_PRIORITY = 49;
        private static int count = 0;

        private const string MCPATH = "GameObject/Module Creator";

        private const string CREATEMODULE = "Create Module"; 

        [MenuItem(MCPATH + "/" + CREATEMODULE, true, MENU_PRIORITY)]
        static bool CreateModuleValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem(MCPATH + "/" + CREATEMODULE, false, MENU_PRIORITY)]
        static void CreateModule()
        {
            count++; 
            if (count != Selection.gameObjects.Count()) return;

            foreach (var obj in Selection.gameObjects)
            {
                var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                ModuleCreatorProcessor.CreateSingleModule(skinnedMeshRenderer, new ModuleCreatorOptions());
            }

            count = 0;
        }
        
        private const string CREATEMODULETR= "Create Module (Advanced)";

        [MenuItem(MCPATH + "/" + CREATEMODULETR, true, MENU_PRIORITY)]
        static bool CreateModuleTRValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem(MCPATH + "/" + CREATEMODULETR, false, MENU_PRIORITY)]
        static void CreateModuleTR()
        {
            count++;
            if (count == Selection.gameObjects.Count())
            {
                SkinnedMeshRenderer[] skinnedMeshRenderers = Selection.gameObjects
                    .Select(obj => obj.GetComponent<SkinnedMeshRenderer>())
                    .Where(renderer => renderer != null)
                    .ToArray();
                ModuleCreator.ShowWindow(skinnedMeshRenderers);
                count = 0;
            }
        }
    }

}
