using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Collections.Immutable;
using com.aoyon.triangleselector.utils;

namespace com.aoyon.modulecreator
{
    public class ModuleCreatorOptions
    {
        public bool IncludePhysBone = true;
        public bool IncludePhysBoneColider = true;

        public bool MergePrefab = false;
        public bool Outputunselected = false;

        public bool RenameRootTransform = true;
        public bool RemainAllPBTransforms = false;
        public bool IncludeIgnoreTransforms = false;
        public GameObject RootObject = null;
        public string SaveName = null;
    }

    public class ModuleCreatorProcessor
    {
        
        public static GameObject CheckAndCopyBones(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, ModuleCreatorOptions settings)
        {   
            GameObject instance = null;
            try
            {
                IEnumerable<GameObject> objs = skinnedMeshRenderers.Select(r => r.gameObject);
                GameObject root = TraceObjects.CheckTargets(objs);

                string mesh_name = objs.Count() == 1 ? objs.First().name : $"{root.name} Parts";
                (GameObject new_root, string variantPath) = SaveRootObject(root, mesh_name);
                new_root.transform.position = Vector3.zero;

                IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers = TraceObjects.TraceCopiedRenderers(root, new_root, skinnedMeshRenderers);

                CreateModule(new_root, newskinnedMeshRenderers, settings, root.scene);
                UnityEngine.Debug.Log("Saved prefab to " + variantPath);

            }

            catch (InvalidOperationException ex)
            {
                UnityEngine.Debug.LogError("[Module Creator] " + ex.Message);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.StackTrace);
                UnityEngine.Debug.LogError(ex);
            }

            return instance;
        }

        public static void CreateModule(GameObject new_root, IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers, ModuleCreatorOptions options, Scene scene)
        {
            try
            {
                HashSet<GameObject> gameobjectsToSave = new();
                HashSet<object> componentsToSave = new();

                var weightedBones = TraceObjects.TraceSkinnedMeshRenderers(newskinnedMeshRenderers, gameobjectsToSave, componentsToSave);
                var physBones = new_root.GetComponentsInChildren<VRCPhysBone>(true);
                TraceObjects.TracePhysBone(physBones, weightedBones, gameobjectsToSave, componentsToSave, options.IncludePhysBoneColider, options.RemainAllPBTransforms, options.IncludeIgnoreTransforms);

                CleanUpHierarchy.CheckAndDeleteRecursive(new_root, gameobjectsToSave, componentsToSave);

                if (options.RenameRootTransform)
                {
                    RenamePhysBone(new_root);
                    RenamePhysBoneColider(new_root);
                }

                PrefabUtility.SavePrefabAsset(new_root);

                GameObject instance = PrefabUtility.InstantiatePrefab(new_root) as GameObject;
                SceneManager.MoveGameObjectToScene(instance, scene);

                EditorGUIUtility.PingObject(instance);
                Selection.activeGameObject = instance;

                Selection.activeGameObject = new_root;
                EditorUtility.FocusProjectWindow();

            }

            catch (InvalidOperationException ex)
            {
                UnityEngine.Debug.LogError("[Module Creator] " + ex.Message);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(ex.StackTrace);
                UnityEngine.Debug.LogError(ex);
            }

        }

        internal static (GameObject, string) SaveRootObject(GameObject root_object, string source_name)
        {
            string variantPath = AssetPathUtility.GeneratePrefabPath(root_object.name, source_name);

            GameObject new_root = PrefabUtility.SaveAsPrefabAsset(root_object, variantPath);        
            if (new_root == null)
            {
                throw new InvalidOperationException("Prefab creation failed.");
            }
            return (new_root, variantPath);

        }

        internal static void RenamePhysBone(GameObject root)
        {
            foreach (VRCPhysBone physBone in root.GetComponentsInChildren<VRCPhysBone>(true))
            {
                physBone.rootTransform.name += ".1";
            }

        }

        internal static void RenamePhysBoneColider(GameObject root)
        {
            foreach (VRCPhysBoneCollider physBone in root.GetComponentsInChildren<VRCPhysBoneCollider>(true))
            {
                physBone.rootTransform.name += ".1";
            }

        }
        
        
    }
}