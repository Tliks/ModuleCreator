using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using com.aoyon.triangleselector;
using com.aoyon.triangleselector.utils;
using static com.aoyon.modulecreator.LocalizationEditor;

namespace com.aoyon.modulecreator
{   

    public class ModuleCreator : EditorWindow
    {
        private GameObject _root;
        private List<SkinnedMeshRenderer> _skinnedMeshRenderers;
        private List<List<Vector3>> _targetselections = new();
        private RenderSelector[] _renderSelectors;

        private Vector2 scrollPosition;
        private static ModuleCreatorOptions _options;
        private static bool _showAdvancedOptions = false;

        public static void ShowWindow(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            ModuleCreator window = GetWindow<ModuleCreator>();
            window.Initialize(skinnedMeshRenderers);
            window.Show();
        }

        private void Initialize(SkinnedMeshRenderer[] skinnedMeshRenderers)
        {
            _skinnedMeshRenderers = skinnedMeshRenderers.ToList();
            _options = new();
            _root = TraceObjects.CheckTargets(_skinnedMeshRenderers.Select(r => r.gameObject));

            int rendererCount = _skinnedMeshRenderers.Count;
            _renderSelectors = new RenderSelector[rendererCount];
            _targetselections = new List<List<Vector3>>(rendererCount);
            for (int i = 0; i < rendererCount; i++)
            {
                var renderSelector = CreateInstance<RenderSelector>();
                var targetSelection = new List<Vector3>();
                renderSelector.Initialize(_skinnedMeshRenderers[i], targetSelection, "default (100%)");
                int currentIndex = i;
                renderSelector.RegisterApplyCallback(newSelection => _targetselections[currentIndex] = newSelection);
                _renderSelectors[i] = renderSelector;
                _targetselections.Add(targetSelection);
            }
        } 

