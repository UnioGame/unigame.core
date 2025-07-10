namespace UniGame.GameFlow.Runtime
{
    using System;
    using UniGame.Runtime.DataFlow;
    using Core.Runtime;

    /// <summary>
    /// base game service class for binding Context source data to service logic
    /// </summary>
    [Serializable]
    public abstract class GameService : IGameService
    {
        private readonly LifeTime _lifeTime = new();

        public void Dispose() => _lifeTime.Terminate();

        public ILifeTime LifeTime => _lifeTime;
    }
}