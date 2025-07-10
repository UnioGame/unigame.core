namespace UniGame.Core.Runtime.DataFlow
{
    using System;
    using UniGame.Runtime.DataFlow;
    using UniGame.Runtime.ObjectPool.Extensions;
    using ObjectPool;
    using Runtime;

    public class LifeTimeContextCommand : IDisposableCommand,IPoolable
    {
        private LifeTime _lifeTime = new();
        private Action<ILifeTime> _action;

        public void Initialize(Action<ILifeTime> contextAction)
        {
            _lifeTime.Restart();
            _action = contextAction;
        }

        public void Execute()
        {
            _lifeTime.Restart();
            _action?.Invoke(_lifeTime);
        }

        public void Dispose() => this.DespawnWithRelease();

        public void Release()
        {
            _lifeTime.Terminate();
            _action = null;
        }
    }
}
