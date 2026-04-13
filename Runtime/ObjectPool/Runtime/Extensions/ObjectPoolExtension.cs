using UniGame.Core.Runtime;

namespace UniGame.Runtime.ObjectPool.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class ObjectPoolExtension
    {
        public static bool CreatePool(this GameObject asset, int preloadCount = 0)
        {
            return ObjectPool.CreatePool(asset,preloadCount);
        }
        
        public static TComponent Spawn<TComponent>(this GameObject prototype)
        {
            if (!prototype) return default(TComponent);
            
            var pawn = ObjectPool.Spawn<TComponent>(prototype);
            return pawn;
        }

        public static T Spawn<T>(this T prototype)
            where T : Object
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn<T>(prototype, Vector3.zero, Quaternion.identity, null, false);
            return pawn;
        }
        
        public static T Spawn<T>(this T prototype, 
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null,
            bool stayWorldPosition = false,
            bool setActive = false)
            where T : Object
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn<T>(prototype, position, rotation, parent, stayWorldPosition,setActive);
            return pawn;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<ObjectsItemResult> SpawnAsync(
            this GameObject prototype, 
            int count,
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null,
            CancellationToken token = default)
        {
            if (prototype == null) return ObjectsItemResult.Empty;
            
            var pawn = await ObjectPool
                .SpawnAsync(prototype,count, position, rotation, parent, token);
            
            return pawn;
        }

        public static GameObject SpawnActive(this GameObject prototype,
            Transform parent = null,
            bool stayWorldPosition = false)
        {
            if (!prototype) return null;
            var transform = prototype.transform;
            var pawn = ObjectPool.Spawn(prototype,true, transform.position, transform.rotation, parent, stayWorldPosition);
            return pawn;
        }
        
        public static GameObject SpawnActive(this GameObject prototype, 
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null,
            bool stayWorldPosition = false)
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn(prototype,true, position, rotation, parent, stayWorldPosition);
            return pawn;
        }
        
        public static T SpawnActive<T>(this T prototype, 
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null,
            bool stayWorldPosition = false)
            where T : Component
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn<T>(prototype,true, position, rotation, parent, stayWorldPosition);
            return pawn;
        }
        
        public static ILifeTime AttachPoolToLifeTime(this GameObject prototype, ILifeTime lifeTime, bool createPoolIfNone = false, int preload = 0)
        {
            return ObjectPool.AttachToLifeTime(prototype, lifeTime,createPoolIfNone,preload);
        }

                
        public static GameObject Spawn(this GameObject source, ILifeTime lifeTime, int preload = 0)
        {
            source.AttachPoolToLifeTime(lifeTime, true, preload);
            return source;
        }
        
        public static GameObject Spawn(this GameObject prototype, Transform parent = null, bool stayWorldPosition = false)
        {
            if (prototype == null) return null;
            var pawn = ObjectPool.Spawn(prototype, Vector3.zero, Quaternion.identity,
                                                    parent, stayWorldPosition);
            return pawn;
        }
        
        public static GameObject Spawn(this GameObject prototype,Vector3 position, 
            Transform parent = null, bool stayWorldPosition = false)
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn(prototype, position, Quaternion.identity,
                                        parent, stayWorldPosition);
            return pawn;
        }
        
        public static GameObject Spawn(this GameObject prototype, 
            Vector3 position, 
            Quaternion rotation, 
            Transform parent = null,
            bool stayWorldPosition = false, 
            bool setActive = true)
        {
            if (!prototype) return null;
            var pawn = Spawn(prototype,setActive, position, rotation, parent, stayWorldPosition);
            return pawn;
        }

        public static GameObject Spawn(this GameObject prototype,bool activateOnSpawn,
            Vector3 position, Quaternion rotation,
            Transform parent = null, 
            bool stayWorldPosition = false, 
            bool setActive = false)
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn(prototype,activateOnSpawn, position, rotation, parent, stayWorldPosition,0,setActive);
            return pawn;
        }
        
        public static T Spawn<T>(this T prototype, Transform parent = null, bool stayWorldPosition = false)
            where T : Object
        {
            if (!prototype) return null;
            var pawn = ObjectPool.Spawn<T>(prototype, Vector3.zero, Quaternion.identity,
                parent, stayWorldPosition);
            return pawn;
        }

        public static T Spawn<T>(Action<T> action = null)
            where T : class, new()
        {
            var item = ClassPool.Spawn( action);
            return item ?? new T();
        }

        public static void DestroyPool(this Object data)
        {
            switch (data) {
                case Component target :
                    ObjectPool.DestroyPool(target.gameObject);
                    break;
                case { } target:
                    ObjectPool.DestroyPool(target);
                    break;
            }
        }

        public static void DespawnAsset(this Object data, bool destroy = false)
        {
            ObjectPool.Despawn(data,destroy);
        }

    }
}
