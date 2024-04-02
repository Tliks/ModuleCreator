using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(ModuleCreater))]
public class ModuleCreater : Editor
{
    private const int PRIORITY = 50;
    
    [MenuItem("GameObject/Module Creater/Create Module", false, PRIORITY)]
    public static void main(MenuCommand menuCommand)
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
        GameObject root =  targetObject.transform.parent.gameObject;

        int skin_index = CheckObjects(root, targetObject);

        GameObject new_root = CopyRootObject(root, $"{root.name}_{targetObject.name}_MA");

        CleanUpHierarchy(new_root, skin_index);

        RemoveComponents(new_root);

        CreatePrefabFromObject(new_root, "Assets/ModuleCreater/output");
    }

    private static int CheckObjects(GameObject root_obj, GameObject targetObject)
    {
        List<GameObject> AllChildren = GetAllChildren(root_obj);

        GameObject armature = root_obj.transform.Find("Armature")?.gameObject;
        if (armature == null)
        {
            armature = root_obj.transform.Find("armature")?.gameObject;
            if (armature == null)
            {
                Debug.LogError("Armature object not found under the root object.");
            }
        }

        //List<int> armature_indexs = GetObjectAndChildrenIndexes(armature, AllChildren);

        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("The target object does not have a SkinnedMeshRenderer.");
        }

        int skin_index = AllChildren.IndexOf(targetObject);

        return skin_index;
    }

    private static HashSet<GameObject> CheckBoneWeight(GameObject targetObject)
    {   
        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        // 指定のメッシュにウェイトを付けてるボーンの一覧を取得
        HashSet<GameObject> weightedBones = GetWeightedBones(skinnedMeshRenderer);

        foreach (GameObject weightedBoneName in weightedBones)
        {
            //Debug.Log($"WeightedBone: {weightedBoneName}");
        } 
        
        Debug.Log($"bones count: {weightedBones.Count}/{skinnedMeshRenderer.bones.Length}");
        return weightedBones;
    }

    private static HashSet<GameObject> GetWeightedBones(SkinnedMeshRenderer skinnedMeshRenderer)
    {   
        BoneWeight[] boneWeights = skinnedMeshRenderer.sharedMesh.boneWeights;
        HashSet<GameObject> weightedBones = new HashSet<GameObject>();
        foreach (var boneWeight in boneWeights)
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

        List<GameObject> AllChildren = GetAllChildren(new_root);

        GameObject skin = AllChildren[skin_index];
        HashSet<GameObject> weightedBoneNames = CheckBoneWeight(skin);
        CheckAndDeleteRecursive(new_root, weightedBoneNames, skin);
    }

    private static void CheckAndDeleteRecursive(GameObject obj, HashSet<GameObject> validNames, GameObject skin)
    {   
        List<GameObject> children = GetChildren(obj);

        // 子オブジェクトに対して再帰的に処理を適用
        foreach (GameObject child in children)
        {
            CheckAndDeleteRecursive(child, validNames, skin);
        }

        // 子オブジェクトがない、指定のメッシュにウェイトを付けていない、指定のメッシュでない条件を全て満たす場合、オブジェクトを削除
        if (obj.transform.childCount == 0 && !validNames.Contains(obj) && obj != skin)
        {
            DestroyImmediate(obj);
        }
    }

    private static void CreatePrefabFromObject(GameObject obj, string BasePath)
    {
        string savePath = $"{BasePath}/{obj.name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(obj, savePath, InteractionMode.UserAction);
        if (prefab != null)
        {
            Debug.Log(obj.name + "が保存されました: " + savePath);
        }
        else
        {
            Debug.LogError("Prefabの作成に失敗しました。");
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

    private static List<GameObject> GetAllChildren(GameObject parent)
    {
        List<GameObject> children = new List<GameObject>();
        AddChildrenRecursive(parent, children);
        return children;
    }

    private static void AddChildrenRecursive(GameObject parent, List<GameObject> children)
    {
        children.Add(parent);
        foreach (Transform child in parent.transform)
        {
            AddChildrenRecursive(child.gameObject, children);
        }
    }

    private static void RemoveComponents(GameObject targetGameObject)
    {
        Component[] components = targetGameObject.GetComponents<Component>();

        foreach (var component in components)
        {
            // コンポーネントがTransform以外の場合、削除
            if (!(component is Transform))
            {
                DestroyImmediate(component);
            }
        }
    }
}