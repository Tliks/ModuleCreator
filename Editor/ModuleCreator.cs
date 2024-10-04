using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using com.aoyon.triangleselector;
using com.aoyon.triangleselector.utils;


namespace com.aoyon.modulecreator
{   

    public class ModuleCreator : EditorWindow
    {
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

            int rendererCount = _skinnedMeshRenderers.Count;
            _renderSelectors = new RenderSelector[rendererCount];
            _targetselections = new List<List<Vector3>>(rendererCount);
            for (int i = 0; i < rendererCount; i++)
            {
                var renderSelector = CreateInstance<RenderSelector>();
                var targetSelection = new List<int>();
                renderSelector.Initialize(_skinnedMeshRenderers[i], targetSelection, "(100%)%");
                renderSelector.RegisterApplyCallback(newSelection => _targetselections[i] = newSelection);
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
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.description"), MessageType.Info);
            
            // 各Rendererに対するUI
            EditorGUILayout.Space();
            for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
            {
                using (new GUILayout.HorizontalScope())
                {
                    float width3 = 80f;
                    float margin = 14f;
                    float remainingWidth = position.width - width3 - margin;

                    float width1 = remainingWidth * 0.65f;
                    float width2 = remainingWidth * 0.35f;

                    _skinnedMeshRenderers[i] = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(_skinnedMeshRenderers[i], typeof(SkinnedMeshRenderer), true, GUILayout.Width(width1));

                    _renderSelectors[i].RenderTriangleSelection(new GUILayoutOption[]{ GUILayout.Width(width2)});

                    string[] labels = new string[]{ "Edit", "Edit", "Close" };
                    GUILayoutOption[] options = new GUILayoutOption[]{ GUILayout.Width(80f), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)};
                    _renderSelectors[i].RenderEditSelection(labels, options);
                }
            }


            // オプション
            EditorGUILayout.Space();
        
            _options.IncludePhysBone = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.PhysBoneToggle"), _options.IncludePhysBone);

            GUI.enabled = _options.IncludePhysBone;
            _options.IncludePhysBoneColider = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.PhysBoneColiderToggle"), _options.IncludePhysBoneColider);
            GUI.enabled = true;

            EditorGUILayout.Space();

            _options.MergePrefab = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.MergePrefab"), _options.MergePrefab);
            _options.Outputunselected = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.OutputUnselcted"), _options.Outputunselected);


            // 実行ボタン
            EditorGUILayout.Space();
            GUI.enabled = _skinnedMeshRenderers != null & _skinnedMeshRenderers.Count() > 0;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.CreateModuleButton")))
            {
                CreateModule();
                Close();
                GUIUtility.ExitGUI(); 
            }
            GUI.enabled = true;


            // 高度なオプション
            EditorGUILayout.Space();
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, LocalizationEditor.GetLocalizedText("Utility.ModuleCreator.advancedoptions"));
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
            GameObject root = TraceObjects.CheckTargets(_skinnedMeshRenderers.Select(r => r.gameObject));

            if (_options.MergePrefab)
            {
                string mesh_name = $"{root.name} Parts";
                (GameObject new_root, string variantPath) = ModuleCreatorProcessor.SaveRootObject(root, mesh_name);
                new_root.transform.position = Vector3.zero;

                List<SkinnedMeshRenderer> newskinnedMeshRenderers = TraceObjects.TraceCopiedRenderers(root, new_root, _skinnedMeshRenderers).ToList();

                for (int i = 0; i < newskinnedMeshRenderers.Count(); i++)
                {
                    var triangleindies = _targetselections[i];
                    if (triangleindies.Count() > 0)
                    {
                        Mesh newMesh = MeshHelper.KeepMesh(newskinnedMeshRenderers[i].sharedMesh, triangleindies.ToHashSet());
                        string path = AssetPathUtility.GenerateMeshPath(root.name, "PartialMesh");
                        AssetDatabase.CreateAsset(newMesh, path);
                        AssetDatabase.SaveAssets();
                        newskinnedMeshRenderers[i].sharedMesh = newMesh;
                    }
                }

                ModuleCreatorProcessor.CreateModule(new_root, newskinnedMeshRenderers, _options, root.scene);
                Debug.Log("Saved prefab to " + variantPath);
            }
            else
            {
                for (int i = 0; i < _skinnedMeshRenderers.Count(); i++)
                {
                    string mesh_name = _skinnedMeshRenderers[i].name;
                    (GameObject new_root, string variantPath) = ModuleCreatorProcessor.SaveRootObject(root, mesh_name);
                    new_root.transform.position = Vector3.zero;

                    var newskinnedMeshRender = TraceObjects.TraceCopiedRenderer(root, new_root, _skinnedMeshRenderers[i]);
                    var triangleindies = _targetselections[i];
                    if (triangleindies.Count() > 0)
                    {
                        Mesh newMesh = MeshHelper.KeepMesh(newskinnedMeshRender.sharedMesh, triangleindies.ToHashSet());
                        string path = AssetPathUtility.GenerateMeshPath(root.name, "PartialMesh");
                        AssetDatabase.CreateAsset(newMesh, path);
                        AssetDatabase.SaveAssets();
                        newskinnedMeshRender.sharedMesh = newMesh;
                    }
                    ModuleCreatorProcessor.CreateModule(new_root, new List<SkinnedMeshRenderer>{ newskinnedMeshRender }, _options, root.scene);
                    Debug.Log("Saved prefab to " + variantPath);

                }

            }
                
        }


    }
}