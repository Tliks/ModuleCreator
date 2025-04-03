using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    internal class CleanUpHierarchy
    {
        public static void CheckAndDeleteRecursive(GameObject gameObject, HashSet<Component> componentsToSave)
        {
            var gameObjectsToSave = componentsToSave.Select(c => c.gameObject).ToHashSet();
            CheckAndDeleteRecursiveImpl(gameObject, gameObjectsToSave, componentsToSave);
        }

        private static void CheckAndDeleteRecursiveImpl(GameObject gameObject, HashSet<GameObject> gameObjectsToSave, HashSet<Component> componentsToSave)
        {
            var children = UnityUtils.GetChildren(gameObject.transform);
            // 子オブジェクトに対して再帰的に処理を適用
            foreach (Transform child in children)
            {   
                CheckAndDeleteRecursiveImpl(child.gameObject, gameObjectsToSave, componentsToSave);
            }

            // 削除しない条件
            if (gameObjectsToSave.Contains(gameObject) || gameObject.transform.childCount != 0)
            {
                ActivateGameObject(gameObject);
                RemoveComponents(gameObject, componentsToSave);
                return;
            }
            
            Object.DestroyImmediate(gameObject, true);
        }

        private static void ActivateGameObject(GameObject gameObject)
        {
            gameObject.SetActive(true);
            gameObject.tag = "Untagged"; 
        }

        private static void RemoveComponents(GameObject gameObject, HashSet<Component> componentsToSave)
        {
            var components = gameObject.GetComponents<Component>();
            foreach (var component in components)
            {
                if (!(component is Transform) && !componentsToSave.Contains(component))
                {
                    Object.DestroyImmediate(component, true);
                }
            }
        }

    }
}