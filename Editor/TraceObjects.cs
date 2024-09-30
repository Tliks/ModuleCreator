using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace com.aoyon.modulecreator
{
    public class TraceObjects: triangleselector.utils.TraceObjects
    {
        public static GameObject CheckTargets(IEnumerable<GameObject> objs)
        {   
            foreach (var obj in objs)
            {
                // null check
                if (obj == null)
                {
                    throw new InvalidOperationException("Target object is not set.");
                }

                // skinnedMeshRendererを持つことを確認
                var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer == null)
                {
                    
                    throw new InvalidOperationException($"'{obj.name}' does not have a SkinnedMeshRenderer.");
                }   

                // PrefabAssetがtargetでないことを確認
                if (PrefabUtility.IsPartOfPrefabAsset(obj))
                {
                    throw new InvalidOperationException("Please select prefab instance in the hierarchy, not on the prefabasset.");
                }

            }

            GameObject root = GetCommonRoot(objs);
            CheckHips(root);
            return root;
        }

        public static void CheckHips(GameObject root)
        {
            GameObject hips = null;
            foreach (Transform child in root.transform)
            {
                foreach (Transform grandChild in child.transform)
                {
                    if (grandChild.name.ToLower().StartsWith("hip"))
                    {
                        hips = grandChild.gameObject;
                        break;
                    }
                }
            }
            if (hips == null)
            {
                Debug.LogWarning("Hips could not be found. Merge Armature/Setup Outfit may not work properly.");
            }
        }

        public static void TracePhysBone(
            IEnumerable<VRCPhysBone> pysbones, 
            HashSet<GameObject> weightedBones,
            HashSet<GameObject> gameobjectsToSave,
            HashSet<object> componentsToSave,
            bool includePhysBoneCollider, 
            bool remainAllPBTransforms, 
            bool includeIgnoreTransforms)
        {
            foreach (VRCPhysBone physBone in pysbones)
            {
                if (physBone.rootTransform == null) physBone.rootTransform = physBone.transform;
                var weightedPBObjects = GetWeightedPhysBoneObjects(physBone, weightedBones, remainAllPBTransforms, includeIgnoreTransforms);

                // 有効なPhysBoneだった場合
                if (weightedPBObjects.Count > 0)
                {
                    componentsToSave.Add(physBone);
                    gameobjectsToSave.Add(physBone.gameObject);
                    gameobjectsToSave.UnionWith(weightedPBObjects);

                    if (includePhysBoneCollider)
                    {
                        foreach (VRCPhysBoneCollider collider in physBone.colliders)
                        {
                            if (collider == null) continue;
                            if (collider.rootTransform == null) collider.rootTransform = collider.transform;
                            componentsToSave.Add(collider);
                            gameobjectsToSave.Add(collider.gameObject);
                            gameobjectsToSave.Add(collider.rootTransform.gameObject);
                        }
                    }
                }
            }
        }

        private static HashSet<GameObject> GetWeightedPhysBoneObjects(
            VRCPhysBone physBone,
            HashSet<GameObject> weightedBones,
            bool remainAllPBTransforms,
            bool includeIgnoreTransforms)
        {
            var WeightedPhysBoneObjects = new HashSet<GameObject>();
            HashSet<Transform> ignoreTransforms = GetIgnoreTransforms(physBone);

            Transform[] allchildren = GetAllChildren(physBone.rootTransform.gameObject);

            foreach (Transform child in allchildren)
            {
                if (weightedBones.Contains(child.gameObject))
                {
                    if (remainAllPBTransforms == true)
                    {
                        WeightedPhysBoneObjects.UnionWith(allchildren.Select(t => t.gameObject));
                        break;

                    }
                    HashSet<GameObject> result = new HashSet<GameObject>();
                    AddSingleChildRecursive(child, result, ignoreTransforms, includeIgnoreTransforms);
                    WeightedPhysBoneObjects.UnionWith(result);
                }
            }

            return WeightedPhysBoneObjects;
        }

        private static void AddSingleChildRecursive(Transform transform, HashSet<GameObject> result, HashSet<Transform> ignoreTransforms, bool includeIgnoreTransforms)
        {   
            if (includeIgnoreTransforms == false && ignoreTransforms.Contains(transform)) return;
            result.Add(transform.gameObject);   
            if (transform.childCount == 1)
            {
                Transform child = transform.GetChild(0);
                AddSingleChildRecursive(child, result, ignoreTransforms, includeIgnoreTransforms);
            }
        }

        private static HashSet<Transform> GetIgnoreTransforms(VRCPhysBone physBone)
        {
            HashSet<Transform> AffectedIgnoreTransforms = new HashSet<Transform>();

            foreach (Transform ignoreTransform in physBone.ignoreTransforms)
            {   
                if (ignoreTransform == null) continue;
                Transform[] AffectedIgnoreTransform = GetAllChildren(ignoreTransform.gameObject);
                AffectedIgnoreTransforms.UnionWith(AffectedIgnoreTransform);
            }

            return AffectedIgnoreTransforms;
        }
    }


}