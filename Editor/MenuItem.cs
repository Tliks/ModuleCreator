using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    internal class MenuItems
    {
        private const string GameObject = "GameObject";
        private const string Tools = "Tools";

        private const string MC = "Module Creator";

        private const int MENU_PRIORITY = 49;
        private const string CREATEMODULEPATH = GameObject + "/" + MC + "/" + "Create Module"; 

        [MenuItem(CREATEMODULEPATH, false, MENU_PRIORITY)]
        static void CreateModule()
        {
            var renderers = Selection.objects.OfType<GameObject>()
                .Select(g => g.GetComponent<Renderer>())
                .Where(r => r is SkinnedMeshRenderer or MeshRenderer)
                .ToArray();
            
            if (renderers.Length == 0) return;

            ModuleCreatorProcessor.CreateModule(renderers, new ModuleCreatorOptions());

            Selection.objects = null;
        }

        private const string INCLUDE_PHYSBONE_PATH = Tools + "/" + MC + "/" + "Include PhysBone";
        private const string INCLUDE_PHYSBONE_COLIDER_PATH = Tools + "/" + MC + "/" + "Include PhysBone Colider";
        private const string INCLUDE_CONSTRAINTS = Tools + "/" + MC + "/" + "Include Constraints";
        private const string UNPACK_PREFAB_PATH = Tools + "/" + MC + "/" + "Unpack Prefab To Origin";

        [MenuItem(INCLUDE_PHYSBONE_PATH, true)]
        private static bool ValidateIncludePhysBone()
        {
            Menu.SetChecked(INCLUDE_PHYSBONE_PATH, ModuleCreatorSettings.IncludePhysBone);
            return true;
        }

        [MenuItem(INCLUDE_PHYSBONE_PATH, false)]
        private static void IncludePhysBone()
        {
            ModuleCreatorSettings.IncludePhysBone = !ModuleCreatorSettings.IncludePhysBone;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem(INCLUDE_PHYSBONE_COLIDER_PATH, true)]
        private static bool ValidateIncludePhysBoneColider()
        {
            Menu.SetChecked(INCLUDE_PHYSBONE_COLIDER_PATH, ModuleCreatorSettings.IncludePhysBoneColider);
            return true;
        }

        [MenuItem(INCLUDE_PHYSBONE_COLIDER_PATH, false)]
        private static void IncludePhysBoneColider()
        {
            ModuleCreatorSettings.IncludePhysBoneColider = !ModuleCreatorSettings.IncludePhysBoneColider;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem(INCLUDE_CONSTRAINTS, true)]
        private static bool ValidateIncludeConstraints()
        {
            Menu.SetChecked(INCLUDE_CONSTRAINTS, ModuleCreatorSettings.IncludeConstraints);
            return true;
        }

        [MenuItem(INCLUDE_CONSTRAINTS, false)]
        private static void IncludeConstraints()
        {
            ModuleCreatorSettings.IncludeConstraints = !ModuleCreatorSettings.IncludeConstraints;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        
        [MenuItem(UNPACK_PREFAB_PATH, true)]
        private static bool ValidateUnpackPrefab()
        {
            Menu.SetChecked(UNPACK_PREFAB_PATH, ModuleCreatorSettings.UnpackPrefabToOrigin);
            return true;
        }

        [MenuItem(UNPACK_PREFAB_PATH, false)]
        private static void UnpackPrefab()
        {
            ModuleCreatorSettings.UnpackPrefabToOrigin = !ModuleCreatorSettings.UnpackPrefabToOrigin;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
