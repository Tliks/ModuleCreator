using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.PhysBone.Components;

public class ModuleCreator : Editor
{
    private const int MENU_PRIORITY = 49;
    private static bool includePhysBone = false;
    
    [MenuItem("GameObject/Module Creator/Create Module", false, MENU_PRIORITY)]
    private static void CreateModule(MenuCommand menuCommand)
    {
        GameObject sourceObject = menuCommand.context as GameObject;

        if (sourceObject == null)
        {
            Debug.LogError("Target object is not set.");
            return;
        }

        includePhysBone = true;
        CheckAndCopyBones(sourceObject);
    }

    [MenuItem("GameObject/Module Creator/Create Module without PhysBone", false, MENU_PRIORITY)]
    private static void CreateModuleWithoutPhysBone(MenuCommand menuCommand)
    {
        GameObject sourceObject = menuCommand.context as GameObject;

        if (sourceObject == null)
        {
            Debug.LogError("Target object is not set.");
            return;
        }

        includePhysBone = false;
        CheckAndCopyBones(sourceObject);
    }

    private static void CheckAndCopyBones(GameObject sourceObject)
    {   
        try
        {
            (GameObject root, int skin_index) = CheckObjects(sourceObject);

            (GameObject new_root, string variantPath) = CopyRootObject(root, sourceObject.name);

            CleanUpHierarchy(new_root, skin_index);

            PrefabUtility.InstantiatePrefab(new_root);
            
            Debug.Log(variantPath + "に保存されました");

        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
        }
    }

    private static (GameObject, int) CheckObjects(GameObject targetObject)
    {
        //親オブジェクトが存在するか確認
        Transform parent = targetObject.transform.parent;
        if (parent == null)
        {
            throw new InvalidOperationException("アバター(衣装)直下のSkinnedMeshRendererがついたオブジェクトを選択してください");
        }
        GameObject root = parent.gameObject;

        //armatureがあるか確認
        GameObject armature = null;
        foreach (Transform child in root.transform)
        {
            if (child.name.ToLower().StartsWith("armature"))
            {
                armature = child.gameObject;
                break;
            }
        }
        if (armature == null)
        {
            throw new InvalidOperationException("Armature object not found under the root object.");
        }

        //SkinnedMeshRendererがついたオブジェクトか確認
        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            throw new InvalidOperationException($"{targetObject.name} does not have a SkinnedMeshRenderer.");
        }

        //skin_index: 複製先でSkinnedMeshRendererがついたオブジェクトを追跡するためのインデックス
        Transform[] AllChildren = GetAllChildren(root);
        int skin_index = Array.IndexOf(AllChildren, targetObject.transform);

