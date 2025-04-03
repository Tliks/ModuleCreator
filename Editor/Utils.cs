using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;

namespace com.aoyon.modulecreator
{
    internal static class Utils
    {
        public static T[] GetImplementClasses<T>() where T : class
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .Select(type => Activator.CreateInstance(type) as T)
                .ToArray();
        }
    }

    internal static class VRCExtensions
    {
        public static Transform GetTarget(this VRCPhysBoneBase physBone) => physBone.rootTransform == null ? physBone.transform : physBone.rootTransform;
        public static Transform GetTarget(this VRCPhysBoneColliderBase physBone) => physBone.rootTransform == null ? physBone.transform : physBone.rootTransform;
        public static Transform GetTarget(this VRCConstraintBase constraint) => constraint.TargetTransform == null ? constraint.transform : constraint.TargetTransform;
    }

    internal class UnityUtils
    {
        public static HashSet<Transform> GetWeightedBones(IEnumerable<SkinnedMeshRenderer> skinnedMeshRenderers)
        {
            HashSet<Transform> weightedBones = new ();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                BoneWeight[] boneWeights = skinnedMeshRenderer.sharedMesh.boneWeights;
                Transform[] bones = skinnedMeshRenderer.bones;

                foreach (BoneWeight boneWeight in boneWeights)
                {
                    if (boneWeight.weight0 > 0)
                    {
                        Transform boneTransform = bones[boneWeight.boneIndex0];
                        if (boneTransform == null)
                        {
                            throw new InvalidOperationException($"missing weighted bone");
                        }
                        weightedBones.Add(boneTransform);
                    }
                    if (boneWeight.weight1 > 0)
                    {
                        Transform boneTransform = bones[boneWeight.boneIndex1];
                        if (boneTransform == null)
                        {
                            throw new InvalidOperationException($"missing weighted bone");
                        }
                        weightedBones.Add(boneTransform);
                    }
                    if (boneWeight.weight2 > 0)
                    {
                        Transform boneTransform = bones[boneWeight.boneIndex2];
                        if (boneTransform == null)
                        {
                            throw new InvalidOperationException($"missing weighted bone");
                        }
                        weightedBones.Add(boneTransform);
                    }
                    if (boneWeight.weight3 > 0)
                    {
                        Transform boneTransform = bones[boneWeight.boneIndex3];
                        if (boneTransform == null)
                        {
                            throw new InvalidOperationException($"missing weighted bone");
                        }
                        weightedBones.Add(boneTransform);
                    }
                }

                Debug.Log($"Bones weighting {skinnedMeshRenderer.name}: {weightedBones.Count}/{skinnedMeshRenderer.bones.Length}");
            }

            return weightedBones;
        }

        public static T GetCorrespondingComponent<T>(GameObject root, GameObject new_root, T targetobj) where T : Component
        {
            return GetCorrespondingComponents(root, new_root, new T[] { targetobj }).First();
        }

        public static IEnumerable<T> GetCorrespondingComponents<T>(GameObject root, GameObject new_root, IEnumerable<T> targetobjs) where T : Component
        {
            var allChildren = GetAllChildren(root.transform);
            var newallChildren = GetAllChildren(new_root.transform);

            if (allChildren.Length != newallChildren.Length)
            {
                throw new InvalidOperationException("The number of children in the original and new objects does not match.");
            }

            return targetobjs.Select(obj => {
                var index = Array.IndexOf(allChildren, obj.transform);
                if (index == -1 || index >= newallChildren.Length)
                {
                    throw new InvalidOperationException("Could not find corresponding transform in new root.");
                }
                return newallChildren[index].GetComponent<T>();
            });
        }

        public static List<Transform> GetChildren(Transform parent)
        {
            var children = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                children.Add(child);
            }
            return children;
        }

        public static Transform[] GetAllChildren(Transform target)
        {
            var children = new List<Transform>();

            foreach(Transform child in target)
            {
                children.Add(child);
                children.AddRange(GetAllChildren(child));
            }
            
            return children.ToArray();
        }
    }
}