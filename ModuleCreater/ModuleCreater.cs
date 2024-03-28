using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class ModuleCreater : MonoBehaviour
{
    public GameObject targetObject; 

    public void CheckAndCopyBones()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target object is not set.");
            return;
        }

        var (armature_indexs, skin_index) = FindObjects(this.gameObject, targetObject);

        GameObject new_root = CopyRootObject(this.gameObject, $"{targetObject.name}_MA");

        CleanUpHierarchy(new_root, armature_indexs, skin_index);

        RemoveComponents(new_root);

        CreatePrefabFromObject(new_root, "Assets/ModuleCreater");

    }

    private (List<int>, int) FindObjects(GameObject root_obj, GameObject targetObject)
    {
        List<GameObject> AllObjects = GetAllObjects(root_obj);

        GameObject armature = root_obj.transform.Find("Armature")?.gameObject;
        if (armature == null)
        {
            Debug.LogError("Armature object not found under the target object.");
        }

        List<int> armature_indexs = GetObjectAndChildrenIndexes(armature, AllObjects);

        SkinnedMeshRenderer skinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("The target object does not have a SkinnedMeshRenderer.");
        }

        int skin_index = GetObjectIndex(targetObject, AllObjects);

        return (armature_indexs, skin_index);
    }

    private HashSet<GameObject> CheckBoneWeight(GameObject targetObject)
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

    private HashSet<GameObject> GetWeightedBones(SkinnedMeshRenderer skinnedMeshRenderer)
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

    private GameObject CopyRootObject(GameObject root_object, string new_name)
    {
        GameObject new_root = Instantiate(root_object);
        new_root.name = new_name;
        return new_root;
    }

    private void CleanUpHierarchy(GameObject new_root, List<int> armature_indexs, int skin_index)
    {   

        List<GameObject> AllObjects = GetAllObjects(new_root);

        List<GameObject> armatures = armature_indexs.Select(index => AllObjects[index]).ToList();
        GameObject armature = armatures[0];
        GameObject skin = AllObjects[skin_index];

        // 不要なオブジェクトを削除
        HashSet<GameObject> weightedBoneNames = CheckBoneWeight(skin);
        CleanUpArmature(new_root, weightedBoneNames, skin);
    }

    private void CleanUpArmature(GameObject root, HashSet<GameObject> validNames, GameObject skin)
    {
        List<GameObject> allChildren = GetAllObjects(root);

        var objectsWithDepth = new List<(GameObject obj, int depth)>();

        foreach (var obj in allChildren)
        {
            int depth = CalculateDepth(obj.transform);
            objectsWithDepth.Add((obj, depth));
        }

        // 階層深度が深い順にソート
        var sortedByDepth = objectsWithDepth.OrderByDescending(x => x.depth).ToList();

        foreach (var item in sortedByDepth)
        {
            GameObject obj = item.obj;


            // 削除の条件
            if (obj.transform.childCount == 0 && !validNames.Contains(obj) && obj!= skin)
            {
                if (obj.transform.parent != null)
                {
                    DestroyImmediate(obj);
                }
            }
        }
    }

    // オブジェクトの階層深度を計算
    private int CalculateDepth(Transform obj)
    {
        int depth = 0;
        while (obj.parent != null)
        {
            depth++;
            obj = obj.parent;
        }
        return depth;
    }

    public static void CreatePrefabFromObject(GameObject obj, string BasePath)
    {
        string savePath = $"{BasePath}/{obj.name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, savePath);
        if (prefab != null)
        {
            Debug.Log(obj.name + "からPrefabが作成され、保存されました: " + savePath);
            PrefabUtility.InstantiatePrefab(prefab);
            DestroyImmediate(obj);
        }
        else
        {
            Debug.LogError("Prefabの作成に失敗しました。");
        }
    }

    public List<GameObject> GetAllObjects(GameObject parent)
    {
        List<GameObject> objects = new List<GameObject>();
        AddChildrenRecursive(parent, objects);
        return objects;
    }

    private void AddChildrenRecursive(GameObject obj, List<GameObject> list)
    {
        list.Add(obj);
        foreach (Transform child in obj.transform)
        {
            AddChildrenRecursive(child.gameObject, list);
        }
    }

    // 指定されたオブジェクトのリスト内でのインデックスを返す
    public int GetObjectIndex(GameObject obj, List<GameObject> list)
    {
        return list.IndexOf(obj);
    }

    // 指定されたオブジェクトとそのすべての子オブジェクトのリスト内でのインデックスを返す
    public List<int> GetObjectAndChildrenIndexes(GameObject obj, List<GameObject> list)
    {
        List<int> indexes = new List<int>();
        AddObjectAndChildrenIndexes(obj, list, indexes);
        return indexes;
    }

    private void AddObjectAndChildrenIndexes(GameObject obj, List<GameObject> list, List<int> indexes)
    {
        int index = list.IndexOf(obj);
        if (index != -1)
        {
            indexes.Add(index);
        }

        foreach (Transform child in obj.transform)
        {
            AddObjectAndChildrenIndexes(child.gameObject, list, indexes);
        }
    }
    public void RemoveComponents(GameObject targetGameObject)
    {
        Component[] components = targetGameObject.GetComponents<Component>();

        
        foreach (var component in components)
        {
            // コンポーネントがTransform以外の場合、削除
            if (!(component is Transform))
            {
                //Debug.LogError($"d{component.name}");
                DestroyImmediate(component);
            }
            else
            {
                //Debug.LogError($"s{component.name}");
            }
        }
    }
}