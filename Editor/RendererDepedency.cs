using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace com.aoyon.modulecreator
{
    internal struct RendererDepedencyProviderContext
    {
        public GameObject Root;
        public IEnumerable<Renderer> Renderers;
        public HashSet<Transform> WeightedBones;

        public bool PhysBone;
        public bool PhysBoneCollider;
        public bool Constraints;

        public RendererDepedencyProviderContext(GameObject root, IEnumerable<Renderer> renderers, bool physBone = true, bool physBoneCollider = true, bool constraints = true)
        {
            Root = root;
            Renderers = renderers;

            WeightedBones = GetWeightedBones(renderers);

            PhysBone = physBone;
            PhysBoneCollider = physBoneCollider;
            Constraints = constraints;
        }

        private static HashSet<Transform> GetWeightedBones(IEnumerable<Renderer> renderers)
        {
            var skinnedMeshRenderers = renderers.OfType<SkinnedMeshRenderer>();
            var meshRenderers = renderers.OfType<MeshRenderer>();
            var weightedBones = UnityUtils.GetWeightedBones(skinnedMeshRenderers);
            weightedBones.UnionWith(meshRenderers.Select(m => m.transform));
            return weightedBones;
        }
    }

    internal class RendererDepedencyProvider
    {
        private static IComponentDependency[] _providers;

        [InitializeOnLoadMethod]
        static void Init()
        {
            _providers = Utils.GetImplementClasses<IComponentDependency>();
        }

        private readonly RendererDepedencyProviderContext _context;
        
        public RendererDepedencyProvider(RendererDepedencyProviderContext context)
        {
            _context = context;
        }

        public HashSet<Component> GetAllDependencies()
        {
            var dependencies = new HashSet<Component>();
            
            dependencies.UnionWith(_context.WeightedBones.Select(t => (Component)t).ToHashSet());

            var collector = new RendererDepedencyCollector(dependencies);
            foreach (var provider in _providers)
            {
                if (!provider.IsEnabled(_context)) continue;
                provider.AddDependency(_context, collector);
            }

            return dependencies;
        }
    }

    internal class RendererDepedencyCollector
    {
        private readonly HashSet<Component> _holder;

        public RendererDepedencyCollector(HashSet<Component> holder)
        {
            _holder = holder;
        }

        public void AddDependency(Component componentToSave)
        {
            _holder.Add(componentToSave);
        }

        public void AddDependencies(IEnumerable<Component> componentsToSave)
        {
            _holder.UnionWith(componentsToSave);
        }
    }

    internal interface IComponentDependency
    {
        public bool IsEnabled(RendererDepedencyProviderContext context);
        public void AddDependency(RendererDepedencyProviderContext context, RendererDepedencyCollector collector);
    }

    internal class SkinnedMeshRendererDependency : IComponentDependency
    {
        public bool IsEnabled(RendererDepedencyProviderContext context)
        {
            return true;
        }

        public void AddDependency(RendererDepedencyProviderContext context, RendererDepedencyCollector collector)
        {
            var skinnedMeshRenderers = context.Renderers.OfType<SkinnedMeshRenderer>();

            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                collector.AddDependency(skinnedMeshRenderer);
                if (skinnedMeshRenderer.rootBone != null) collector.AddDependency(skinnedMeshRenderer.rootBone);
                if (skinnedMeshRenderer.probeAnchor != null) collector.AddDependency(skinnedMeshRenderer.probeAnchor);
            }
        }
    }

    internal class MeshRendererDependency : IComponentDependency
    {
        public bool IsEnabled(RendererDepedencyProviderContext context)
        {
            return true;
        }

        public void AddDependency(RendererDepedencyProviderContext context, RendererDepedencyCollector collector)
        {
            var meshRenderers = context.Renderers.OfType<MeshRenderer>();

            foreach (var meshRenderer in meshRenderers)
            {
                collector.AddDependency(meshRenderer);
                var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null) collector.AddDependency(meshFilter);
                if (meshRenderer.probeAnchor != null) collector.AddDependency(meshRenderer.probeAnchor);
            }
        }
    }

    internal class PhysBoneDependency : IComponentDependency
    {
        public bool IsEnabled(RendererDepedencyProviderContext context)
        {
            return context.PhysBone;
        }

        public void AddDependency(RendererDepedencyProviderContext context, RendererDepedencyCollector collector)
        {
            var physBones = context.Root.GetComponentsInChildren<VRCPhysBone>(true);

            foreach (VRCPhysBone physBone in physBones)
            {
                var weightedPBObjects = GetWeightedPhysBoneObjects(physBone, context.WeightedBones);

                // 有効なPhysBoneだった場合
                if (weightedPBObjects.Count > 0)
                {
                    collector.AddDependency(physBone);
                    collector.AddDependencies(weightedPBObjects.Select(t => (Component)t));

                    if (context.PhysBoneCollider)
                    {
                        foreach (VRCPhysBoneCollider collider in physBone.colliders)
                        {
                            if (collider == null) continue;
                            collector.AddDependency(collider);
                            collector.AddDependency(collider.GetTarget());
                        }
                    }
                }
            }
        }

        private static HashSet<Transform> GetWeightedPhysBoneObjects(VRCPhysBone physBone, HashSet<Transform> weightedBones)
        {
            var WeightedPhysBoneObjects = new HashSet<Transform>();

            var allchildren = UnityUtils.GetAllChildren(physBone.GetTarget());

            foreach (Transform child in allchildren)
            {
                if (weightedBones.Contains(child))
                {
                    var result = new HashSet<Transform>();
                    SingleChainRecursive(child, result);
                    WeightedPhysBoneObjects.UnionWith(result);
                }
            }

            return WeightedPhysBoneObjects;
        }

        private static void SingleChainRecursive(Transform transform, HashSet<Transform> result)
        {   
            result.Add(transform);   
            if (transform.childCount == 1)
            {
                Transform child = transform.GetChild(0);
                SingleChainRecursive(child, result);
            }
        }
    }

    internal class ConstraintDependency : IComponentDependency
    {
        public bool IsEnabled(RendererDepedencyProviderContext context)
        {
            return context.Constraints;
        }

        public void AddDependency(RendererDepedencyProviderContext context, RendererDepedencyCollector collector)
        {
            var root = context.Root.transform;
            var weightedBones = context.WeightedBones;

            var informations = new List<ConstraintInformation>();

            // UnityConstraintsに関する情報を収集
            var unityConstraints = root.GetComponentsInChildren<IConstraint>(true);
            foreach (var unityConstraint in unityConstraints)
            {
                var target = (unityConstraint as Component).transform;
                var sources = Enumerable.Range(0, unityConstraint.sourceCount)
                    .Select(i => unityConstraint.GetSource(i).sourceTransform)
                    .Where(s => s != null)
                    .ToHashSet();
                informations.Add(new ConstraintInformation(root, target, sources));
            }

            // VRCConstraintsに関する情報を収集
            var vrcConstraints = root.GetComponentsInChildren<VRCConstraintBase>(true);
            foreach (var vrcConstraint in vrcConstraints)
            {
                var target = vrcConstraint.GetTarget();
                var souces = vrcConstraint.Sources
                    .Select(s => s.SourceTransform)
                    .Where(s => s != null)
                    .ToHashSet();
                informations.Add(new ConstraintInformation(root, target, souces));
            }

            var validBones = weightedBones.SelectMany(b => GetParents(root, b)).ToHashSet();

            float timeout = 10f;
            float startTime = UnityEngine.Time.realtimeSinceStartup;
            while (true)
            {
                // 有効なConstraint Sources
                var validSources = new HashSet<Transform>();
                foreach (var information in informations)
                {   
                    // 残すべきcontraintかどうかの判定
                    if (information.TargetChildren.Overlaps(validBones))
                    {
                        collector.AddDependency(information.Target);

                        var sourceParents = information.Sources
                            .SelectMany(s => s.SourceParents);

                        collector.AddDependencies(sourceParents
                            .Select(t => t.transform));

                        validSources.UnionWith(sourceParents);
                    }
                }
                // Constraintを通しメッシュに影響するボーンが新規発見された場合
                // 追加した上で再度実行
                // 新規で見つからない場合は終了
                if (validSources.Except(validBones).Count() > 0)
                {
                    validBones.UnionWith(validSources);
                }
                else
                {
                    break;
                }

                // タイムアウト
                if (UnityEngine.Time.realtimeSinceStartup - startTime >= timeout)
                {
                    throw new Exception("TraceConstraints: Timeout");
                }
            }
            return;
        }

        class ConstraintInformation
        {
            public Transform Target;
            public HashSet<Transform> TargetChildren;
            public HashSet<ConstraintSourceInformation> Sources;

            public ConstraintInformation(Transform root, Transform target, HashSet<Transform> sources)
            {
                Target = target;
                TargetChildren = UnityUtils.GetAllChildren(target)
                    .ToHashSet();
                Sources = sources
                    .Select(s => new ConstraintSourceInformation(
                        Source: s,
                        SourceParents: GetParents(root, s)
                    )).ToHashSet();
            }
        }
        private static HashSet<Transform> GetParents(Transform root, Transform target)
        {
            var parents = new HashSet<Transform>();
            var current = target;
            while (current != null && current != root)
            {
                parents.Add(current);
                current = current.parent;
            }
            if(current == root)
            {
                parents.Add(current);
            }
            return parents;
        }

        struct ConstraintSourceInformation
        {
            public Transform Source;
            public HashSet<Transform> SourceParents;

            public ConstraintSourceInformation(Transform Source, HashSet<Transform> SourceParents)
            {
                this.Source = Source;
                this.SourceParents = SourceParents;
            }
        }

    }
}