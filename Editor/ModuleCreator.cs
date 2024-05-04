using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.PhysBone.Components;

public class ModuleCreatorSettings
{
    public bool IncludePhysBone = true;
    public bool IncludePhysBoneColider = true;

    public bool RenameRootTransform = false;
    public bool RemainAllPBTransforms = false;
    public bool IncludeIgnoreTransforms = false;
    public GameObject RootObject = null;

    public void LogSettings()
    {
        Debug.Log("Module Creator Settings:");
        Debug.Log($"PhysBone: {IncludePhysBone}");
        Debug.Log($"PhysBoneColider: {IncludePhysBoneColider}");
        Debug.Log($"RenameRootTransform: {RenameRootTransform}");
    }
}

public class ModuleCreator
{
    private ModuleCreatorSettings Settings;

    public ModuleCreator(ModuleCreatorSettings settings)
    {
        Settings = settings;
    }
    
    public void CheckAndCopyBones(GameObject sourceObject)
    {   
        try
        {
            (GameObject root, int skin_index) = CheckObjects(sourceObject);

            (GameObject new_root, string variantPath) = CopyRootObject(root, sourceObject.name);

            CleanUpHierarchy(new_root, skin_index, Settings);

            PrefabUtility.InstantiatePrefab(new_root);
            
            Debug.Log(variantPath + "に保存されました");

        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            Debug.LogError(ex.StackTrace);
        }
    }

    private GameObject CheckRoot(GameObject targetObject)
    {
        if (Settings.RootObject) return Settings.RootObject;
        //親オブジェクトが存在するか確認
        Transform parent = targetObject.transform.parent;
        if (parent == null)
        {
            throw new InvalidOperationException("アバター(衣装)直下のSkinnedMeshRendererがついたオブジェクトを選択してください");
        }
        GameObject root = parent.gameObject;
        return root;
    }

