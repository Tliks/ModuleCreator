using System.Collections.Generic;
using com.aoyon.triangleselector.utils;

namespace com.aoyon.modulecreator
{
    public class LocalizationEditor
    {
        
        private static Dictionary<string, string[]> _LocalizedText = new Dictionary<string, string[]>
        {
            { "Utility.ModuleCreator.description", new string[] { "Generate a Prefab containing the mesh of the selection.", "選択した箇所のメッシュを含むPrefabを生成します。" }},
            { "Utility.ModuleCreator.advancedoptions", new string[] {"Advanced Options", "高度なオプション"}},
            { "Utility.ModuleCreator.CreateModuleButton", new string[] { "Create Module", "モジュールを作成" } },
            { "Utility.ModuleCreator.OutputUnselcted", new string[] { "Include Unselected Module", "未選択のモジュールも含める" } },
            { "Utility.ModuleCreator.MergePrefab", new string[] { "Combine into a Single Prefab", "一つのPrefabにまとめる" } },
            { "Utility.ModuleCreator.PhysBoneToggle", new string[] { "PhysBone", "PhysBone" } },
            { "Utility.ModuleCreator.PhysBoneColiderToggle", new string[] { "PhysBoneColider", "PhysBoneColider" } },
            { "Utility.ModuleCreator.AdditionalTransformsToggle", new string[] { "Additional Transforms", "追加のTransform" } },
            { "Utility.ModuleCreator.IncludeIgnoreTransformsToggle", new string[] { "Include IgnoreTransforms", "IgnoreTransformsを含める" } },
            { "Utility.ModuleCreator.RenameRootTransformToggle", new string[] { "Rename RootTransform", "RootTransformの名前を変更" } },
            { "Utility.ModuleCreator.SpecifyRootObjectLabel", new string[] { "Specify Root Object", "ルートオブジェクトを指定" } },
            { "Utility.ModuleCreator.tooltip.AdditionalTransformsToggle", new string[] { "Output Additional PhysBones Affected Transforms for exact PhysBone movement", "正確なPhysBoneの動きのために追加のPhysBones Affected Transformsを出力" } },
            { "Utility.ModuleCreator.tooltip.IncludeIgnoreTransformsToggle", new string[] { "Output PhysBone's IgnoreTransforms", "PhysBoneのIgnoreTransformsを出力する" } },
            { "Utility.ModuleCreator.tooltip.RenameRootTransformToggle", new string[] { "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.", "推奨されません。モジュラーアバターの仕様により、場合によっては衣装側の物理ボーンが削除されることがあります。そのため、PhysBoneのRootTransformの名前を変更することで、衣装側のPhysBoneが確実に統合されるようにします。これにより重複が発生する可能性があります。" } },
            { "Utility.ModuleCreator.tooltip.SpecifyRootObjectLabel", new string[] { "The default root object is the parent object of the specified skinned mesh renderer object", "デフォルトのルートオブジェクトは、指定されたSkinned Mesh Rendererがついたオブジェクトの親オブジェクトです" } },
        };
        
        private const string PreferenceKey = "com.aoyon.modulecreator.lang";
        private static int selectedLanguageIndex = LocalizationManager.GetSelectedLanguageIndex(PreferenceKey);

        public static string GetLocalizedText(string key)
        {
            return LocalizationManager.GetLocalizedText(_LocalizedText, key, selectedLanguageIndex);
        }

        public static void RenderLocalize()
        {
            LocalizationManager.RenderLocalize(ref selectedLanguageIndex, PreferenceKey);
        }

    }
}