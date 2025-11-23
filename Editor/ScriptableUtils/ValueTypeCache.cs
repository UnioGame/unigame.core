using UniCore.Runtime.ProfilerTools;

namespace UniGame.Core.Editor
{
    
#if UNITY_EDITOR
    
    using System;
    using System.Collections.Generic;
    using global::UniGame.Runtime.ReflectionUtils;
    using global::UniGame.Runtime.Utils;
    using UniModules;
    using UniModules.Editor;
    using UnityEditor;
    using UnityEngine;

    public static class ValueTypeCache
    {
        private static MemorizeItem<Type, ScriptableObject> _assetCache = 
            MemorizeTool.Memorize<Type, ScriptableObject>(null);
        
        private static MemorizeItem<Type, string> _pathCache = 
            MemorizeTool.Memorize<Type, string>(null);
        
        private static bool _initialized;
        private static string _assetPath;
        private static Dictionary<Type,List<Action<ScriptableObject>>> _callbacks = new();
        
        public static bool IsInitialized => _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Reset()
        {
            _initialized = false;
            _assetPath = string.Empty;
            _callbacks?.Clear();
        }
        
        public static string GetAssetPath<TAsset>(bool includeNamespace = false) where TAsset : ScriptableObject
        {
            var targetType = typeof(TAsset);
            return GetAssetPath(targetType,includeNamespace);
        }
        
        public static string GetAssetPath(Type targetType,bool includeNamespace = false)
        {
            if(_pathCache.ContainsKey(targetType))
                return _pathCache[targetType];
            
            var generatedPath = EditorPathConstants.GeneratedContentDefaultPath;
            var assetPath = includeNamespace 
                ? generatedPath.CombinePath(targetType.Namespace).CombinePath(targetType.Name)
                : generatedPath.CombinePath(targetType.Name);
            
            _pathCache[targetType] = assetPath;
            
            return assetPath;
        }

        public static TAsset LoadAsset<TAsset>() where TAsset : ScriptableObject
        {
            var assetType = typeof(TAsset);
            var asset = LoadAsset(assetType) as TAsset;
            return asset;
        }
        
        public static ScriptableObject LoadAsset(Type assetType)
        {
            if (_assetCache.ContainsKey(assetType))
            {
                return _assetCache[assetType];
            }

            var asset = LoadAssetInternal( assetType);
            _assetCache[assetType] = asset;
            return asset;
        }
        
        private static ScriptableObject LoadAssetInternal(Type targetType)
        {
            if (_assetCache.TryGetValue(targetType, out var asset) && asset != null)
            {
                return asset;
            }
            
            var info = targetType.GetCustomAttribute<GeneratedAssetInfoAttribute>();

            var path = info == null || string.IsNullOrEmpty(info.Location) 
                ? GetAssetPath(targetType)
                : EditorPathConstants.GeneratedContentDefaultPath.CombinePath(info.Location);
                
            GameLog.Log($"GeneratedAsset Create asset of type {targetType.Name} : with path : {path}");

            _pathCache[targetType] = path;
            
            var newAsset  = AssetEditorTools.LoadOrCreate<ScriptableObject>(targetType, path,targetType.Name);
            
            _assetCache[targetType] = newAsset;
            
            if (!_callbacks.TryGetValue(targetType, out var callbacks)) 
                return newAsset;
            
            foreach (var callback in callbacks)
                callback?.Invoke(newAsset);
            
            callbacks.Clear();

            return newAsset;
        }

        [InitializeOnLoadMethod]
        private static void OnInitialize()
        {
            EditorApplication.delayCall -= OnDelayedCall;
            EditorApplication.delayCall += OnDelayedCall;
        }
        
        private static void OnDelayedCall()
        {
            _initialized = true;

            foreach (var callbackValue in _callbacks)
                LoadAssetInternal(callbackValue.Key);
        }
        
    }

#endif
    
}
