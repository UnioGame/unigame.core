namespace UniModules.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UniModules.UniCore.EditorTools.Editor;
    using global::UniGame.Runtime.ReflectionUtils;
    using global::UniGame.Runtime.Utils;
    using global::UniGame.Core.Runtime.Extension;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;
    
    public static partial class AssetEditorTools
    {
        private const string PrefabExtension = "prefab";
        private const string AssetExtension  = "asset";

        private static Type         _componentType = typeof(Component);
        private static List<Object> _emptyAssets   = new List<Object>();
        private static string       clientDataPath;
        private static Regex        modificationRegex;

        private static Func<Object, EditorResource> _editorResourceFactory = MemorizeTool.Create<Object, EditorResource>(x =>
        {
            var result = new EditorResource();
            if (!x) return result;
            result.Update(x);
            return result;
        });

        public const  string       AssetRoot                = "assets";
        public const  string       ModificationTemplate     = @"\n( *)(m_Modifications:[\w,\W]*)(?=\n( *)m_RemovedComponents)";
        public static List<string> _modificationsIgnoreList = new List<string> {".fbx"};


        public static bool IsPureEditorMode =>
            EditorApplication.isPlaying == false &&
            EditorApplication.isPlayingOrWillChangePlaymode == false &&
            EditorApplication.isCompiling == false &&
            EditorApplication.isUpdating == false;

        /// <summary>
        //	Can create Scriptable object/ component /gameobject
        /// </summary>
        public static Object CreateAsset(this Type type)
        {
            Object asset = null;
            switch (type)
            {
                case Type t when t.IsScriptableObject():
                    asset = ScriptableObject.CreateInstance(type);
                    break;
                case Type t when t.IsGameObject():
                    var gameObject = new GameObject(t.Name);
                    asset = gameObject;
                    break;
                case Type t when t.IsComponent():
                    var assetObject = new GameObject(t.Name, t);
                    asset = assetObject.GetComponent(t);
                    break;
            }

            return asset;
        }

        public static string GetAssetExtension(this Object asset)
        {
            switch (asset)
            {
                case GameObject _:
                    return PrefabExtension;
                case ScriptableObject _:
                    return AssetExtension;
                case Component _:
                    return PrefabExtension;
                default:
                    return string.Empty;
            }
        }
        
        public static string GetAssetExtension(this Type asset)
        {
            if (asset.IsGameObject())
                return PrefabExtension;
            if (asset.IsComponent())
                return PrefabExtension;
            if (asset.IsScriptableObject())
                return AssetExtension;
            return string.Empty;
        }

        public static void ApplyProgressAssetAction<T>(List<T> assets, string message, Action<T> action)
        {
            var count = assets.Count;
            for (int i = 0; i < count; i++)
            {
                var asset = assets[i];
                if (asset == null) continue;
                var progress = i / ((float) count);
                EditorUtility.DisplayProgressBar($"Progress [{i} of {count}] :", asset.ToString(), progress);
                action(asset);
            }
        }
        
        public static ScriptableObject LoadOrCreate(
            Type assetType,
            string path,
            string assetName,
            Action<Object> onCreateAction = null,
            bool refreshDatabase = false)
        {
            assetName = string.IsNullOrEmpty(assetName) ? assetType.Name : assetName;

            if (TryGetAsset(assetType,path,out var  asset)) return asset as ScriptableObject;

            asset = ScriptableObject.CreateInstance(assetType);
            
            onCreateAction?.Invoke(asset);
            
            asset = asset != null 
                ? asset.SaveAsset(assetName, path, refreshDatabase) 
                : asset;
            
            return asset as ScriptableObject;
        }

        public static TAsset LoadOrCreate<TAsset>(
            TAsset asset,
            string path,
            string assetName, 
            Action<TAsset> onCreateAction = null,
            bool refreshDatabase = false) where TAsset : ScriptableObject
        {
            return asset!=null 
                ? asset 
                : LoadOrCreate<TAsset>(path, assetName,onCreateAction, refreshDatabase);
        }
        
        public static TAsset LoadOrCreate<TAsset>(string path)
            where TAsset : ScriptableObject
        {
            return LoadOrCreate<TAsset>(path, typeof(TAsset).Name, null);
        }
        
        public static TAsset LoadOrCreate<TAsset>(Type assetType,string path)
            where TAsset : ScriptableObject
        {
            return LoadOrCreate<TAsset>(path, typeof(TAsset).Name, null);
        }
        
        public static TAsset LoadOrCreate<TAsset>(string path, Action<TAsset> action)
            where TAsset : ScriptableObject
        {
            return LoadOrCreate(path, typeof(TAsset).Name, action);
        }
        
        public static TAsset LoadOrCreate<TAsset>(
            this TAsset asset,
            string path,
            Action<TAsset> action)
            where TAsset : ScriptableObject
        {
            return asset 
                ? asset 
                : LoadOrCreate(path, typeof(TAsset).Name, action);
        }

        public static TAsset LoadOrCreate<TAsset>(
            string path,
            string assetName,
            Action<TAsset> onCreateAction = null,
            bool refreshDatabase = false) where TAsset : ScriptableObject
        {
            var assetType = typeof(TAsset);
            var asset = LoadOrCreate(assetType,path,assetName,null,refreshDatabase);
            var result = asset as TAsset;
            onCreateAction?.Invoke(result);
        
            return result;
        }
        
        public static TAsset LoadOrCreate<TAsset>(
            Type assetType,
            string path,
            string assetName,
            Action<TAsset> onCreateAction = null,
            bool refreshDatabase = false) where TAsset : ScriptableObject
        {
            var asset = LoadOrCreate(assetType,path,assetName,null,refreshDatabase);
            var result = asset as TAsset;
            onCreateAction?.Invoke(result);
        
            return result;
        }

        public static bool TryGetAsset<TAsset>(string path, out TAsset asset)
            where TAsset : Object
        {
            if (TryGetAsset(typeof(TAsset), path, out var assetObject))
                asset = assetObject as TAsset;
            else
            {
                asset = null;
            }
            return asset!=null;
        }
        
        private static string[] _pathsCache = new string[1];
        public static bool TryGetAsset(Type assetType,string path, out Object asset)
        {
            _pathsCache[0] = path;
            asset = GetAsset(assetType,_pathsCache);
            
            if (asset != null) return asset != null;
            
            var fullPath = path.ToAbsoluteProjectPath();
            var fileExists = File.Exists(fullPath);
            if (!fileExists) return asset != null;
            
            Debug.LogError("File exists but AssetDatabase doesn't load it");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    
            if (asset == null)
                Debug.LogError("AssetDatabase load filed after refresh");

            return asset != null;
        }

        public static void ApplyAssetEditing(Action action)
        {
            if (action == null) return;
            try
            {
                AssetDatabase.StartAssetEditing();
                action();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
        
        public static bool OpenScript<T>(params string[] folders)
        {
            return OpenScript(typeof(T), folders);
        }

        public static bool OpenScript(this Type type, params string[] folders)
        {
            var asset = GetScriptAsset(type, folders);
            if (asset == null)
                return false;
            return AssetDatabase.OpenAsset(asset.GetInstanceID(), 0, 0);
        }

        public static MonoScript GetScriptAsset(this Type type, params string[] folders)
        {
            var typeName = type.Name;
            var filter   = $"t:script {typeName}";

            var assets = AssetDatabase.FindAssets(filter, folders);
            var assetGuid = assets.FirstOrDefault(
                x => string.Equals(typeName,
                    Path.GetFileNameWithoutExtension(x.AssetGuidToPath()),
                    StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(assetGuid))
                return null;

            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(assetGuid.AssetGuidToPath());
            return asset;
        }

        public static List<AttributeItemData<T, TAttr>> GetAssetsAndFieldsWithAttribute<T, TAttr>(string[] folders = null)
            where T : Object
            where TAttr : Attribute
        {
            var result = new List<AttributeItemData<T, TAttr>>();

            var assets = GetAssets<T>(folders);

            foreach (var asset in assets)
            {
                var type      = asset.GetType();
                var value = type.GetCustomAttributeWithChild<TAttr>();
                var attribute = value.attribute;

                if (attribute == null) continue;
                
                var targetValue = value.field!=null
                    ? value.field.GetValue(asset)
                    : asset;
                    
                result.Add(new AttributeItemData<T, TAttr>
                {
                    Attribute = attribute,
                    Value     = asset,
                    Target = targetValue
                });
            }

            return result;
        }

        public static List<AttributeItemData<TAttr>> GetAssetsAndFieldsWithAttribute<TAttr>(Type assetType,string[] folders = null)
            where TAttr : Attribute
        {
            var result = new List<AttributeItemData<TAttr>>();

            var assets = GetAssets(assetType,folders);

            foreach (var asset in assets)
            {
                var type      = asset.GetType();
                var value = type.GetCustomAttributeWithChild<TAttr>();
                var attribute = value.attribute;

                if (attribute == null) continue;
                
                var targetValue = value.field!=null
                    ? value.field.GetValue(asset)
                    : asset;
                    
                result.Add(new AttributeItemData<TAttr>
                {
                    Attribute = attribute,
                    Source     = asset,
                    Target = targetValue
                });
            }

            return result;
        }
        
        public static AttributeItemData<TAttr> GetAttributeOnSelfOrChildren<TAttr>(this object asset)
            where TAttr : Attribute
        {
            if (asset == null) return AttributeItemData<TAttr>.Empty;
            
            var type      = asset.GetType();
            var value = type.GetCustomAttributeWithChild<TAttr>();
            var attribute = value.attribute;

            if (attribute == null) return AttributeItemData<TAttr>.Empty;
                
            var targetValue = value.field!=null
                ? value.field.GetValue(asset)
                : asset;

            var result = new AttributeItemData<TAttr>
            {
                IsFound = true,
                Attribute = attribute,
                Source = asset,
                Target = targetValue
            };

            return result;
        }
        
        public static List<AttributeItemData<T, TAttr>> GetAssetsWithAttribute<T, TAttr>(string[] folders = null)
            where T : ScriptableObject
            where TAttr : Attribute
        {
            var result = new List<AttributeItemData<T, TAttr>>();

            var assets = GetAssets<T>(folders);

            foreach (var asset in assets)
            {
                var type      = asset.GetType();
                var attribute = type.GetCustomAttribute<TAttr>();
                if (attribute != null)
                {
                    result.Add(new AttributeItemData<T, TAttr>
                    {
                        Attribute = attribute,
                        Value     = asset
                    });
                }
            }

            return result;
        }


        public static void FindItems<T>(Action<Object, T> action)
        {
            var assets = GetAllAssets();
            ApplyTypeItemAction(assets, action);
        }

        public static void ApplyTypeItemAction<TData>(List<Object> resources, Action<Object, TData> action, HashSet<object> excludedItems = null)
        {
            if (resources == null || resources.Count == 0) return;

            var items = new List<Object>();
            excludedItems = excludedItems != null ? excludedItems : new HashSet<object>();
            for (int i = 0; i < resources.Count; i++)
            {
                var asset = resources[i];
                if (asset == null) continue;

                GetAssets(asset, items);
                ProceedTypeAssets(items, action, excludedItems);

                var progress = i / ((float) resources.Count);
                var canceled = EditorUtility.DisplayCancelableProgressBar(string.Format("Prepare Assets [{0} of {1}] :", i, resources.Count), asset.name, progress);
                if (canceled)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        public static void ApplyTypeItemAction<TData>(List<string> resources, Action<Object, TData> action) where TData : class
        {
            if (resources == null || resources.Count == 0) return;

            var items         = new List<Object>();
            var excludedItems = new HashSet<object>();
            for (int i = 0; i < resources.Count; i++)
            {
                var assetPath = resources[i];
                var asset     = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset == null)
                {
                    Debug.LogErrorFormat("ApplyTypeItemAction load NULL asset at path {0}", assetPath);
                    continue;
                }

                GetAssets(asset, items);
                var progress = i / ((float) resources.Count);
                var canceled = EditorUtility.DisplayCancelableProgressBar(string.Format("Prepare Assets [{0} of {1}] : ", i, resources.Count), assetPath, progress);

                ProceedTypeAssets(items, action, excludedItems);
                if (canceled)
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        public static void ProceedTypeAssets<TData>(List<Object> assets, Action<Object, TData> action, HashSet<object> excludedItems = null)
        {
            GUI.changed = true;
            for (int j = 0; j < assets.Count; j++)
            {
                var asset = assets[j];
                if (!asset) continue;
                FindTypeItemsInAsset(asset, action, excludedItems);
            }
        }

        public static void FindTypeItemsInAsset<TData>(Object asset, Action<Object, TData> assetAction = null, HashSet<object> excludedItems = null,
            Func<TData, TData> editAction = null)
        {
            try
            {
                ReflectionTools.FindResource(asset, assetAction, excludedItems, editAction);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static void EditTypeItemsInAsset<TData>(Object asset, Func<TData, TData> editAction = null, HashSet<object> excludedItems = null)
        {
            try
            {
                ReflectionTools.FindResource(asset, null, excludedItems, editAction);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static void SearchTypeFromAsset<TData>(Object asset, Action<Object, TData> assetAction, HashSet<object> excludedItems = null)
        {
            try
            {
                var items = new List<Object>();
                GetAssets(asset, items);
                excludedItems = excludedItems == null ? new HashSet<object>() : excludedItems;
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    try
                    {
                        ReflectionTools.FindResource(item, assetAction, excludedItems);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public static Dictionary<Object, List<TData>> SearchTypeFromAsset<TData>(Object asset, HashSet<object> excludedItems = null)
        {
            var result = new Dictionary<Object, List<TData>>();
            SearchTypeFromAsset<TData>(asset, (x, y) =>
            {
                List<TData> items = null;
                if (result.TryGetValue(x, out items))
                {
                    items.Add(y);
                }

                result[x] = new List<TData> {y};
            });
            return result;
        }

        public static List<T> GetRootAssetsFiltered<T>() where T : Object
        {
            var items       = new List<T>();
            var gameObjects = GetAssets<GameObject>();
            for (int i = 0; i < gameObjects.Count; i++)
            {
                var components = gameObjects[i].GetComponents<T>();
                if (components == null || components.Length == 0) continue;
                items.AddRange(components);
            }

            var assets = GetAssets<T>();
            items.AddRange(assets);
            return items;
        }

        public static List<Object> GetAllAssets()
        {
            var items       = new List<Object>();
            var gameObjects = GetAssets<GameObject>();
            items.AddRange(gameObjects.ToArray());

            var scriptableObjects = GetAssets<ScriptableObject>().ToArray();
            items.AddRange(scriptableObjects);
            return items;
        }
        
        public static T GetAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            
            var folder = path.TrimEndPath();
            asset = GetAsset(typeof(T),string.Empty,new[] {folder}) as T;
            return asset;
        }

        public static T GetAsset<T>(string filter,string[] folders) where T : Object
        {
            var asset = GetAsset(typeof(T),filter, folders) as T;
            return asset;
        }
        
        public static T GetAsset<T>(string[] folders = null) where T : Object
        {
            var asset = GetAsset(typeof(T),string.Empty, folders) as T;
            return asset;
        }

        public static List<Object> GetAssets(Object asset, List<Object> output)
        {
            output.Clear();

            if (!asset) return output;

            var gameObjectComponent = asset as GameObject;
            if (gameObjectComponent)
            {
                output.AddRange(gameObjectComponent.GetComponentsInChildren<Component>(true));
            }

            output.Add(asset);

            return output;
        }

        public static List<EditorResource> GetEditorResources<TSource>(string[] folders = null)
            where TSource : Object
        {
            var result = GetAssets<TSource>(folders).
                Select(x =>
            {
                var asset = new EditorResource();
                asset.Update(x);
                return asset;
            }).ToList();
            return result;
        }

        public static List<T> GetAssets<T>(string[] folders = null,int count = 0) where T : Object
        {
            var targetType = typeof(T);
            return GetAssets<T>(targetType, folders,count);
        }


        public static EditorResource ToEditorResource(this Object asset, bool forceUpdate = false)
        {
            var editorResource = _editorResourceFactory(asset);
            if (forceUpdate)
                editorResource.Update();
            return editorResource;
        }

        public static List<T> GetAssets<T>(Type targetType, string folder,int count = 0) where T : Object
        {
            return GetAssets<T>(targetType, string.IsNullOrEmpty(folder) ? null : new[] {folder},count);
        }

        public static List<T> GetAssets<T>(Type targetType, string[] folders = null,int count = 0) where T : Object
        {
            var items = GetAssets(targetType, folders,count);
            return items.OfType<T>().ToList();
        }
        
        public static void GetAssetsPaths<T>(string[] folders ,List<string> result) where T : Object
        {
            var assetsPaths = result;
            var filter      = $"t:{typeof(T).Name}";
            var ids         = AssetDatabase.FindAssets(filter, folders);

            for (var i = 0; i < ids.Length; i++) {
                var id   = ids[i];
                var path = AssetDatabase.GUIDToAssetPath(id);
                assetsPaths.Add(path);
            }
        }
        
        public static List<string> GetAssetsPaths<T>(string[] folders = null) where T : Object
        {
            var assetsPaths = new List<string>();
            var filter      = $"t:{typeof(T).Name}";
            var ids         = AssetDatabase.FindAssets(filter, folders);

            for (var i = 0; i < ids.Length; i++) {
                var id   = ids[i];
                var path = AssetDatabase.GUIDToAssetPath(id);
                assetsPaths.Add(path);
            }

            return assetsPaths;
        }

        public static List<T> GetAssetsWithChilds<T>(string[] folders = null) where T : Object
        {
            var childs = ReflectionTools.FindAllImplementations(typeof(T));
            var items  = GetAssets(typeof(T), folders);
            for (var i = 0; i < childs.Count; i++)
            {
                var child  = childs[i];
                var assets = GetAssets(child, folders);
                items.AddRange(assets);
            }

            return items.OfType<T>().ToList();
        }

        public static string GetFoldersTemplateName(string path, int depth, bool includeFileName = true, int startDepth = 0)
        {
            var result     = includeFileName ? Path.GetFileNameWithoutExtension(path) : string.Empty;
            var tempPath   = path;
            var pathLength = depth + startDepth;
            for (var i = 0; i < pathLength; i++)
            {
                var parent = Directory.GetParent(tempPath);
                if (parent == null)
                    break;
                var parentName = parent.Name;
                if (i >= startDepth)
                {
                    result = string.IsNullOrEmpty(result) ? parentName : string.Format("{0}-{1}", parentName, result);
                }

                tempPath = parent.FullName;
                if (string.Equals(parentName, AssetRoot, StringComparison.OrdinalIgnoreCase))
                    break;
            }

            result = GetValidBundleTag(result);

            return result;
        }
        
        public static bool ShowProgress(ProgressData progress)
        {
            if (progress.IsDone)
            {
                EditorUtility.ClearProgressBar();
                return true;
            }

            var isCanceled = EditorUtility.DisplayCancelableProgressBar(progress.Title, progress.Content, progress.Progress);
            if (isCanceled)
            {
                EditorUtility.ClearProgressBar();
            }

            return isCanceled;
        }

        public static void ClearProgress() => EditorUtility.ClearProgressBar();
        
        public static void ShowProgress(IEnumerable<ProgressData> awaiter)
        {
            try
            {
                foreach (var progress in awaiter)
                {
                    var isCanceled = EditorUtility.DisplayCancelableProgressBar(progress.Title, progress.Content, progress.Progress);
                    if (isCanceled)
                        break;
                }
            }
            finally
            {
                ClearProgress();
            }
        }


        public static void ShowActionProgress(IEnumerator<ProgressData> awaiter)
        {
            var isShown = false;

            try
            {
                while (awaiter.MoveNext())
                {
                    isShown = true;
                    var progress   = awaiter.Current;
                    var isCanceled = EditorUtility.DisplayCancelableProgressBar(progress.Title, progress.Content, progress.Progress);

                    if (isCanceled)
                        break;
                }
            }
            finally
            {
                if (isShown) EditorUtility.ClearProgressBar();
            }
        }

        public static string GetGUID(this Object asset)
        {
            if (!asset) return string.Empty;

            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) 
                ? string.Empty 
                : AssetDatabase.AssetPathToGUID(path);
        }

        public static string GetUniqueAssetName(string path)
        {
            return AssetDatabase.GenerateUniqueAssetPath(path);
        }

        public static bool IsComponent(Type targetType)
        {
            return _componentType.IsAssignableFrom(targetType);
        }


        public static bool IsContainsDuplicatedAssets(List<AssetImporter> assets)
        {
            var assetsNames = assets.Select(x => Path.GetFileNameWithoutExtension(x.assetPath)).ToList();
            var groups      = assetsNames.GroupBy(x => x).ToList();
            return groups.Count != assets.Count;
        }
    }
}