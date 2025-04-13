using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace com.aoyon.modulecreator
{
    public class ModuleCreatorOptions
    {
        public bool IncludePhysBone;
        public bool IncludePhysBoneColider;
        public bool IncludeConstraints;

        public string SaveName;

        public bool UnpackPrefabToOrigin;
        public bool RenameRootTransform;

        public ModuleCreatorOptions()
        {            
            IncludePhysBone = ModuleCreatorSettings.IncludePhysBone;
            IncludePhysBoneColider = ModuleCreatorSettings.IncludePhysBoneColider;
            IncludeConstraints = ModuleCreatorSettings.IncludeConstraints;
            SaveName = "";
            UnpackPrefabToOrigin = ModuleCreatorSettings.UnpackPrefabToOrigin;
            RenameRootTransform = false;
        }
    }

    public class ModuleCreatorProcessor
    {
        public static GameObject CreateModule(GameObject target, ModuleCreatorOptions options)
        {
            if (string.IsNullOrEmpty(options.SaveName)) options.SaveName = target.name;
            return CreateModule(new GameObject[]{ target }, options);
        }
        
        public static GameObject CreateModule(Renderer renderer, ModuleCreatorOptions options)
        {
            if (string.IsNullOrEmpty(options.SaveName)) options.SaveName = renderer.name;
            return CreateModule(new Renderer[]{ renderer }, options);
        }

        public static GameObject CreateModule(GameObject[] targets, ModuleCreatorOptions options)
        {
            if (!TryGetCommonRoot(targets, out var root)) throw new InvalidOperationException("Please select the objects that have a common parent");
            var renderers = targets.Select(t => t.GetComponent<Renderer>()).Where(r => r != null).ToArray();
            if (string.IsNullOrEmpty(options.SaveName)) options.SaveName = $"{root.name} Parts";
            return CreateModuleImpl(root, renderers, options);
        }

        public static GameObject CreateModule(Renderer[] renderers, ModuleCreatorOptions options)
        {
            if (!TryGetCommonRoot(renderers.Select(t => t.gameObject), out var root)) throw new InvalidOperationException("Please select the objects that have a common parent");
            if (string.IsNullOrEmpty(options.SaveName)) options.SaveName = $"{root.name} Parts";
            return CreateModuleImpl(root, renderers, options);
        }

        private static GameObject CreateModuleImpl(GameObject root, Renderer[] renderers, ModuleCreatorOptions options)
        {   
            var tmpRoot = GetTmpRoot(root);

            RemoveMissingScripts(tmpRoot);
            if (PrefabUtility.IsPartOfAnyPrefab(tmpRoot) && options.UnpackPrefabToOrigin) {
                UnpackPrefabToOrigin(tmpRoot);
            }

            var newRenderers = UnityUtils.GetCorrespondingComponents(root, tmpRoot, renderers).ToArray();

            var context = new RendererDepedencyProviderContext(tmpRoot, newRenderers, options.IncludePhysBone, options.IncludePhysBoneColider, options.IncludeConstraints);
            var componentsToSave = new RendererDepedencyProvider(context).GetAllDependencies();
            CleanUpHierarchy.CheckAndDeleteRecursive(tmpRoot, componentsToSave);

            if (options.RenameRootTransform)
            {
                RenamePBRootTransforms(tmpRoot);
            }
            
            var variantPath = AssetSaver.GeneratePrefabPath(root.name, options.SaveName);
            var variant = PrefabUtility.SaveAsPrefabAsset(tmpRoot, variantPath);
            Debug.Log("Saved prefab to " + variantPath);
            UnityEngine.Object.DestroyImmediate(tmpRoot, true);

            EditorGUIUtility.PingObject(variant);
            Selection.activeObject = variant;
            EditorUtility.FocusProjectWindow();

            if (root.scene.IsValid())
            {
                var instance = PrefabUtility.InstantiatePrefab(variant) as GameObject;
                SceneManager.MoveGameObjectToScene(instance, root.scene);
                EditorGUIUtility.PingObject(instance);
                Selection.activeGameObject = instance;
                return instance;
            }
            else
            {
                return variant;
            }
        }

        private static bool TryGetCommonRoot(IEnumerable<GameObject> objs, out GameObject commonRoot)
        {
            commonRoot = null;
            foreach (var obj in objs)
            {
                var root = GetRoot(obj);
                if (root == null) return false;
                if (commonRoot != null && root != commonRoot) {
                    return false;
                }
                else {
                    commonRoot = root;
                }
            }
            return true;
        }

        private static GameObject GetRoot(GameObject obj)
        {
            if (PrefabUtility.IsPartOfAnyPrefab(obj))
            {
                return PrefabUtility.GetNearestPrefabInstanceRoot(obj);
            }
            else
            {
                return obj.transform.parent?.gameObject;
            }
        }

        private static GameObject GetTmpRoot(GameObject root)
        {
            GameObject tmp_root;
            // Prefab Stage
            if (PrefabUtility.IsPartOfPrefabAsset(root) || PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(root) == true)
            {   
                throw new NotSupportedException("Please execute on the object in the scene");
                /*
                var scene = EditorSceneManager.GetActiveScene();
                // nullが返される
                tmp_root = PrefabUtility.InstantiatePrefab(root, scene) as GameObject;
                */
            }
            // Prefab Instance
            else if (PrefabUtility.IsPartOfPrefabInstance(root))
            {
                Selection.activeObject  = root;
                SceneView.lastActiveSceneView.Focus();
                EditorWindow.focusedWindow.SendEvent(EditorGUIUtility.CommandEvent("Duplicate"));
                tmp_root = Selection.activeGameObject;
            }
            // Non Prefab
            else if (PrefabUtility.IsPartOfAnyPrefab(root))
            {
                tmp_root = UnityEngine.Object.Instantiate(root);
            }
            else
            {
                throw new Exception("invalid prefab type");
            }

            if (tmp_root == root) throw new Exception();

            var transform = tmp_root.transform;
            transform.localPosition = Vector3.zero;
            //transform.localRotation = Quaternion.identity;
            //transform.localScale = Vector3.one;

            return tmp_root;
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            int missings = 0;
            var components = root.GetComponentsInChildren<Component>(true);
            foreach (var component in components)
            {
                if (component == null)
                {
                    UnityEngine.Object.DestroyImmediate(component);
                    missings++;
                }
            }
            if (missings > 0)
            {
                Debug.Log($"Removed {missings} missing components");
            }
        }

        private static void UnpackPrefabToOrigin(GameObject root)
        {
            while (true)
            {
                var parent = PrefabUtility.GetCorrespondingObjectFromSource(root);
                if (parent == null) throw new Exception();
                if (PrefabUtility.IsPartOfModelPrefab(parent) || PrefabUtility.IsPartOfRegularPrefab(parent)) break;
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }
        }

        private static void RenamePBRootTransforms(GameObject root)
        {
            var physBoneTransforms = root.GetComponentsInChildren<VRCPhysBone>(true).Select(p => p.GetTarget());
            var physBoneColliderTransforms = root.GetComponentsInChildren<VRCPhysBoneCollider>(true).Select(p => p.GetTarget());

            var allTransforms = physBoneTransforms.Concat(physBoneColliderTransforms).ToHashSet();
            foreach (var transform in allTransforms)
            {
                transform.name += ".1";
            }
        }
    }
}