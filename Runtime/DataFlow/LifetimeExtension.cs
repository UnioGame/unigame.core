using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniCore.Runtime.ProfilerTools;
using UniGame.Core.Runtime.Common;
using UniGame.Core.Runtime.DataFlow;
using UniGame.Runtime.DataFlow;
using UniGame.Runtime.ObjectPool;
using UniGame.Runtime.ObjectPool.Extensions;
using UniGame.Common;
using UniGame.DataFlow;
using UniGame.Core.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LifetimeExtension
{

    public static async UniTask AwaitTimeoutLog(this ILifeTime lifeTime,TimeSpan timeOut,
        Func<string> message,LogType logType = LogType.Error)
    {
        var delay = timeOut.TotalMilliseconds;
        if (delay > 0) return;

        var token = lifeTime.Token;
        await UniTask.Delay(timeOut,cancellationToken:token)
            .AttachExternalCancellation(token);

        var logMessage = message();
        
        switch (logType)
        {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                GameLog.LogError(logMessage);
                break;
            case LogType.Warning:
                GameLog.LogWarning(logMessage);
                break;
            case LogType.Log:
                GameLog.Log(logMessage);
                break;
        }
        
        GameLog.Log(message?.Invoke());
    }
    
    public static ILifeTime DestroyWith(this ILifeTime lifeTime, GameObject gameObject)
    {
        if (!gameObject) return lifeTime;
        DestroyWith(gameObject,lifeTime);
        return lifeTime;
    }
    
    public static ILifeTime AddTo(this ILifeTimeContext lifeTimeContext,Action action,Action cancellationAction)
    {
        return lifeTimeContext.LifeTime.AddTo(action,cancellationAction);
    }
    
    public static ILifeTime AddTo(this ILifeTime lifeTime,Action action,Action cancellationAction)
    {
        if (lifeTime.IsTerminated) return lifeTime;
        
        action?.Invoke();
        lifeTime.AddCleanUpAction(cancellationAction);
        return lifeTime;
    }
    
    public static T AddTo<T>(this T disposable, ILifeTime lifeTime)
        where T : IDisposable
    {
        if (disposable != null)
            lifeTime.AddDispose(disposable);
        return disposable;
    }
    
    public static LifeTime RestartWith(this LifeTime lifeTime, ILifeTime owner)
    {
        owner.AddCleanUpAction(lifeTime.Restart);
        return lifeTime;
    }

    public static T DestroyWith<T>(this T asset, ILifeTime lifeTime)
        where T : Object
    {
        if (asset == null) return asset;
        
        switch (asset)
        {
            case Component component:
            {
                DestroyComponentWith(component, lifeTime);
                break;
            }
            case GameObject gameObject:
            {
                DestroyObjectWith(gameObject, lifeTime);
                break;
            }
            default:
            {
                DestroyAssetWith(asset, lifeTime);
                break;
            }
        }

        return asset;
    }

    
    public static ILifeTime DespawnWith(this Object asset, ILifeTime lifeTime)
    {
        if (asset == null) return lifeTime;
        if (lifeTime.IsTerminated)
        {
            asset.Despawn();
            return lifeTime;
        }

        lifeTime.AddCleanUpAction(asset.Despawn);
        return lifeTime;
    }
    
    public static Component DestroyComponentWith(Component asset, ILifeTime lifeTime)
    {
        if (!asset) return asset;
        DestroyObjectWith(asset.gameObject,lifeTime);
        return asset;
    }
    
    public static Object DestroyAssetWith(Object asset, ILifeTime lifeTime)
    {
        if (!asset) return asset;
        lifeTime.AddCleanUpAction(() => CheckDestroy(asset));
        return asset;
    }

    public static GameObject DestroyObjectWith(GameObject gameObject, ILifeTime lifeTime)
    {
        if (!gameObject) return gameObject;
        lifeTime.AddCleanUpAction(() => CheckDestroy(gameObject));
        return gameObject;
    }

    public static void CheckDestroy(Object asset)
    {
        if (asset == null) return;
        Object.Destroy(asset);
    }
    
    public static ILifeTime DestroyWith(this ILifeTime lifeTime, Component component)
    {
        if (!component) return lifeTime;
        lifeTime.AddCleanUpAction(() =>  Despawn(component.gameObject,true));
        return lifeTime;
    }
    
    public static ILifeTime DespawnWith(this ILifeTime lifeTime, GameObject gameObject)
    {
        if (!gameObject) return lifeTime;
        lifeTime.AddCleanUpAction(() => Despawn(gameObject,false));
        return lifeTime;
    }

    
    public static LifeTime AddTo(this LifeTime lifeTimeDefinition, ILifeTime lifeTime)
    {
        if (lifeTime == null)
            return lifeTimeDefinition;
        lifeTime.AddCleanUpAction(lifeTimeDefinition.Terminate);
        return lifeTimeDefinition;
    }

    
    public static LifeTime ReleaseWith(this LifeTime lifeTimeDefinition, ILifeTime lifeTime)
    {
        if (lifeTime == null)
            return lifeTimeDefinition;
        lifeTime.AddCleanUpAction(lifeTimeDefinition.Terminate);
        return lifeTimeDefinition;
    }
    
    public static TLifeTime AddCleanUpAction<TLifeTime>(this TLifeTime context,Action action)
        where TLifeTime : ILifeTimeContext
    {
        context.LifeTime.AddCleanUpAction(action);
        return context;
    }
    
    public static ILifeTimedAction CreateLifeTimedAction<TLifeTime>(
        this ILifeTime lifeTime,
        Action action,
        Action onLifetimeFinished = null)
    {
        var lifetimeAction = ClassPool.Spawn<LifeTimedAction>();
        lifetimeAction.Initialize(lifeTime,action,onLifetimeFinished);
        return lifetimeAction;
    }


    public static IDisposableCommand CreateLifeTimeCommand<TLifeTime>(this Action<ILifeTime> action)
    {
        var lifetimeAction = ClassPool.Spawn<LifeTimeContextCommand>();
        lifetimeAction.Initialize(action);
        return lifetimeAction;
    }
    
    public static TLifeTime AddDisposable<TLifeTime>(this TLifeTime context,IDisposable action)
        where TLifeTime : ILifeTimeContext
    {
        context.LifeTime.AddDispose(action);
        return context;
    }

    public static bool IsTerminatedLifeTime(this ILifeTime lifeTime)
    {
        return lifeTime == null || lifeTime.IsTerminated || lifeTime == LifeTime.TerminatedLifetime;
    }

    public static ILifeTime GetLifeTime(this object source)
    {
        if (source == null)
            return LifeTime.TerminatedLifetime;
        
        switch (source)
        {
            case Scene scene:
                return scene.GetSceneLifeTime();
            case ILifeTime lifeTime:
                return lifeTime;
            case ILifeTimeContext lifeTimeContext:
                return lifeTimeContext.LifeTime;
            case Component component:
                return component.GetAssetLifeTime();
            case GameObject gameObject:
                return gameObject.GetAssetLifeTime();
        }
        
        return LifeTime.TerminatedLifetime;
    }
    
    /// <summary>
    /// release lifetime when all dependencies released
    /// </summary>
    public static ILifeTime MergeLifeTime(
        this ILifeTime source, 
        Action cleanup,
        params ILifeTime[] additional)
    {
        var mergedLifeTime = source.MergeLifeTime(additional);
        mergedLifeTime.AddCleanUpAction(cleanup);
        
        return mergedLifeTime;
    }

    /// <summary>
    /// release lifetime when all dependencies released
    /// </summary>
    public static UnionLifeTime ToUnionLifeTime(this ILifeTime source)
    {
        var mergedLifeTime = new UnionLifeTime();
        mergedLifeTime.Add(source);
        
        return mergedLifeTime;
    }
    
    /// <summary>
    /// release lifetime when all dependencies released
    /// </summary>
    public static ILifeTime MergeLifeTime(
        this ILifeTime source, 
        params ILifeTime[] additional)
    {
        var mergedLifeTime = source.ToUnionLifeTime();
        mergedLifeTime.Add(additional);
        
        return mergedLifeTime;
    }
    
    #region type convertion
    
    public static CancellationToken AsCancellationToken(this ILifeTime lifeTime)
    {
        return lifeTime.Token;
    } 
    
    #endregion
    
    
    private static void Despawn(GameObject gameObject,bool destroy)
    {
        if (!gameObject) return;
        
        if (destroy)
        {
            Object.Destroy(gameObject);
            return;
        }
        
        gameObject.DespawnAsset();
    }
}
