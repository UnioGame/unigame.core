namespace UniGame.Runtime.DataFlow
{
    using System;
    using Core.Runtime;

    public class LifeTimeContext : ILifeTimeContext, IDisposable
    {
        public LifeTime lifeTime = new();

        public ILifeTime LifeTime => lifeTime;
        
        public void Dispose()
        {
            lifeTime.Terminate();
        }
    }
}