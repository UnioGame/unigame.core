using System;

namespace UniGame.DataFlow
{
    using System.Threading;
    using global::UniGame.Core.Runtime;
    using Runtime.DataFlow;
    using global::UniGame.Runtime.ObjectPool;
    using global::UniGame.Core.Runtime.ObjectPool;

    [Serializable]
    public class LifeTimeCompose : IDisposable, IPoolable,ILifeTime
    {
        private int _counter;
        private LifeTime _lifeTime = new();

        #region lifetime api
        
        public ILifeTime AddCleanUpAction(Action cleanAction) => _lifeTime.AddCleanUpAction(cleanAction);

        public ILifeTime AddDispose(IDisposable item) => _lifeTime.AddDispose(item);

        public ILifeTime AddRef(object o) => _lifeTime.AddRef(o);

        public bool IsTerminated => _lifeTime.IsTerminated;

        public CancellationToken Token => _lifeTime.Token;

        #endregion
        
        public void Release()
        {
            _lifeTime.Restart();
            _counter = 0;
        }
        
        public void Add(params ILifeTime[] lifeTimes)
        {
            foreach (var lifeTime in lifeTimes) {
                Add(lifeTime);
            }
        }
        
        public void Add(ILifeTime lifeTime)
        {
            Interlocked.Increment(ref _counter);
            lifeTime.AddDispose(this);
        }
        
        public void Dispose()
        {
            Interlocked.Decrement(ref _counter);
            _lifeTime.Terminate();
            
            if (_counter > 0) {
                return;
            }
            
            //despawn when counter <= 0
            ClassPool.Despawn(this);
        }

    }
}
