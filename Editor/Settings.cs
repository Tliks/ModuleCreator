using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    [FilePath("ProjectSettings/Packages/com.aoyon.modulecreator/settings.json", FilePathAttribute.Location.ProjectFolder)]
    public class ModuleCreatorSettings : ScriptableSingleton<ModuleCreatorSettings>
    {
        [InitializeOnLoadMethod]
        private static void Init()
        {
            _ = instance;
        }
        private static void SetValue<T>(ref T field, T value)
        {
            if (!Equals(field, value))
            {
                field = value;
                instance.Save(true);
            }
        }

        [SerializeField]
        private bool includePhysBone = true;

        [SerializeField]
        private bool includePhysBoneColider = true;

        [SerializeField]
        private bool includeConstraints = true;

        [SerializeField]
        private bool unpackPrefabToOrigin = true;

        public static bool IncludePhysBone
        {
            get => instance.includePhysBone;
            set => SetValue(ref instance.includePhysBone, value);
        }

        public static bool IncludePhysBoneColider
        {
            get => instance.includePhysBoneColider;
            set => SetValue(ref instance.includePhysBoneColider, value);
        }

        public static bool IncludeConstraints
        {
            get => instance.includeConstraints;
            set => SetValue(ref instance.includeConstraints, value);
        }

        public static bool UnpackPrefabToOrigin
        {
            get => instance.unpackPrefabToOrigin;
            set => SetValue(ref instance.unpackPrefabToOrigin, value);
        }
    }
}