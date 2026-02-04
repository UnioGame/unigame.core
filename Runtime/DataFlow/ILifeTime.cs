using System.Threading;

namespace UniGame.Core.Runtime
{
    using System;

    public interface ILifeTime
    {
        /// <summary>
        /// cleanup with source object, call when life time terminated
        /// </summary>
        ILifeTime AddCleanUpAction<T>(T source, Action<T> cleanAction);
        
        /// <summary>
        /// cleanup action, call when life time terminated
        /// </summary>
        ILifeTime AddCleanUpAction(Action cleanAction);

        /// <summary>
        /// add child disposable object
        /// </summary>
        ILifeTime AddDispose(IDisposable item);

        /// <summary>
        /// save object from GC
        /// </summary>
        ILifeTime AddRef(object o);

        /// <summary>
        /// is lifetime terminated
        /// </summary>
        bool IsTerminated { get; }
        
        CancellationToken Token { get; }
    }
}