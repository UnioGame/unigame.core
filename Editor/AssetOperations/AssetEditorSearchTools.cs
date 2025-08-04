using System.IO;
using System.Text.RegularExpressions;
using UniGame.Runtime.ObjectPool;
using UniGame.Runtime.ObjectPool.Extensions;
using UniGame.Runtime.Utils;

namespace UniModules.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::UniGame.Core.Runtime.Extension;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public partial class AssetEditorTools
    {
        #region asset loading

        public readonly static List<string> assetExtensions = new List<string>()
        {
            ".prefab", ".unity", ".asset"
        };

        public static MemorizeItem<(string filter, string[] location), string[]> AssetFilterGuidMap =
            MemorizeTool.Memorize<(string filter, string[] location), string[]>(
                x =>
                {
                    var filter = x.filter;
                    var folders = x.location;
                    return GetAssetFilterGuids(filter, folders);
                });

        public const string guidRegExpr = @"(guid:(\s)* (?<guid_group>[\w|\d]*))";
        public const string guidGroupName = @"guid_group";
        public const string FilterTemplate = "t: {0} {1}";

        
        
        static AssetEditorTools()
        {
            GuidRegexpr = new Regex(guidRegExpr,
                RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);
        }

        public static MemorizeItem<string, List<string>> guidsReferencies = MemorizeTool.Memorize<string, List<string>>(
            path => { return GetDependenciesNonCached(path); });

        public readonly static Regex GuidRegexpr;
        
        private static List<Object> _emptyAssetsList = new List<Object>();
        private static string[] EmptyDirFilter = new string[0];

        public static List<Object> GetAssets(Type assetType, string[] folders = null, int count = 0)
        {
            var filterText = GetTypeFilter(assetType);
            var assets = GetAssets(assetType, new List<Object>(), filterText, folders, count);

            return assets;
        }

        public static string[] GetAssetFilterGuids(string filter, string[] folders,bool cached)
        {
            return cached
                ? AssetFilterGuidMap.GetValue((filter, folders))
                : GetAssetFilterGuids(filter, folders);
        }
        
        public static void GetAssetsPaths(string filter, string[] folders, List<string> result)
        {
            var ids = GetAssetFilterGuids(filter, folders);
            foreach (var id in ids)
            {
                var path = AssetDatabase.GUIDToAssetPath(id);
                result.Add(path);
            }
        }

        public static string[] GetAssetFilterGuids(string filter,params string[] folders)
        {
            var ids = folders is not { Length: > 0 }
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter,folders);
            
            return ids;
        }
        
        public static string GetTypeFilter<T>()
        {
            return GetTypeFilter(typeof(T));
        }

        public static string GetTypeFilter(Type filter)
        {
            var filterText = $"t:{filter.Name}";
            return filterText;
        }

        public static IEnumerable<string> GetDependenciesFromAsset(string path)
        {
            return guidsReferencies[path];
        }

        public static List<string> GetDependenciesNonCached(string path)
        {
            var result = new List<string>();
            var globalPath = path.ToAbsoluteProjectPath();
            if (!File.Exists(globalPath))
                return result;
            var fileExt = Path.GetExtension(path);
            if (!assetExtensions.Any(x => x.Equals(fileExt, StringComparison.InvariantCultureIgnoreCase)))
                return result;

            var content = File.ReadAllText(globalPath);
            var matchResult = GuidRegexpr.Matches(content);
            foreach (Match match in matchResult)
            {
                if (!match.Success)
                    continue;

                var group = match.Groups[guidGroupName];
                if (group == null) continue;

                var matches = group.Captures;
                foreach (Capture capture in matches)
                {
                    var guid = capture.Value;
                    if (string.IsNullOrEmpty(guid))
                        continue;
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(assetPath)) continue;
                    result.Add(assetPath);
                }
            }

            return result;
        }

        public static Object GetAsset(string filter, string[] folders = null)
        {
            return GetAsset(UnityTypeExtension.assetType,filter, folders);
        }
        
        public static Object GetAsset(Type type, string[] folders = null)
        {
            return GetAsset(GetTypeFilter(type), folders);
        }
        
        public static List<Object> GetAssets(Type type, string filter, string[] folders = null)
        {
            var isComponent = type.IsComponent();
            if (isComponent)
                return GetComponentsAssets(type, filter, folders);

            var filterValue = CreateFilter(type, filter);

            return GetAssets(filterValue, folders).Where(x => x && type.IsInstanceOfType(x)).ToList();
        }

        public static string CreateFilter(Type type, string filter)
        {
            //check is filter already contains type
            if (!string.IsNullOrEmpty(filter))
            {
                if (filter.Contains("t:")) return filter;
            }
            var targetFilter  =string.Format(FilterTemplate, type.Name, filter);
            return targetFilter;
        }
        
        public static List<Object> GetAssets(string filter, string[] folders = null, int count = 0)
        {
            if (string.IsNullOrEmpty(filter))
                return new List<Object>();

            var path = AssetDatabase.GUIDToAssetPath(filter);

            return !string.IsNullOrEmpty(path)
                ? new List<Object>() { AssetDatabase.LoadAssetAtPath<Object>(path) }
                : GetAssets<Object>(filter, folders, count);
        }

        public static List<T> GetAssets<T>(string filter, string[] folders = null, int count = 0) where T : Object
        {
            var sourceList = ClassPool.Spawn<List<Object>>();

            var assets = GetAssets(typeof(T), sourceList, filter, folders, count);

            var result = assets
                .OfType<T>()
                .ToList();
            sourceList.Despawn();
            return result;
        }

        public static List<T> GetAssetsByPaths<T>(List<string> paths) where T : Object
        {
            var assets = new List<T>();
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (!asset) continue;
                assets.Add(asset);
            }

            return assets;
        }
        
        public static void GetAssetsPaths(Type type, List<string> resultContainer, string[] folders = null)
        {
            var filter = GetTypeFilter(type);
            var isComponent = type.IsComponent();
            var filterType = isComponent ? UnityTypeExtension.gameObjectType : type;
            var searchFilter = CreateFilter(filterType, filter);
            var ids = GetAssetFilterGuids(searchFilter,folders,false);
            
            GetAssetsPaths(searchFilter, ids,resultContainer);
        }

        public static List<Object> GetAssets(
            Type type, 
            List<Object> resultContainer, 
            string filter,
            string[] folders = null, 
            int count = 0)
        {
            var isComponent = type.IsComponent();
            var filterType = isComponent ? UnityTypeExtension.gameObjectType : type;
            var searchFilter = CreateFilter(filterType, filter);
            var ids = GetAssetFilterGuids(searchFilter,folders,false);
            count = count <= 0 ? int.MaxValue : count;
            
            foreach (var id in ids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(id);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogErrorFormat("Asset importer {0} with NULL path detected", id);
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
                if(!asset) continue;
                if (isComponent && asset is GameObject gameObject)
                {
                    var component = gameObject.GetComponent(type);
                    if (!component) continue;
                }
                
                resultContainer.Add(asset);
                if (resultContainer.Count >= count) break;
            }
            
            return resultContainer;
        }

        public static  List<AssetEditorInfo> GetAssetsInfo(
            Type type, 
            string filter,
            string[] folders = null, 
            int count = 0)
        {
            var result = new List<AssetEditorInfo>();
            var isComponent = type.IsComponent();
            var targetType = isComponent ? UnityTypeExtension.gameObjectType : type;
            var searchFilter = CreateFilter(targetType, filter);
            var ids = GetAssetFilterGuids(searchFilter,folders,false);
            
            count = count <= 0 ? int.MaxValue : count;
            
            foreach (var id in ids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(id);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogErrorFormat("Asset importer {0} with NULL path detected", id);
                    continue;
                }
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
                if (!asset) continue;

                if (isComponent && 
                    asset is GameObject gameObject)
                {
                    var component = gameObject.GetComponent(type);
                    if (!component) continue;
                }
 
                result.Add(new AssetEditorInfo()
                {
                    guid = id,
                    path = assetPath,
                    asset = asset,
                    assetType = type
                });
                
                if (result.Count >= count) break;
            }
            
            return result;
        }
        


        public static Object GetAsset(Type type, string filter, string[] folders = null)
        {
            var searchFilter = CreateFilter(type, filter);
            var ids = GetAssetFilterGuids(searchFilter,folders,false);
    
            foreach (var id in ids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(id);
                if (string.IsNullOrEmpty(assetPath)) continue;
                
                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                if(!fileName.Equals(filter,StringComparison.OrdinalIgnoreCase))
                    continue;
                
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
                if (asset != null) return asset;
            }
            
            foreach (var id in ids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(id);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var asset = AssetDatabase.LoadAssetAtPath(assetPath, type);
                if (asset != null) return asset;
            }

            return null;
        }

        /// <summary>
        /// load components
        /// </summary>
        /// <param name="type">component type</param>
        /// <param name="filter"></param>
        /// <param name="folders">folder filter</param>
        /// <returns>list of found items</returns>
        public static List<Object> GetComponentsAssets(Type type, string filter = "", string[] folders = null)
        {
            if (type.IsComponent() == false) return new List<Object>();

            var filterText =  CreateFilter(UnityTypeExtension.gameObjectType, filter);
            var assets = GetAssets<GameObject>(filterText, folders);
            var resultAssets = new List<Object>();

            foreach (var t in assets)
            {
                var targetComponents = t.GetComponents(type);
                resultAssets.AddRange(targetComponents);
            }

            return resultAssets;
        }

        #endregion
    }
    
    [Serializable]
    public struct AssetEditorInfo
    {
        public string guid;
        public string path;
        public Object asset;
        public Type assetType;
    }
}