namespace UniGame.Core.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UniGame.Runtime.DataFlow;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class AssetLifeTime 
    {
        public struct AssetLifeTimeHandle
        {
            public int id;
            public LifeTime lifeTime;
            public bool terminateOnDisable;
            public Object asset;
        }
        
        public const int DefaultCapacity = 64;
        public static AssetLifeTimeHandle[] assetLifeTimeHandles;
        public static Dictionary<int, int> lifeTimeMap;
        public static int[] emptySlots;
        public static int[] lockedSlots;
        public static int assetLifeTimeCount = 0;
        public static CancellationTokenSource cancellationSource;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset()
        {
            if (assetLifeTimeHandles != null)
            {
                foreach (var variableHandle in assetLifeTimeHandles)
                    variableHandle.lifeTime?.Restart();
            }
            
            lifeTimeMap?.Clear();
            cancellationSource?.Cancel();
            cancellationSource?.Dispose();

            lifeTimeMap = new Dictionary<int, int>(DefaultCapacity);
            cancellationSource = new CancellationTokenSource();
            assetLifeTimeHandles = Array.Empty<AssetLifeTimeHandle>();
            emptySlots = Array.Empty<int>();
            lockedSlots = Array.Empty<int>();
            assetLifeTimeCount = 0;

            ResizeLifeTimes(DefaultCapacity);
            
            if(Application.isPlaying)
                UpdateLifeTimesAsync().Forget();

            Application.quitting -= Reset;
            Application.quitting += Reset;
        }

        private static async UniTask UpdateLifeTimesAsync()
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                UpdateLifeTimes();

                await UniTask.WaitForEndOfFrame();
            }
        }

        private static void UpdateLifeTimes()
        {
            var space = 0;
            var size = assetLifeTimeCount;
            
            for (var i = 0; i < size; i++)
            {
                var index = lockedSlots[i];
                if(index < 0) break;

                var targetIndex = i - space;
                
                if (space > 0)
                {
                    lockedSlots[i] = lockedSlots[targetIndex];
                    lockedSlots[targetIndex] = index;
                }
                
                ref var handle = ref assetLifeTimeHandles[index];
                if(handle.id == 0) continue;
                    
                var asset = handle.asset;

                if (asset != null &&
                    (!handle.terminateOnDisable ||
                     asset is not GameObject { activeInHierarchy: false })) continue;
                
                space++;
                
                lifeTimeMap.Remove(handle.id);
                handle.lifeTime.Restart();
                handle.asset = null;
                handle.id = 0;
                
                assetLifeTimeCount--;
                lockedSlots[targetIndex] = -1;
                emptySlots[assetLifeTimeCount] = index;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TryResizeLifeTimes()
        {
            if (assetLifeTimeCount < assetLifeTimeHandles.Length) return;

            var size = assetLifeTimeHandles.Length;
            size *= 2;
            
            ResizeLifeTimes(size);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ResizeLifeTimes(int newSize)
        {
            var size = newSize;
            var oldSize = assetLifeTimeHandles.Length;
            
            Array.Resize(ref assetLifeTimeHandles,size);
            Array.Resize(ref emptySlots,size);
            Array.Resize(ref lockedSlots,size);
                
            for (var i = oldSize; i < size; i++)
            {
                lockedSlots[i] = -1;
                emptySlots[i] = i;
                assetLifeTimeHandles[i].id = 0;
            }
        }
        
        public static ILifeTime GetAssetLifeTime(this Object source,
            bool terminateOnDisable = false)
        {
            var id = source.GetInstanceID();
            if(lifeTimeMap.TryGetValue(id,out var index))
                return assetLifeTimeHandles[index].lifeTime;

            TryResizeLifeTimes();
            
            terminateOnDisable = terminateOnDisable && source is GameObject;
            
            var emptyIndex = emptySlots[assetLifeTimeCount];
            var handle = new AssetLifeTimeHandle()
            {
                id = id,
                lifeTime = new LifeTime(),
                terminateOnDisable = terminateOnDisable,
                asset = source
            };
            
            emptySlots[assetLifeTimeCount] = -1;
            assetLifeTimeHandles[emptyIndex] = handle;
            lifeTimeMap[id] = emptyIndex;
            lockedSlots[assetLifeTimeCount] = emptyIndex;
            
            assetLifeTimeCount++;
            
            return handle.lifeTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifeTime DestroyOnCleanup(this LifeTime lifeTime, GameObject gameObject)
        {
            lifeTime.AddCleanUpAction(() =>
            {
                if (gameObject == null) return;
                Object.Destroy(gameObject);
            });
            return lifeTime;
        }
        
        public static ILifeTime DestroyOnCleanup(this LifeTime lifeTime, Component component, bool onlyComponent = false)
        {
            if (!onlyComponent)
            {
                return lifeTime.DestroyOnCleanup(component.gameObject);
            }
            
            lifeTime.AddCleanUpAction(() =>
            {
                if (component)
                    Object.Destroy(component);
            });
            
            return lifeTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifeTime GetAssetLifeTime(this Component component, bool terminateOnDisable = false)
        {
            return component.gameObject.GetAssetLifeTime(terminateOnDisable);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifeTime AddDisposable(this Object gameObject, IDisposable disposable)
        {
            return gameObject.GetAssetLifeTime().AddDispose(disposable);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifeTime AddCleanUp(this Object gameObject, Action cleanupAction)
        {
            return gameObject.GetAssetLifeTime().AddCleanUpAction(cleanupAction);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifeTime AddDisposable(this Component component, IDisposable disposable) => AddDisposable(component.gameObject, disposable);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ILifeTime AddCleanUp(this Component component, Action action) =>AddCleanUp(component.gameObject, action);
        
    }
}
