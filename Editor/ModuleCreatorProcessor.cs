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
        
        public static void CreateSingleModule(SkinnedMeshRenderer skinnedMeshRenderer, ModuleCreatorOptions options, IEnumerable<Vector3> positioins = null, bool KeepMesh = true)
        {
            GameObject root = TraceObjects.CheckTarget(skinnedMeshRenderer.gameObject);
            (GameObject newRoot, string variantPath) = SaveRootObject(root, options.SaveName);
            newRoot.transform.position = Vector3.zero;

            var newSkinnedMeshRender = TraceObjects.TraceCopiedRenderer(root, newRoot, skinnedMeshRenderer);

            ProcessMesh(newSkinnedMeshRender, positioins, KeepMesh, root.name);

            CreateModuleImpl(newRoot, new List<SkinnedMeshRenderer> { newSkinnedMeshRender }, options, root.scene);
            Debug.Log("Saved prefab to " + variantPath);
        }

        public static void CreateMergewdeModule(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers, ModuleCreatorOptions options, IEnumerable<IEnumerable<Vector3>> positioins = null, bool KeepMesh = true)
        {
            GameObject root = TraceObjects.CheckTargets(skinnedMeshRenderers.Select(r => r.gameObject));
            (GameObject newRoot, string variantPath) = SaveRootObject(root, options.SaveName);
            newRoot.transform.position = Vector3.zero;

            List<SkinnedMeshRenderer> newskinnedMeshRenderers = TraceObjects.TraceCopiedRenderers(root, newRoot, skinnedMeshRenderers).ToList();

            var positionsList = positioins.ToList();
            for (int i = 0; i < newskinnedMeshRenderers.Count(); i++)
            {
                ProcessMesh(newskinnedMeshRenderers[i], positionsList[i], KeepMesh, root.name);
            }

            CreateModuleImpl(newRoot, newskinnedMeshRenderers, options, root.scene);
            Debug.Log("Saved prefab to " + variantPath);
        }

        private static void CreateModuleImpl(GameObject new_root, IEnumerable<SkinnedMeshRenderer> newskinnedMeshRenderers, ModuleCreatorOptions options, Scene scene)
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
        
        private static void ProcessMesh(SkinnedMeshRenderer skinnedMeshRenderer, IEnumerable<Vector3> positioins, bool KeepMesh, string rootName)
        {
            if (positioins != null && positioins.Count() > 0)
            {
                Mesh newMesh = KeepMesh 
                    ? MeshHelper.KeepMesh(skinnedMeshRenderer.sharedMesh, positioins) 
                    : MeshHelper.DeleteMesh(skinnedMeshRenderer.sharedMesh, positioins);

                string path = AssetPathUtility.GenerateMeshPath(rootName, $"{skinnedMeshRenderer.name}_modified");
                AssetDatabase.CreateAsset(newMesh, path);
                AssetDatabase.SaveAssets();
                skinnedMeshRenderer.sharedMesh = newMesh;
            }
        }

    }
}