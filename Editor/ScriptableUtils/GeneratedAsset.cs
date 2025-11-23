namespace UniGame.Core.Editor
{
    using System;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using UniModules.Editor;
#endif
    
    public abstract class GeneratedAsset<TAsset> : 
        ScriptableObject
        where TAsset : ScriptableObject
    {
#if UNITY_EDITOR
        
        #region static data

        private static TAsset _selector;

        public static readonly Type AssetType = typeof(TAsset);

        public static string AssetPath =>  ValueTypeCache.GetAssetPath<TAsset>();

        public static TAsset Asset => _selector = _selector != null 
            ? _selector 
            : ValueTypeCache.LoadAsset<TAsset>();
        
        #endregion
        
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button]
#endif
        public void DestroyAsset()
        {
            if (!_selector) return;
            var path = AssetDatabase.GetAssetPath(_selector);
            AssetDatabase.DeleteAsset(path);
            _selector = null;
            this.MarkDirty();
        }

#endif
        
    }
}