using System.Threading;

namespace UniGame.DataFlow
{
    using System;
    using System.Collections.Generic;
    using global::UniGame.Core.Runtime;
    using Runtime.Common;
    using Runtime.DataFlow;
    using global::UniGame.Runtime.ObjectPool;
    using global::UniGame.Core.Runtime.ObjectPool;

    public class ComposedLifeTime : IPoolable, IComposedLifeTime
    {
        private LifeTime _lifeTime = new();

        #region lifetime api

        public ILifeTime AddCleanUpAction(Action cleanAction) => _lifeTime.AddCleanUpAction(cleanAction);

        public ILifeTime AddDispose(IDisposable item) =>  _lifeTime.AddDispose(item);

        public ILifeTime AddRef(object o) =>  _lifeTime.AddRef(o);

        public bool IsTerminated => _lifeTime.IsTerminated;
        
        public CancellationToken Token => _lifeTime.Token;

        #endregion

        public IComposedLifeTime Bind(ILifeTime lifeTime)
        {
            if (IsTerminated) {
                return this;
            }
            
            //weak ref to composite lifetime
            var disposableAction = ClassPool.Spawn<DisposableAction>();
            //release refs if lifetime finished
            _lifeTime.AddCleanUpAction(disposableAction.Complete);
            //bind to target lifetime
            disposableAction.Initialize(Dispose);
            lifeTime.AddDispose(disposableAction);
            
            return this;
        }
        
        public IComposedLifeTime Bind(IEnumerable<ILifeTime> lifeTimes)
        {
            if (IsTerminated) {
                return this;
            }

            foreach (var x in lifeTimes) {
                if (!Bind(x).IsTerminated) {
                    continue;
                }

                Dispose();
                return this;
            }

            return this;
        }
        
        public void Dispose() => _lifeTime.Terminate();
        
        public void Release() => _lifeTime.Terminate();
    }
}