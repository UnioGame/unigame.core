namespace UniModules.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using Object = UnityEngine.Object;

    public partial class AssetEditorTools
    {
        #region Asset Importers

        public static List<AssetImporter> GetActiveAssets(bool foldersOnly = true)
        {
            var assets  = new List<AssetImporter>();
            var targets = Selection.objects;
            for (int i = 0; i < targets.Length; i++) {
                var target = targets[i];
                var path   = AssetDatabase.GetAssetPath(target);
                var asset  = foldersOnly ? GetDirectoryImporter(path) : AssetImporter.GetAtPath(path);
                if (asset) {
                    assets.Add(asset);
                }
            }

            return assets;
        }

        
        public static AssetImporter GetDirectoryImporter(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            return AssetImporter.GetAtPath(directory);
        }

        public static List<AssetImporter> GetAssetImporters(string item, string[] folders = null, bool foldersOnly = false)
        {
            var assetImporters = new List<AssetImporter>();
            var filterText     = item;
            var ids            = AssetDatabase.FindAssets(filterText, folders);
            for (int i = 0; i < ids.Length; i++) {
                var id   = ids[i];
                var path = AssetDatabase.GUIDToAssetPath(id);
                if (foldersOnly && Directory.Exists(path) == false)
                    continue;
                var importer = AssetImporter.GetAtPath(path);
                assetImporters.Add(importer);
            }

            return assetImporters;
        }

        public static List<AssetImporter> GetAssetImporters<T>(string[] folders = null, bool foldersOnly = false) where T : Object
        {
            return GetAssetImporters($"t:{typeof(T).Name}", folders, foldersOnly);
        }

        public static AssetImporter GetAssetImporter(Object asset)
        {
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset));
            return importer;
        }
        
        public static List<AssetImporter> GetAssetImporters(Type targetType, string[] folders = null, bool foldersOnly = false)
        {
            return GetAssetImporters(string.Format("t:{0}", targetType.Name), folders, foldersOnly);
        }

        #endregion
    }
}