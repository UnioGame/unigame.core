using UnityEngine;

namespace UniGame.Runtime.ObjectPool
{
    using UniGame.Core.Runtime.Extension;
    using UniGame.Core.Runtime.ObjectPool;
    using Object = UnityEngine.Object;

    public static class ObjectPoolData
    {
        public static Transform _root;
        public static Transform RootContainer
        {
            get
            {
                if (_root) return _root;
                var asset = new GameObject(nameof(ObjectPoolData));
                Object.DontDestroyOnLoad(asset);
                _root = asset.transform;
                return _root;
            }
        }

        public static void ReleasePoolable(Object asset)
        {
            if (asset == null) return;

            if (asset is IPoolable poolable)
            {
                poolable.Release();
                return;
            }

            var root = asset.GetRootAsset() as GameObject;
            root?.GetComponent<IPoolable>()?.Release();
        }
    }
}
