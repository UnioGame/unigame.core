using System;
using System.Threading;
using UniGame.Runtime.DataFlow;
using UniGame.Core.Runtime;

namespace UniGame.DataFlow
{
    [Serializable]
    public class UnionLifeTime : IDisposable, ILifeTime
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

        public void Terminate() => _lifeTime.Terminate();
        
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
            if (_counter > 0) return;
            _lifeTime.Terminate();
        }

    }
}