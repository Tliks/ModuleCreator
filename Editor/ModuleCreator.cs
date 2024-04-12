using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System;

public class ModuleCreator : Editor
{
    private const int PRIORITY = 49;
    
    [MenuItem("GameObject/Module Creator/Create Module", false, PRIORITY)]
    private static void Main(MenuCommand menuCommand)
    {
        GameObject targetObject = menuCommand.context as GameObject;

        if (targetObject == null)
        {
            Debug.LogError("Target object is not set.");
            return;
        }

        CheckAndCopyBones(targetObject);
    }

    private static void CheckAndCopyBones(GameObject targetObject)
    {   
        try
        {
            (GameObject root, int skin_index) = CheckObjects(targetObject);

            GameObject new_root = CopyRootObject(root, $"{root.name}_{targetObject.name}_MA");

            CleanUpHierarchy(new_root, skin_index);

            RemoveComponents(new_root);

            CreatePrefabFromObject(new_root, root.name);

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

    private static GameObject CopyRootObject(GameObject root_object, string new_name)
    {
        GameObject new_root = Instantiate(root_object);
        new_root.name = new_name;
        return new_root;
    }

    private static void CleanUpHierarchy(GameObject new_root, int skin_index)
    {   

        Transform[] AllChildren = GetAllChildren(new_root);
        GameObject skin = AllChildren[skin_index].gameObject;
        skin.SetActive(true);
        skin.tag = "Untagged"; 

        HashSet<GameObject> weightedBones = CheckBoneWeight(skin);
        HashSet<Transform> All_PB_Transforms = Find_PB_Transforms(new_root, weightedBones);
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
            //RemoveComponents(obj);
            return;
        }
        DestroyImmediate(obj);
    }

    private static void CreatePrefabFromObject(GameObject obj, string folder_name)
    {
        string base_path = "Assets/ModuleCreator";
        if (!AssetDatabase.IsValidFolder(base_path))
        {
            AssetDatabase.CreateFolder("Assets", "ModuleCreator");
            AssetDatabase.Refresh();
        }
        
        string folder_path = $"{base_path}/{folder_name}";
        if (!AssetDatabase.IsValidFolder(folder_path))
        {
            AssetDatabase.CreateFolder(base_path, folder_name);
            AssetDatabase.Refresh();
        }

        string savePath = $"{folder_path}/{folder_name}_{obj.name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(obj, savePath, InteractionMode.UserAction);
        if (prefab != null)
        {
            Debug.Log(savePath + "に保存されました");
        }
        else
        {
            throw new InvalidOperationException("Prefabの作成に失敗しました。");
        }
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
        Component[] components = targetGameObject.GetComponents<Component>();

        foreach (Component component in components)
        {
            // コンポーネントがTransform以外の場合、削除
            if (!(component is Transform))
            {
                DestroyImmediate(component);
            }
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

    private static HashSet<Transform> Find_PB_Transforms(GameObject root, HashSet<GameObject> weightedBones)
    {   
        HashSet<Transform> All_PB_Transforms = new HashSet<Transform>();

        VRCPhysBone[] physBones = root.GetComponentsInChildren<VRCPhysBone>(true);
        foreach (VRCPhysBone physBone in physBones)
        {   
            if (physBone.rootTransform == null) 
            {   
                physBone.rootTransform = physBone.transform;
            }
            
            Transform[] PB_Transforms = GetAllChildren(physBone.rootTransform.gameObject);

            HashSet<Transform> weighted_PB_Transforms = new HashSet<Transform>();

            foreach (Transform PB_Transform in PB_Transforms)
            {
                if (weightedBones.Contains(PB_Transform.gameObject))
                {   
                    HashSet<Transform> result = new HashSet<Transform>();
                    AddSingleChildRecursive(PB_Transform, result);
                    weighted_PB_Transforms.UnionWith(result);
                }
            }

            //有効なPBだった場合
            if (weighted_PB_Transforms.Count > 0)
            {
                All_PB_Transforms.Add(physBone.transform);
                All_PB_Transforms.UnionWith(weighted_PB_Transforms);

                foreach (VRCPhysBoneCollider collider in physBone.colliders)
                {   
                    if (collider.rootTransform == null) 
                    {   
                        collider.rootTransform = collider.transform;
                    }

                    All_PB_Transforms.Add(collider.transform);
                    All_PB_Transforms.Add(collider.rootTransform);
                }
            }            
        }

        VRCPhysBoneCollider[] colliders = root.GetComponentsInChildren<VRCPhysBoneCollider>(true);
        foreach (VRCPhysBoneCollider collider in colliders)
        {
            if (!All_PB_Transforms.Contains(collider.transform))
            {
                DestroyImmediate(collider);
            }
        }

        return All_PB_Transforms;
    }

}