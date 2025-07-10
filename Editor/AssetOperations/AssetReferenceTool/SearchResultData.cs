using UniGame.Core.Runtime;

namespace UniModules.Editor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using UniModules.UniCore.EditorTools.Editor;
    using global::UniGame.Runtime.DataFlow;
    using Object = UnityEngine.Object;

    public class SearchResultData : IDisposable, ILifeTimeContext
    {
        private LifeTime _lifeTime = new LifeTime();
        private Action<ProgressData> _progressAction;

        public Action<ProgressData> ProgressAction => _progressAction;

        public ConcurrentDictionary<Object, List<ResourceHandle>> referenceMap = new ConcurrentDictionary<Object, List<ResourceHandle>>();
        
        public AssetResourcesMap assets  = new AssetResourcesMap(2);

        public ILifeTime LifeTime => _lifeTime;
        
        public void Dispose() => _lifeTime.Terminate();

        public SearchResultData AddKey(Object asset)
        {
            if (referenceMap.ContainsKey(asset)) {
                return this;
            }
            referenceMap[asset] = new List<ResourceHandle>();
            var resource = new EditorResource();
            resource.Update(asset);
            assets[asset] = resource;
            return this;
        }

        public SearchResultData ReportProgress(ProgressData progress)
        {
            _progressAction?.Invoke(progress);
            return this;
        }

        public void Complete() => _lifeTime.Restart();
    }
}