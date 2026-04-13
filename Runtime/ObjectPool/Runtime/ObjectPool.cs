using UniGame.Core.Runtime;
using UniGame.Core.Runtime.Extension;
using UnityEngine;

namespace UniGame.Runtime.ObjectPool
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public static class ObjectPool
    {
        private static List<AssetsPoolObject> _pools = new();
        
        public static ObjectPoolAsset activePool;
        
        public static ObjectPoolAsset PoolAsset => GetPool();

        public static AssetsPoolObject GetPoolByTag(string tagValue)
        {
            return PoolAsset.GetPoolByTag(tagValue);
        }
                
        public static bool LinkPoolTag(this Object asset, string tagValue)
        {
            return PoolAsset.LinkPoolTag(asset, tagValue);
        }
        
        public static bool UnlinkPoolTag(string tagValue)
        {
            return PoolAsset.UnlinkPoolTag(tagValue);
        }
        
        public static AssetsPoolObject GetPoolOrCreate(Object prototype,int preload)
        {
            var component = prototype as Component;
            var protoObject = component != null ? component.gameObject : prototype as  GameObject;
            if(protoObject == null) return null;
            
            var poolObject = PoolAsset;
            if(poolObject.TryGetPool(prototype, out var pool)) return pool;
            
            pool = poolObject.CreatePool(protoObject, preload);
            return pool;
        }
        
        public static ILifeTime AttachToLifeTime(
            Component poolAsset, 
            ILifeTime lifeTime, 
            bool createIfEmpty = false,int preload = 0)
        {
            return PoolAsset.AttachToLifeTime(poolAsset.gameObject, lifeTime, createIfEmpty,preload);
        }
        
        public static ILifeTime AttachToLifeTime(
            GameObject poolAsset, 
            ILifeTime lifeTime, 
            bool createIfEmpty = false,
            int preload = 0)
        {
            return PoolAsset.AttachToLifeTime(poolAsset, lifeTime, createIfEmpty,preload);
        }

        public static bool HasPool(this Object poolAsset)
        {
            var pool = PoolAsset.GetPool(poolAsset);
            return pool != null;
        }
        
        public static bool HasCustomPoolLifeTime(this Object poolAsset)
        {
            var originLifeTime = PoolAsset.LifeTime;
            var pool = PoolAsset.GetPool(poolAsset);
            return pool != null && pool.owner != originLifeTime;
        }
        
        public static ILifeTime ApplyPoolAssetLifeTime(this ILifeTime lifeTime)
        {
            return PoolAsset.AttachToLifeTime(lifeTime);
        }
        
        // These methods allows you to spawn prefabs via Component with varying levels of transform data
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Spawn<T>(Object asset) where T : Object
        {
            return PoolAsset.Spawn<T>(asset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Spawn<T>(GameObject prefab)
        {
            return PoolAsset.Spawn<T>(prefab);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static  UniTask<ObjectsItemResult> SpawnAsync(
            GameObject target,
            int count,
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null,
            CancellationToken token = default)
        {
            return PoolAsset.SpawnAsync(target,count,position,rotation,parent,token);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static  T Spawn<T>(
            Object target,
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null,
            bool stayWorld = false,
            bool setActive = false)
            where T : Object
        {
            return PoolAsset.Spawn<T>(target,position,rotation,parent,stayWorld,setActive);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static  T Spawn<T>(Object target,
            bool activate, Vector3 position, 
            Quaternion rotation, 
            Transform parent = null,
            bool stayWorld = false)
            where T : Object
        {
            var asset = PoolAsset.Spawn<T>(target,position,rotation,parent,stayWorld);
            var gameObjectAsset = asset.GetRootAsset() as GameObject;
            gameObjectAsset?.SetActive(activate);
            return asset;
        }
        
        // These methods allows you to spawn prefabs via GameObject with varying levels of transform data
        public static GameObject Spawn(GameObject prefab)
        {
            return PoolAsset.Spawn(prefab);
        }

        public static  GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation,bool stayWorld = false)
        {
            return PoolAsset.Spawn(prefab,position,rotation,stayWorld);
        }

        public static  GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent,bool stayWorld)
        {
            return PoolAsset.Spawn(prefab,position,rotation,parent,stayWorld);
        }

        public static  Object Spawn(Object prefab, Vector3 position, Quaternion rotation, Transform parent,bool stayWorld, int preload)
        {
            return PoolAsset.Spawn(prefab,position,rotation,parent,stayWorld,preload);
        }

        public static  GameObject Spawn(GameObject prefab,bool activate, Vector3 position, Quaternion rotation, 
            Transform parent = null, bool stayWorld = false, int preload = 0, bool setActive = false)
        {
            return PoolAsset.Spawn(prefab,activate,position,rotation,parent,stayWorld,preload,setActive);
        }
        
        public static void CreatePool(Component targetPrefab, int preloads = 0)
        {
            PoolAsset.CreatePool(targetPrefab.gameObject,preloads);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CreatePool(GameObject targetPrefab, int preloads = 0)
        {
            var pool = PoolAsset.CreatePool(targetPrefab,preloads);
            return pool != null;
        }

        public static  void DestroyPool(Object poolAsset)
        {
            PoolAsset.DestroyPool(poolAsset);
        }
        
        public static void DestroyPoolByTag(string tagValue)
        {
            var pool = PoolAsset.GetPoolByTag(tagValue);
            if (pool == null) return;
            DestroyPool(pool.asset);
        }
        
        public static  void DestroyAllPools()
        {
            _pools.Clear();
            _pools.AddRange(ObjectPoolAsset.pools.Values);
            foreach (var poolObject in _pools)
                poolObject.Dispose();
            _pools.Clear();
        }
        
        // This allows you to despawn a clone via GameObject, with optional delay
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static  void Despawn(Object clone,bool destroy = false)
        {
            PoolAsset.Despawn(clone,destroy);
        }

        public static  void RemoveFromPool(GameObject target)
        {
            PoolAsset.RemoveFromPool(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ObjectPoolAsset GetPool()
        {
            if (activePool) return activePool;
            activePool = new GameObject(nameof(ObjectPoolAsset)).AddComponent<ObjectPoolAsset>();
            return activePool;
        }
        
    }
}