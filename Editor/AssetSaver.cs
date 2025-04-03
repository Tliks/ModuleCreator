using System.IO;
using UnityEditor;

namespace com.aoyon.modulecreator
{
    internal static class AssetSaver
    {
        private const string BASEPATH = "Assets/ModuleCreator";
        
        public static string GenerateMeshPath(string folderName, string fileName)
        {
            return GenerateVaildPath($"{BASEPATH}/{folderName}/Mesh", fileName, "asset");
        }

        public static string GeneratePrefabPath(string folderName, string fileName)
        {
            return GenerateVaildPath($"{BASEPATH}/{folderName}/Prefab", fileName, "prefab");
        }

        public static string GenerateVaildPath(string folderpath, string fileName, string fileExtension)
        {
            CreateDirectory(folderpath);
            string path = folderpath + "/" + fileName + "." + fileExtension;
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        public static void CreateDirectory(string folderpath)
        {
            if (!Directory.Exists(folderpath)) 
            {
                Directory.CreateDirectory(folderpath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

    }
}