using System.Threading;
using UnityEngine;

namespace UniGame.DataFlow
{
    using System;
    using global::UniGame.Core.Runtime;
    using Runtime.DataFlow;

    public class LifeTimeBehaviour :
        MonoBehaviour, 
        ILifeTime
    {
        private readonly LifeTime _lifeTime = new();
        private readonly LifeTime _disableLifeTime = new();

        public ILifeTime AddCleanUpAction(Action cleanAction) => _lifeTime.AddCleanUpAction(cleanAction);

        public ILifeTime AddDispose(IDisposable item) => _lifeTime.AddDispose(item);

        public ILifeTime AddRef(object o) => _lifeTime.AddRef(o);

        public ILifeTime DisableLifeTime => _disableLifeTime;
        
        public bool IsTerminated => _lifeTime.IsTerminated;
        
        public CancellationToken Token => _lifeTime.Token;

        private void OnEnable()
        {
            _disableLifeTime.Restart();
        }

        private void OnDisable()
        {
            _disableLifeTime.Terminate();
        }

        private void OnDestroy()
        {
            _lifeTime.Terminate();
            _disableLifeTime.Terminate();
        }
    }
}
