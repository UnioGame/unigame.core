using System;
using System.Threading;
using JetBrains.Annotations;
using UniCore.Runtime.ProfilerTools;
using UniGame.Core.Runtime.Common;
using UniGame.Core.Runtime.DataFlow;
using UniModules.UniCore.Runtime.DataFlow;
using UniGame.Runtime.ObjectPool;
using UniGame.Runtime.ObjectPool.Extensions;
using UniModules.UniGame.Core.Runtime.Common;
using UniModules.UniGame.Core.Runtime.DataFlow;
using UniModules.UniGame.Core.Runtime.DataFlow.Extensions;
using UniGame.Core.Runtime;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class LifetimeExtension
{

    public static ILifeTime LogOnRelease(this ILifeTime lifeTime,string message)
    {
        lifeTime.AddCleanUpAction(() => GameLog.Log(message));
        return lifeTime;
    }
    public static ILifeTime LogOnRelease(this ILifeTime lifeTime,string message,Color color)
    {
        lifeTime.AddCleanUpAction(() => GameLog.Log(message,color));
        return lifeTime;
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
    
    public static ILifeTime BindEvent(this ILifeTimeContext lifeTimeContext,Action action,Action cancellationAction)
    {
        return lifeTimeContext.LifeTime.BindEvent(action,cancellationAction);
    }
    
    public static ILifeTime BindEvent<TArg>(this ILifeTimeContext lifeTimeContext,Action<TArg> action,Action<TArg> cancellationAction)
    {
        return lifeTimeContext.LifeTime.BindEvent(action,cancellationAction);
    }
    
    public static ILifeTime BindEvent<TArg,TArg2>(this ILifeTimeContext lifeTimeContext,Action<TArg,TArg2> action,Action<TArg,TArg2> cancellationAction)
    {
        return lifeTimeContext.LifeTime.BindEvent(action,cancellationAction);
    }
    
    public static ILifeTime BindEvent<TArg,TArg2>(this ILifeTime lifeTime,Action<TArg,TArg2> source,Action<TArg,TArg2> listener)
    {
        if (source == null || listener == null || lifeTime.IsTerminated) return lifeTime;
        
        Observable.FromEvent(x => source+=listener,
                x => source-=listener)
            .Subscribe()
            .AddTo(lifeTime);
        
        return lifeTime;
    }
    
    public static ILifeTime BindEvent<TArg>(this ILifeTime lifeTime,Action<TArg> source,Action<TArg> listener)
    {
        if (source == null || listener == null || lifeTime.IsTerminated) return lifeTime;
        
        Observable.FromEvent(x => source+=listener,
                x => source-=listener)
            .Subscribe()
            .AddTo(lifeTime);
        
        return lifeTime;
    }
    
    public static ILifeTime BindEvent(this ILifeTime lifeTime,Action source,Action listener)
    {
        if (source == null || listener == null || lifeTime.IsTerminated) return lifeTime;
        
        Observable.FromEvent(x => source+=listener,
                x => source-=listener)
            .Subscribe()
            .AddTo(lifeTime);
        
        return lifeTime;
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
        
        return lifeTime;
    }
        
    public static IDisposableLifetime AddTo(this ILifeTime lifeTime, Action cleanupAction)
    {
        var disposableAction = ClassPool.Spawn<DisposableLifetime>();
        disposableAction.AddCleanUpAction(cleanupAction);
        lifeTime.AddDispose(disposableAction);
        return disposableAction;
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
        if (!asset) return lifeTime;
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

    
    public static LifeTimeDefinition AddTo(this LifeTimeDefinition lifeTimeDefinition, ILifeTime lifeTime)
    {
        if (lifeTime == null)
            return lifeTimeDefinition;
        lifeTime.AddCleanUpAction(lifeTimeDefinition.Terminate);
        return lifeTimeDefinition;
    }

    
    public static LifeTimeDefinition ReleaseWith(this LifeTimeDefinition lifeTimeDefinition, ILifeTime lifeTime)
    {
        if (lifeTime == null)
            return lifeTimeDefinition;
        lifeTime.AddCleanUpAction(lifeTimeDefinition.Release);
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
       
    public static IComposedLifeTime Compose(this ILifeTime source, params  ILifeTime[] lifeTimes)
    {
        var composeAction = ClassPool.Spawn<ComposedLifeTime>();
        return composeAction.
            Bind(source).
            Bind(lifeTimes);
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