        return (root, skin_index);
    }

    private static HashSet<GameObject> CheckBoneWeight(GameObject targetObject)
    {   
        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        // 指定のメッシュにウェイトを付けてるボーンの一覧を取得
        HashSet<GameObject> weightedBones = GetWeightedBones(skinnedMeshRenderer);

        Debug.Log($"Bones influencing {targetObject.name}: {weightedBones.Count}/{skinnedMeshRenderer.bones.Length}");
        return weightedBones;
    }

    private static HashSet<GameObject> GetWeightedBones(SkinnedMeshRenderer skinnedMeshRenderer)
    {   
        BoneWeight[] boneWeights = skinnedMeshRenderer.sharedMesh.boneWeights;
        HashSet<GameObject> weightedBones = new HashSet<GameObject>();
        foreach (BoneWeight boneWeight in boneWeights)
        {
            if (boneWeight.weight0 > 0) weightedBones.Add(skinnedMeshRenderer.bones[boneWeight.boneIndex0].gameObject);
            if (boneWeight.weight1 > 0) weightedBones.Add(skinnedMeshRenderer.bones[boneWeight.boneIndex1].gameObject);
            if (boneWeight.weight2 > 0) weightedBones.Add(skinnedMeshRenderer.bones[boneWeight.boneIndex2].gameObject);
            if (boneWeight.weight3 > 0) weightedBones.Add(skinnedMeshRenderer.bones[boneWeight.boneIndex3].gameObject);
        }
        return weightedBones;
    }

    private static (GameObject, string) CopyRootObject(GameObject root_object, string source_name)
    {
        string base_path = $"Assets/ModuleCreator";
        if (!AssetDatabase.IsValidFolder(base_path))
        {
            AssetDatabase.CreateFolder("Assets", "ModuleCreator");
            AssetDatabase.Refresh();
        }
        
        string folderPath = $"{base_path}/{root_object.name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(base_path, root_object.name);
            AssetDatabase.Refresh();
        }

        string fileName = $"{source_name}_MA";
        string fileExtension = "prefab";
        
        string variantPath = AssetDatabase.GenerateUniqueAssetPath(folderPath + "/" + fileName + "." + fileExtension);
        
        GameObject new_root = PrefabUtility.SaveAsPrefabAsset(root_object, variantPath);        
        if (new_root == null)
        {
            throw new InvalidOperationException("Prefabの作成に失敗しました。");
        }
        return (new_root, variantPath);
    }

    private static void CleanUpHierarchy(GameObject new_root, int skin_index)
    {   
        //複製先のSkinnedMeshRendererがついたオブジェクトを取得
        Transform[] AllChildren = GetAllChildren(new_root);
        GameObject skin = AllChildren[skin_index].gameObject;
        skin.SetActive(true);
        skin.tag = "Untagged"; 

        HashSet<GameObject> weightedBones = CheckBoneWeight(skin);
        HashSet<Transform> All_PB_Transforms;
        if (includePhysBone == true) 
        {
            All_PB_Transforms = FindPhysBoneTransforms(new_root, weightedBones);
        }
        else
        {
            All_PB_Transforms = new HashSet<Transform>();
        }
        CheckAndDeleteRecursive(new_root, weightedBones, skin, All_PB_Transforms);
    }

    private static void CheckAndDeleteRecursive(GameObject obj, HashSet<GameObject> weightedBones, GameObject skin, HashSet<Transform> All_PB_Transforms)
    {   
        List<GameObject> children = GetChildren(obj);

        // 子オブジェクトに対して再帰的に処理を適用
        foreach (GameObject child in children)
        {   
            CheckAndDeleteRecursive(child, weightedBones, skin, All_PB_Transforms);
        }

        // 削除しない条件
        if (obj == skin || All_PB_Transforms.Contains(obj.transform) || weightedBones.Contains(obj) || obj.transform.childCount != 0)
        {
            RemoveComponents(obj);
            return;
        }
        DestroyImmediate(obj, true);
    }

    private static List<GameObject> GetChildren(GameObject parent)
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in parent.transform)
        {
            children.Add(child.gameObject);
        }
        return children;
    }

    private static Transform[] GetAllChildren(GameObject parent)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        return children;
    }

    private static void RemoveComponents(GameObject targetGameObject)
    {
        // Componentを列挙し、Transform、SkinnedMeshRenderer、(VRCPhysBone、VRCPhysBoneCollider)以外を削除
        List<Component> componentsToRemove;
        if (includePhysBone == true)
        {
            componentsToRemove = targetGameObject.GetComponents<Component>()
                .Where(c => !(c is Transform) && !(c is SkinnedMeshRenderer)&& !(c is VRCPhysBone) && !(c is VRCPhysBoneCollider))
                .ToList();
        }
        else
        {
            componentsToRemove = targetGameObject.GetComponents<Component>()
                .Where(c => !(c is Transform) && !(c is SkinnedMeshRenderer))
                .ToList();
        }

        foreach (var component in componentsToRemove)
        {
            DestroyImmediate(component, true);
        }
    }

    public static void AddSingleChildRecursive(Transform transform, HashSet<Transform> result)
    {
        result.Add(transform);
        if (transform.childCount == 1)
        {
            Transform child = transform.GetChild(0);
            AddSingleChildRecursive(child, result);
        }
    }

    private static HashSet<Transform> FindPhysBoneTransforms(GameObject root, HashSet<GameObject> weightedBones)
    {
        var physBoneTransforms = new HashSet<Transform>();

        foreach (VRCPhysBone physBone in root.GetComponentsInChildren<VRCPhysBone>(true))
        {
            if (physBone.rootTransform == null) physBone.rootTransform = physBone.transform;
            var weightedPBTransforms = GetWeightedPhysBoneTransforms(physBone.rootTransform, weightedBones);
            if (weightedPBTransforms.Count > 0)
            {
                physBoneTransforms.Add(physBone.transform);
                physBoneTransforms.UnionWith(weightedPBTransforms);

                foreach (VRCPhysBoneCollider collider in physBone.colliders)
                {
                    if (collider.rootTransform == null) collider.rootTransform = collider.transform;
                    physBoneTransforms.Add(collider.transform);
                    physBoneTransforms.Add(collider.rootTransform);
                }
            }
            else
            {
                DestroyImmediate(physBone, true);
            }
        }

        RemoveUnusedPhysBoneColliders(root, physBoneTransforms);
        return physBoneTransforms;
    }

    private static HashSet<Transform> GetWeightedPhysBoneTransforms(Transform rootTransform, HashSet<GameObject> weightedBones)
    {
        var weightedPBTransforms = new HashSet<Transform>();

        foreach (Transform child in GetAllChildren(rootTransform.gameObject))
        {
            if (weightedBones.Contains(child.gameObject))
            {
                HashSet<Transform> result = new HashSet<Transform>();
                AddSingleChildRecursive(child, result);
                weightedPBTransforms.UnionWith(result);
            }
        }

        return weightedPBTransforms;
    }

    private static void RemoveUnusedPhysBoneColliders(GameObject root, HashSet<Transform> physBoneTransforms)
    {
        foreach (VRCPhysBoneCollider collider in root.GetComponentsInChildren<VRCPhysBoneCollider>(true))
        {
            if (!physBoneTransforms.Contains(collider.transform))
            {
                DestroyImmediate(collider, true);
            }
        }
    }

}