using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using VRC.SDK3.Dynamics.PhysBone.Components;

public class ModuleCreater : MonoBehaviour
{
    public GameObject targetObject; 

    public void main()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target object is not set.");
            return;
        }

        CheckAndCopyBones(targetObject);
    }

    public void main_All()
    {
        HashSet<GameObject> skins = FindObjectsWithComponent<SkinnedMeshRenderer>(this.gameObject);
        foreach (GameObject skin in skins)
        {
            CheckAndCopyBones(skin);
        }
    }


    private void CheckAndCopyBones(GameObject targetObject)
    {
        int skin_index = CheckObjects(this.gameObject, targetObject);

        GameObject new_root = CopyRootObject(this.gameObject, $"{targetObject.name}_MA");

        CleanUpHierarchy(new_root, skin_index);

        RemoveComponents(new_root);

        CreatePrefabFromObject(new_root, "Assets/ModuleCreater/output");
    }

    private int CheckObjects(GameObject root_obj, GameObject targetObject)
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

    private void CleanUpHierarchy(GameObject new_root, int skin_index)
    {   

        List<GameObject> AllChildren = GetAllChildren(new_root);

        GameObject skin = AllChildren[skin_index];
        HashSet<GameObject> weightedBoneNames = CheckBoneWeight(skin);
        (HashSet<GameObject> PBComponents, HashSet<GameObject> ObjectsUnderPB) = FindObjectsUnderPB(new_root, weightedBoneNames);
        CheckAndDeleteRecursive(new_root, weightedBoneNames, skin, ObjectsUnderPB, PBComponents);
    }

    private void CheckAndDeleteRecursive(GameObject obj, HashSet<GameObject> weightedBoneNames, GameObject skin, HashSet<GameObject> ObjectsUnderPB, HashSet<GameObject> PBComponents)
    {   
        List<GameObject> children = GetChildren(obj);

        // 子オブジェクトに対して再帰的に処理を適用
        foreach (GameObject child in children)
        {
            CheckAndDeleteRecursive(child, weightedBoneNames, skin, ObjectsUnderPB, PBComponents);
        }

        // 子オブジェクトがない、指定のメッシュにウェイトを付けていない、指定のメッシュでない条件を全て満たす場合、オブジェクトを削除
        if (obj == skin || PBComponents.Contains(obj))
        {
            return;
        }
        if (ObjectsUnderPB.Contains(obj) || weightedBoneNames.Contains(obj))
        {
            return;
        }
        if (obj.transform.childCount != 0)
        {
            return;
        }

        DestroyImmediate(obj);
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

    private List<GameObject> GetChildren(GameObject parent)
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in parent.transform)
        {
            children.Add(child.gameObject);
        }
        return children;
    }

    private List<GameObject> GetAllChildren(GameObject parent)
    {
        List<GameObject> children = new List<GameObject>();
        AddChildrenRecursive(parent, children);
        return children;
    }

    private void AddChildrenRecursive(GameObject parent, List<GameObject> children)
    {
        children.Add(parent);
        foreach (Transform child in parent.transform)
        {
            AddChildrenRecursive(child.gameObject, children);
        }
    }

    private void RemoveComponents(GameObject targetGameObject)
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

    private HashSet<GameObject> FindObjectsWithComponent<T>(GameObject rootObj) where T : Component
    {
        HashSet<GameObject> objectsWithComponent = new HashSet<GameObject>();
        List<GameObject> allChildren = GetAllChildren(rootObj);

        foreach (GameObject obj in allChildren)
        {
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                objectsWithComponent.Add(obj);
            }
        }
        return objectsWithComponent;
    }

    private (HashSet<GameObject>, HashSet<GameObject>) FindObjectsUnderPB(GameObject root_obj, HashSet<GameObject> weightedBoneNames)
    {   
        HashSet<GameObject> AllObjectsUnderPB = new HashSet<GameObject>();
        HashSet<GameObject> PBComponents = new HashSet<GameObject>();
        List<GameObject> AllChildren = GetAllChildren(root_obj);

        foreach (GameObject obj in AllChildren)
        {   
            var physBone = obj.GetComponent<VRCPhysBone>();
            var physBoneCollider = obj.GetComponent<VRCPhysBoneCollider>();

            if (physBone != null && physBone.rootTransform != null) 
            {   
                GameObject rootBone = physBone.rootTransform.gameObject;
                if (weightedBoneNames.Contains(rootBone))
                {
                    //Debug.Log("PB"+rootBone.name);
                    PBComponents.Add(obj);
                    List<GameObject> ObjectsUnderPB = GetAllChildren(rootBone);
                    AllObjectsUnderPB.UnionWith(ObjectsUnderPB); 
                }
                //Debug.Log(ObjectsUnderPB.Count);
            }
            
            if (physBoneCollider != null && physBoneCollider.rootTransform != null)
            {
                GameObject rootBone = physBoneCollider.rootTransform.gameObject;
                if (weightedBoneNames.Contains(obj))
                {   
                    PBComponents.Add(obj);
                    AllObjectsUnderPB.Add(rootBone); 
                }
                //Debug.Log("PBC"+rootBone.name);
                //List<GameObject> ObjectsUnderPB = GetAllChildren(rootBone);
                //Debug.Log(ObjectsUnderPB.Count);
            }

        }
        // 結果の表示（オプション）
        foreach (GameObject obj in AllObjectsUnderPB)
        {
            //Debug.Log(obj.name);
        }
        Debug.Log(AllObjectsUnderPB.Count);

        return (PBComponents, AllObjectsUnderPB);
    }

}