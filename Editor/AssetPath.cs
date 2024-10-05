using com.aoyon.triangleselector.utils;

namespace com.aoyon.modulecreator
{
    public static class AssetPathUtility
    {
        private const string BASEPATH = "Assets/ModuleCreator";
        
        public static string GenerateMeshPath(string root_name, string mesh_name)
        {
            string folderpath = $"{BASEPATH}/{root_name}/Mesh";
            string fileName = mesh_name;
            string fileExtension = "asset";
            
            return AssetUtility.GenerateVaildPath(folderpath, fileName, fileExtension);
        }

        public static string GeneratePrefabPath(string root_name, string mesh_name)
        {
            string folderpath =  $"{BASEPATH}/{root_name}/Prefab";;
            string fileName = mesh_name;
            string fileExtension = "prefab";
            
            return AssetUtility.GenerateVaildPath(folderpath, fileName, fileExtension);
        }

    }
}