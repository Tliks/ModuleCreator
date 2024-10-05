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
            
            // 各Rendererに対するUI
            EditorGUILayout.Space();

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


            // オプション
            EditorGUILayout.Space();
        
            _options.IncludePhysBone = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.PhysBoneToggle"), _options.IncludePhysBone);

            GUI.enabled = _options.IncludePhysBone;
            _options.IncludePhysBoneColider = EditorGUILayout.Toggle(GetLocalizedText("Utility.ModuleCreator.PhysBoneColiderToggle"), _options.IncludePhysBoneColider);
            GUI.enabled = true;

            EditorGUILayout.Space();

            GUI.enabled = _skinnedMeshRenderers.Count > 1;
            _options.MergePrefab = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.MergePrefab"), _options.MergePrefab);
            GUI.enabled = true;
            
            GUI.enabled = !_options.MergePrefab && _targetselections.Any(s => s.Count > 0);
            _options.Outputunselected = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.OutputUnselcted"), _options.Outputunselected);
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

                GUIContent content_at = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.AdditionalTransformsToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.AdditionalTransformsToggle"));
                _options.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, _options.RemainAllPBTransforms);

                GUIContent content_ii = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.IncludeIgnoreTransformsToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.IncludeIgnoreTransformsToggle"));
                _options.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, _options.IncludeIgnoreTransforms);

                GUIContent content_rr = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.RenameRootTransformToggle"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.RenameRootTransformToggle"));
                _options.RenameRootTransform = EditorGUILayout.Toggle(content_rr, _options.RenameRootTransform);

                GUI.enabled = true;

                GUIContent content_sr = new GUIContent(
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.SpecifyRootObjectLabel"),
                    LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.tooltip.SpecifyRootObjectLabel"));
                _options.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, _options.RootObject, typeof(GameObject), true);    
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
                    var triangleindies = _targetselections[i];
                    if (triangleindies.Count() > 0)
                    {
                        Mesh newMesh = MeshHelper.KeepMesh(newskinnedMeshRenderers[i].sharedMesh, triangleindies.ToHashSet());
                        string path = AssetPathUtility.GenerateMeshPath(_root.name, "PartialMesh");
                        AssetDatabase.CreateAsset(newMesh, path);
                        AssetDatabase.SaveAssets();
                        newskinnedMeshRenderers[i].sharedMesh = newMesh;
                        //MeshHelper.RemoveUnusedMaterials(newskinnedMeshRenderers[i]);
                    }
                }

                ModuleCreatorProcessor.CreateModule(new_root, newskinnedMeshRenderers, _options, _root.scene);
                Debug.Log("Saved prefab to " + variantPath);
            }
            else
            {
                for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
                {
                    ProcessMeshRenderer(_skinnedMeshRenderers[i], _targetselections[i], false);

                    if (_options.Outputunselected)
                    {
                        ProcessMeshRenderer(_skinnedMeshRenderers[i], _targetselections[i], true);
                    }
                }
            }

        }

        private void ProcessMeshRenderer(SkinnedMeshRenderer renderer, IEnumerable<Vector3> triangleIndices, bool outputUnselected)
        {
            string meshName = renderer.name + (outputUnselected ? " Other" : "");
            (GameObject newRoot, string variantPath) = ModuleCreatorProcessor.SaveRootObject(_root, meshName);
            newRoot.transform.position = Vector3.zero;

            var newSkinnedMeshRender = TraceObjects.TraceCopiedRenderer(_root, newRoot, renderer);
            Mesh newMesh = outputUnselected 
                ? MeshHelper.DeleteMesh(newSkinnedMeshRender.sharedMesh, triangleIndices) 
                : MeshHelper.KeepMesh(newSkinnedMeshRender.sharedMesh, triangleIndices);

            string path = AssetPathUtility.GenerateMeshPath(_root.name, "PartialMesh");
            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.SaveAssets();
            newSkinnedMeshRender.sharedMesh = newMesh;
            //MeshHelper.RemoveUnusedMaterials(newSkinnedMeshRender);
            ModuleCreatorProcessor.CreateModule(newRoot, new List<SkinnedMeshRenderer> { newSkinnedMeshRender }, _options, _root.scene);
            Debug.Log("Saved prefab to " + variantPath);
        }
                
    }
}