        void OnDestroy()
        {
            foreach(var renderSelector in _renderSelectors)
            {
                renderSelector.Dispose();
            }
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // 冒頭
            LocalizationEditor.RenderLocalize();
            EditorGUILayout.HelpBox(GetLocalizedText("Utility.ModuleCreator.description"), MessageType.Info);
            
            EditorGUILayout.Space();

            // 各Rendererに対するUI
            for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    float width3 = 70f;
                    float margin = 14f;
                    float remainingWidth = position.width - width3 - margin;

                    float width1 = remainingWidth * 0.55f;
                    float width2 = remainingWidth * 0.45f;

                    GUI.enabled = TriangleSelector.Disposed;
                    _skinnedMeshRenderers[i] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(_skinnedMeshRenderers[i], typeof(SkinnedMeshRenderer), true, GUILayout.Width(width1));
                    _renderSelectors[i].RenderTriangleSelection(new GUILayoutOption[]{ GUILayout.Width(width2)});
                    GUI.enabled = true;

                    string[] labels = new string[]{ "Edit", "Edit", "Close" };
                    GUILayoutOption[] options = new GUILayoutOption[]{ GUILayout.Width(width3), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)};
                    _renderSelectors[i].RenderEditSelection(labels, options);
                }
            }

            EditorGUILayout.Space();

            // オプション
            _options.IncludePhysBone = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.PhysBoneToggle"), _options.IncludePhysBone);

            GUI.enabled = _options.IncludePhysBone;
            _options.IncludePhysBoneColider = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.PhysBoneColiderToggle"), _options.IncludePhysBoneColider);
            GUI.enabled = true;

            EditorGUILayout.Space();

            GUI.enabled = _skinnedMeshRenderers.Count > 1;
            using (new GUILayout.HorizontalScope())
            {
                _options.MergePrefab = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.MergePrefab"), _options.MergePrefab);
                RenderInfo(GetLocalizedText("Utility.ModuleCreator.MergePrefab.tooltip"));
            }
            GUI.enabled = true;
            
            GUI.enabled = _skinnedMeshRenderers.Count == 1 && _targetselections.Any(s => s.Count > 0);
            using (new GUILayout.HorizontalScope())
            {
                _options.Outputunselected = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.OutputUnselcted"), _options.Outputunselected);
                RenderInfo(GetLocalizedText("Utility.ModuleCreator.OutputUnselcted.tooltip"));
            }
            GUI.enabled = true;

            // 実行ボタン
            EditorGUILayout.Space();
            GUI.enabled = _skinnedMeshRenderers != null & _skinnedMeshRenderers.Count() > 0;
            if (GUILayout.Button(GetLocalizedText("Utility.ModuleCreator.CreateModuleButton")))
            {
                CreateModule();
                Close();
                GUIUtility.ExitGUI(); 
            }
            GUI.enabled = true;


            // 高度なオプション
            EditorGUILayout.Space();
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, GetLocalizedText("Utility.ModuleCreator.advancedoptions"));
            if (_showAdvancedOptions)
            { 
                GUI.enabled = _options.IncludePhysBone;
                using (new GUILayout.HorizontalScope())
                {
                    _options.RemainAllPBTransforms = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.AdditionalTransformsToggle"), _options.RemainAllPBTransforms);
                    RenderInfo(GetLocalizedText("Utility.ModuleCreator.tooltip.AdditionalTransformsToggle"));
                }                

                using (new GUILayout.HorizontalScope())
                {
                    _options.IncludeIgnoreTransforms = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.IncludeIgnoreTransformsToggle"), _options.IncludeIgnoreTransforms);
                    RenderInfo(GetLocalizedText("Utility.ModuleCreator.tooltip.IncludeIgnoreTransformsToggle"));
                }                

                using (new GUILayout.HorizontalScope())
                {
                    _options.RenameRootTransform = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.RenameRootTransformToggle"), _options.RenameRootTransform);
                    RenderInfo(GetLocalizedText("Utility.ModuleCreator.tooltip.RenameRootTransformToggle"));
                }                

                GUI.enabled = true;

                using (new GUILayout.HorizontalScope())
                {
                    _options.RootObject = (GameObject)EditorGUILayout.ObjectField(GetLocalizedText("Utility.ModuleCreator.SpecifyRootObjectLabel"), _options.RootObject, typeof(GameObject), true);
                    RenderInfo(GetLocalizedText("Utility.ModuleCreator.tooltip.SpecifyRootObjectLabel"));
                }                
            }

            EditorGUILayout.EndScrollView();
            
        }

        private void CreateModule()
        {
            if (_options.MergePrefab)
            {
                string mesh_name = $"{_root.name} Parts";
                (GameObject new_root, string variantPath) = ModuleCreatorProcessor.SaveRootObject(_root, mesh_name);
                new_root.transform.position = Vector3.zero;

                List<SkinnedMeshRenderer> newskinnedMeshRenderers = TraceObjects.TraceCopiedRenderers(_root, new_root, _skinnedMeshRenderers).ToList();

                for (int i = 0; i < newskinnedMeshRenderers.Count(); i++)
                {
                    ProcessMesh(newskinnedMeshRenderers[i], _targetselections[i], true);
                }

                ModuleCreatorProcessor.CreateModule(new_root, newskinnedMeshRenderers, _options, _root.scene);
                Debug.Log("Saved prefab to " + variantPath);
            }
            else
            {
                if (_options.Outputunselected && _skinnedMeshRenderers.Count == 1 && _targetselections[0].Count > 0)
                {
                    CreateSingleModule(_skinnedMeshRenderers[0], _targetselections[0], _skinnedMeshRenderers[0].name, true);
                    CreateSingleModule(_skinnedMeshRenderers[0], _targetselections[0], $"{_skinnedMeshRenderers[0].name} Unselected", false);
                    return;
                }

                for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
                {
                    CreateSingleModule(_skinnedMeshRenderers[i], _targetselections[i], _skinnedMeshRenderers[i].name, true);
                }
            }
        }

        private void CreateSingleModule(SkinnedMeshRenderer skinnedMeshRenderer, IEnumerable<Vector3> positioins, string meshName, bool KeepMesh)
        {
            (GameObject newRoot, string variantPath) = ModuleCreatorProcessor.SaveRootObject(_root, meshName);
            newRoot.transform.position = Vector3.zero;

            var newSkinnedMeshRender = TraceObjects.TraceCopiedRenderer(_root, newRoot, skinnedMeshRenderer);

            ProcessMesh(newSkinnedMeshRender, positioins, KeepMesh);

            ModuleCreatorProcessor.CreateModule(newRoot, new List<SkinnedMeshRenderer> { newSkinnedMeshRender }, _options, _root.scene);
            Debug.Log("Saved prefab to " + variantPath);
        }

        private void ProcessMesh(SkinnedMeshRenderer skinnedMeshRenderer, IEnumerable<Vector3> positioins, bool KeepMesh)
        {
            if (positioins.Count() > 0)
            {
                Mesh newMesh = KeepMesh 
                    ? MeshHelper.KeepMesh(skinnedMeshRenderer.sharedMesh, positioins) 
                    : MeshHelper.DeleteMesh(skinnedMeshRenderer.sharedMesh, positioins);

                string path = AssetPathUtility.GenerateMeshPath(_root.name, $"{skinnedMeshRenderer.name}_modified");
                AssetDatabase.CreateAsset(newMesh, path);
                AssetDatabase.SaveAssets();
                skinnedMeshRenderer.sharedMesh = newMesh;
            }
        }

        private void RenderInfo(string label)
        {
            GUIContent infoContent = new GUIContent(EditorGUIUtility.IconContent("_Help"));
            infoContent.tooltip = label;
            GUILayoutOption[] options = {GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)};
            GUILayout.Label(infoContent, options);
        }
                
    }
}