    private void CheckHips(GameObject root)
    {
        // Check if Hips exists under the root node
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
            //throw new InvalidOperationException("Hips not found under the root object.");
            Debug.LogWarning("Hips not found under the root object.");
        }
    }

    private void CheckSkin(GameObject targetObject)
    {
        //SkinnedMeshRendererがついたオブジェクトか確認
        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            throw new InvalidOperationException($"{targetObject.name} does not have a SkinnedMeshRenderer.");
        }
    }

    private void Checktarget(GameObject targetObject)
    {
        if (targetObject == null)
        {
            throw new InvalidOperationException("Target object is not set.");
        }

    }

    private (GameObject, int) CheckObjects(GameObject targetObject)
    {
        Checktarget(targetObject);
        GameObject root = CheckRoot(targetObject);
        CheckHips(root);
        CheckSkin(targetObject);

        //skin_index: 複製先でSkinnedMeshRendererがついたオブジェクトを追跡するためのインデックス
        Transform[] AllChildren = GetAllChildren(root);
        int skin_index = Array.IndexOf(AllChildren, targetObject.transform);

        return (root, skin_index);
    }

    private HashSet<GameObject> CheckBoneWeight(GameObject targetObject)
    {   
        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        // 指定のメッシュにウェイトを付けてるボーンの一覧を取得
        HashSet<GameObject> weightedBones = GetWeightedBones(skinnedMeshRenderer);

        Debug.Log($"Bones influencing {targetObject.name}: {weightedBones.Count}/{skinnedMeshRenderer.bones.Length}");
        return weightedBones;
    }

    private HashSet<GameObject> GetWeightedBones(SkinnedMeshRenderer skinnedMeshRenderer)
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

    private (GameObject, string) CopyRootObject(GameObject root_object, string source_name)
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

    private void CleanUpHierarchy(GameObject new_root, int skin_index, ModuleCreatorSettings settings)
    {   
        HashSet<GameObject> objectsToSave = new HashSet<GameObject>();

        //複製先のSkinnedMeshRendererがついたオブジェクトを取得
        Transform[] AllChildren = GetAllChildren(new_root);
        GameObject skin = AllChildren[skin_index].gameObject;
        objectsToSave.Add(skin);

        HashSet<GameObject> weightedBones = CheckBoneWeight(skin);
        objectsToSave.UnionWith(weightedBones);

        if (settings.IncludePhysBone == true) 
        {
            HashSet<GameObject> PhysBoneObjects = FindPhysBoneObjects(new_root, weightedBones);
            objectsToSave.UnionWith(PhysBoneObjects);
        }

        CheckAndDeleteRecursive(new_root, objectsToSave);
    }

    private void CheckAndDeleteRecursive(GameObject obj, HashSet<GameObject> objectsToSave)
    {   
        List<GameObject> children = GetChildren(obj);

        // 子オブジェクトに対して再帰的に処理を適用
        foreach (GameObject child in children)
        {   
            CheckAndDeleteRecursive(child, objectsToSave);
        }

        // 削除しない条件
        if (objectsToSave.Contains(obj) || obj.transform.childCount != 0)
        {
            ActivateObject(obj);
            RemoveComponents(obj);
            return;
        }
        
        UnityEngine.Object.DestroyImmediate(obj, true);
    }

    private List<GameObject> GetChildren(GameObject parent)
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

    private void ActivateObject(GameObject obj)
    {
        obj.SetActive(true);
        obj.tag = "Untagged"; 
    }

    private void RemoveComponents(GameObject targetGameObject)
    {
        // Componentを列挙し、Transform、SkinnedMeshRenderer、(VRCPhysBone、VRCPhysBoneCollider)以外を削除
        List<Component> componentsToRemove;
        if (Settings.IncludePhysBone == true)
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
            UnityEngine.Object.DestroyImmediate(component, true);
        }
    }

    private void AddSingleChildRecursive(Transform transform, HashSet<GameObject> result, HashSet<Transform> ignoreTransforms)
    {   
        if (Settings.IncludeIgnoreTransforms == false && ignoreTransforms.Contains(transform)) return;
        result.Add(transform.gameObject);   
        if (transform.childCount == 1)
        {
            Transform child = transform.GetChild(0);
            AddSingleChildRecursive(child, result, ignoreTransforms);
        }
    }

    private HashSet<Transform> GetIgnoreTransforms(VRCPhysBone physBone)
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

    private HashSet<GameObject> FindPhysBoneObjects(GameObject root, HashSet<GameObject> weightedBones)
    {
        var physBoneObjects = new HashSet<GameObject>();

        foreach (VRCPhysBone physBone in root.GetComponentsInChildren<VRCPhysBone>(true))
        {
            if (physBone.rootTransform == null) physBone.rootTransform = physBone.transform;
            var weightedPBObjects = GetWeightedPhysBoneObjects(physBone, weightedBones);
            if (weightedPBObjects.Count > 0)
            {
                //MAの仕様に反し衣装側のPBを強制
                if (Settings.RenameRootTransform == true)
                {
                    physBone.rootTransform.name = $"{physBone.rootTransform.name}.1";
                }

                physBoneObjects.Add(physBone.gameObject);
                physBoneObjects.UnionWith(weightedPBObjects);

                if (Settings.IncludePhysBoneColider == true)
                {
                    foreach (VRCPhysBoneCollider collider in physBone.colliders)
                    {
                        if (collider == null) continue;
                        if (collider.rootTransform == null) collider.rootTransform = collider.transform;
                        physBoneObjects.Add(collider.gameObject);
                        physBoneObjects.Add(collider.rootTransform.gameObject);
                    }
                }
            }
            else UnityEngine.Object.DestroyImmediate(physBone, true);
        }

        if (Settings.IncludePhysBoneColider == true) RemoveUnusedPhysBoneColliders(root, physBoneObjects);
        return physBoneObjects;
    }

    private HashSet<GameObject> GetWeightedPhysBoneObjects(VRCPhysBone physBone, HashSet<GameObject> weightedBones)
    {
        var WeightedPhysBoneObjects = new HashSet<GameObject>();
        HashSet<Transform> ignoreTransforms = GetIgnoreTransforms(physBone);

        Transform[] allchildren = GetAllChildren(physBone.rootTransform.gameObject);

        foreach (Transform child in allchildren)
        {
            if (weightedBones.Contains(child.gameObject))
            {
                if (Settings.RemainAllPBTransforms == true)
                {
                    WeightedPhysBoneObjects.UnionWith(allchildren.Select(t => t.gameObject));
                    break;

                }
                HashSet<GameObject> result = new HashSet<GameObject>();
                AddSingleChildRecursive(child, result, ignoreTransforms);
                WeightedPhysBoneObjects.UnionWith(result);
            }
        }

        return WeightedPhysBoneObjects;
    }

    private void RemoveUnusedPhysBoneColliders(GameObject root, HashSet<GameObject> physBoneObjects)
    {
        foreach (VRCPhysBoneCollider collider in root.GetComponentsInChildren<VRCPhysBoneCollider>(true))
        {
            if (physBoneObjects.Contains(collider.gameObject))
            {
                //MAの仕様に反し衣装側のPBCを強制
                if (Settings.RenameRootTransform == true)
                {
                    collider.rootTransform.name = $"{collider.rootTransform.name}.1";
                    //Debug.Log(collider.rootTransform.name);
                }
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(collider, true);
            }
        }
    }

}