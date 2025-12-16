namespace UniGame.Runtime.ObjectPool
{
    using UniCore.Runtime.ProfilerTools;
    using UniGame.Core.Runtime.Extension;
    using Common;
    using DataFlow;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using UniGame.Core.Runtime.ObjectPool;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using Object = UnityEngine.Object;
    
    [Serializable]
    public class AssetsPoolObject
    {
        public LifeTime lifeTime = new();
        public GameObject gameObjectAsset;
        public ILifeTime owner;
        
        #region private properties
        
        private DisposableAction _disposableAction;
        
        #endregion

        public string sourceName;
        
        [Tooltip("The prefab the clones will be based on")]
        public GameObject asset;

        [Tooltip("Should this pool preload some clones?")]
        public int preload;

        public Transform containerObject;
        
        // The total amount of created prefabs
        public int total;

        // All the currently cached prefab instances
        public Stack<GameObject> Cache = new();

        

        public AssetsPoolObject AttachLifeTime(ILifeTime newLifeTime)
        {
            //de-attach from current lifetime
            ResetParentLifeTime();

            owner = newLifeTime;
            
            //bind new lifetime
            _disposableAction = new DisposableAction();
            _disposableAction.Initialize(Dispose);
            
            owner.AddDispose(_disposableAction);
            
            return this;
        }

        public AssetsPoolObject ResetParentLifeTime()
        {
            _disposableAction?.Complete();
            return this;
        }

        public void Dispose() =>  lifeTime.Terminate();
        
        
        public AssetsPoolObject Initialize(Component objectAsset,ILifeTime targetLifeTime, int preloadCount = 0,Transform root = null)
        {
            return Initialize(objectAsset.gameObject,targetLifeTime,preloadCount,root);
        }

        public AssetsPoolObject Initialize(GameObject objectAsset,ILifeTime targetLifeTime, int preloadCount = 0,Transform root = null)
        {
            lifeTime ??= new LifeTime();
            lifeTime.Restart();
            lifeTime.AddCleanUpAction(OnDestroy);

            sourceName = objectAsset.name;
            asset = objectAsset;
            preload = preloadCount;
            containerObject = root;
            
            gameObjectAsset = objectAsset;

            UpdatePreload();
            AttachLifeTime(targetLifeTime);
            
            return this;
        }

        // This will return a clone from the cache, or create a new instance
        public Object Spawn(
            Vector3 position, 
            Quaternion rotation,
            Transform parent = null, 
            bool stayWorld = false,
            bool setActive = false)
        {
#if UNITY_EDITOR
            if (!asset) {
                Debug.LogError("Attempting to spawn null");
                return null;
            }
#endif

            // Attempt to spawn from the cache
            while (Cache.Count > 0) {
            
                var clone = TakeFromCache(position, rotation, parent, stayWorld, setActive);
                if(clone == null) continue;
                return clone;
            }

            return CreateGameObject(position, rotation, parent, stayWorld);
        }
        
        // This will return a clone from the cache, or create a new instance
        public async UniTask<ObjectsItemResult> SpawnAsync(
            int count,
            Vector3 position, 
            Quaternion rotation,
            Transform parent = null, 
            CancellationToken token = default)
        {
#if UNITY_EDITOR
            if (!asset) {
                Debug.LogError("Attempting to spawn null");
                return ObjectsItemResult.Empty;
            }
#endif

            var result = TakeFromCache(count, position, rotation, parent);
            if(result.Success && result.Length == count) 
                return result;

            var amount = count - result.Length;
            
            var spawnedItems = await CreateGameObjectAsync(
                amount,
                position,
                rotation,
                parent,
                token);
            
            var spawnCompleted = spawnedItems.Length == count;
            if (spawnCompleted) return spawnedItems;
            
            spawnedItems.Items.CopyTo(result.Items,result.Length);
            result.First = result.Length > 0 ? result.Items[0] : default;
            result.Length += spawnedItems.Length;
            result.Success = true;
            
            return result;
        }

        // This will despawn a clone and add it to the cache
        public void Despawn(Object clone, bool destroy = false)
        {
            if (!clone) return;
            
            if (clone is IPoolable poolable)
                poolable.Release();

            var target = clone.GetRootAsset() as GameObject;
            
            if (destroy) {
                Object.Destroy(target);
                return;
            }

            OnObjectDespawn(clone);
            
            // Add it to the cache
            Cache.Push(target);
        }
        
        public void PreloadAsset()
        {
            if (!gameObjectAsset) return;
            
            // Create clone
            var clone = CreateGameObject(Vector3.zero, Quaternion.identity, null);

            AddIntoCache(clone);
        }

        private void AddIntoCache(GameObject clone)
        {
            var rootAsset = clone.GetRootAsset();
            if (rootAsset is GameObject gameObjectClone)
            {
                gameObjectClone.transform.SetParent(containerObject);
            }
            
            // Add it to the cache
            var pawn = OnObjectDespawn(clone);
            
            Cache.Push(pawn);
        }
        
        // Makes sure the right amount of prefabs have been preloaded
        public void UpdatePreload()
        {
            if (asset == null) return;
            for (var i = total; i < preload; i++) {
                PreloadAsset();
            }
        }

        #region private methods

        private GameObject TakeFromCache(
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool stayWorld = false,
            bool setActive = false)
        {
            // Attempt to spawn from the cache
            while (Cache.Count > 0) {
                
                var clone = Cache.Pop();
                
                if (clone == null) {
                    GameLog.Log($"The {sourceName} pool contained a null cache entry",Color.red);
                    continue;
                }
                
                clone = ApplyGameAssetProperties(clone, position, rotation, parent, stayWorld, setActive);
                return clone;
            }
            
            return default;
        }

        private ObjectsItemResult TakeFromCache(
            int count,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            bool stayWorld = false,
            bool setActive = false)
        {
            if(count <= 0 || count > Cache.Count) return ObjectsItemResult.Empty;
            
            var result = new ObjectsItemResult();
            var spawnedCount = 0;
            
            result.Items = new GameObject[count];
            
            // Attempt to spawn from the cache
            while (Cache.Count > 0 && spawnedCount < count) {
                var clone = TakeFromCache(position, rotation, parent, stayWorld, setActive);
                if(clone == null) continue;
                
                result.Items[spawnedCount] = clone;
                spawnedCount++;
            }
            
            result.First = spawnedCount > 0 ? result.Items[0] : default;
            result.Length = spawnedCount;
            result.Success = spawnedCount == count;
            return result;
        }
        
        private void OnDestroy()
        {
            _disposableAction?.Complete();
            
            owner = null;

            foreach (var item in Cache)
            {
                if (item == null) continue;
#if UNITY_EDITOR
                if (Application.isPlaying == false)
                {
                    Object.DestroyImmediate(item);
                    continue;
                }
#endif
                Object.Destroy(item);
            }
            
            Cache.Clear();

            if (containerObject == null) return;
            
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                Object.DestroyImmediate(containerObject.gameObject);
                return;
            }
#endif
            Object.Destroy(containerObject.gameObject);
        }
        
        private GameObject CreateGameObject(
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null, 
            bool stayWorldPosition = false)
        {
            if (!asset) return default;
            
            var result = Object.Instantiate(gameObjectAsset, position, rotation);
            var resultTransform = result.transform;
            if (resultTransform.parent != parent)
                resultTransform.SetParent(parent, stayWorldPosition);
            
            total += 1;
            
            return result;
        }


        private GameObject ApplyGameAssetProperties(
            Object target, 
            Vector3 position,
            Quaternion rotation, 
            Transform parent, 
            bool stayWorldPosition = false)
        {
            switch (target) {
                case Component componentTarget:
                    return ApplyGameAssetProperties(componentTarget.gameObject, position, rotation, parent, stayWorldPosition);
                case GameObject gameObjectTarget:
                    return ApplyGameAssetProperties(gameObjectTarget, position, rotation, parent, stayWorldPosition);
            }
            return default;
        }
        
        private GameObject ApplyGameAssetProperties(GameObject target, Vector3 position,
            Quaternion rotation, Transform parent, bool stayWorldPosition = false, bool setActive = false)
        {
            var transform = target.transform;
            transform.localPosition = position;
            transform.localRotation = rotation;

            if (transform.parent != parent)
                transform.SetParent(parent, stayWorldPosition);
            
            target.SetActive(setActive);

            return target;
        }

        private GameObject ResetGameObjectState(GameObject targetGameObject)
        {
            
#if UNITY_EDITOR
            if(Application.isPlaying == false) return targetGameObject;
#endif
            if (targetGameObject == null) return targetGameObject;
            
            var parent = containerObject != null ? containerObject : null;
            // Move it under this GO
            if (targetGameObject.transform.parent != null) 
                targetGameObject.transform.SetParent(parent, false);

            targetGameObject.SetActive(false);
            
            return targetGameObject;
        }
        
        private GameObject OnObjectDespawn(Object target)
        {
            switch (target) {
                case Component componentTarget:
                    return ResetGameObjectState(componentTarget.gameObject);
                case GameObject gameObjectTarget:
                    return ResetGameObjectState(gameObjectTarget);
            }
            return null;
        }
                
        private async UniTask<ObjectsItemResult> CreateGameObjectAsync(
            int count,
            Vector3 position,
            Quaternion rotation, 
            Transform parent = null, 
            CancellationToken token = default)
        {
            if (gameObjectAsset == null) 
                return ObjectsItemResult.Empty;
            
            if(count <= 0) 
                return ObjectsItemResult.Empty;

            var operation =  Object.InstantiateAsync(gameObjectAsset,
                count,parent, 
                position, rotation,
                token);

#if UNITY_EDITOR
            //CleanupResourceOnCancellation(operation, token);
#endif

            var assetResult = await operation
                .ToUniTask(cancellationToken: token);

            // if (assetResult.IsCanceled)
            // {
            //     operation.Cancel();
            //     throw new TaskCanceledException();
            // }
            
            var resultItems = assetResult;
            
            if (resultItems == null || resultItems.Length == 0)
                return ObjectsItemResult.Empty;
            
            var spawnedCount = resultItems.Length;
            total += spawnedCount;

            
            return new ObjectsItemResult()
            {
                First = resultItems[0],
                Items = resultItems,
                Success = true,
                Length = spawnedCount,
            };
        }
        
        
        private void CleanupResourceOnCancellation<TAsset>(AsyncInstantiateOperation<TAsset> operation, CancellationToken token)
            where TAsset : Object
        {
#if UNITY_EDITOR
            operation.completed += operationHandle =>
            {
                if(!token.IsCancellationRequested) return;
                if(operation.Result == null) return;
                foreach (var gameObject in operation.Result)
                {
                    if(gameObject == null) continue;
                    Object.DestroyImmediate(gameObject);
                }
            };
#endif
        }
        #endregion
    }
}