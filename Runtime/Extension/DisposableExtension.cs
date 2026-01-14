namespace UniGame.Runtime.Extension
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Common;
    using global::UniGame.Runtime.ObjectPool;

    public static class DisposableExtension
    {
        public static void DisposeItems<TTarget>(this List<TTarget> disposables)
            where  TTarget : IDisposable
        {
            for (int i = 0; i < disposables.Count; i++)
            {
                var item = disposables[i];
                item.Dispose();
            }
            disposables.Clear();
        }

        public static IDisposable AsDisposable<T>(
            this T source, 
            Action<T> cancelationAction)
        {
            var disposable = ClassPool.Spawn<DisposableAction>();
            disposable.Initialize(() => cancelationAction?.Invoke(source));
            return disposable;
        }
        
        public static IDisposable AsDisposable(
            this object source, 
            Action cancelationAction)
        {
            var disposable = ClassPool.Spawn<DisposableAction>();
            disposable.Initialize(() => cancelationAction?.Invoke());
            return disposable;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Cancel(this IDisposable disposable)
        {
            disposable?.Dispose();
        }
                
        public static IDisposable Cancel(this IDisposable disposable, bool clearValue)
        {
            disposable?.Dispose();
            return clearValue ? null : disposable;
        }

        public static void Cancel(this object target, ref IDisposable disposable)
        {
            disposable.Cancel();
            disposable = null;
        }

        public static void Cancel<TItem>(this List<TItem> disposables)
            where TItem : IDisposable
        {
            if (disposables == null)
                return;
            for (var i = 0; i < disposables.Count; i++)
            {
                disposables[i]?.Dispose();
            }
            
            disposables.Clear();
        }

        public static void Cancel(this List<IDisposable> disposables)
        {
            if (disposables == null)
                return;
            for (var i = 0; i < disposables.Count; i++)
            {
                disposables[i]?.Dispose();
            }
            
            disposables.Clear();
        }

    }
}
