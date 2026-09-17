#if ODIN_INSPECTOR

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UniGame.Runtime.DataFlow;
using UniGame.Core.Runtime;

namespace UniGame.Context.Editor
{
    [Serializable]
    public class LifeTimeEditorData
    {
        #region indspector

        [Searchable]
        [ListDrawerSettings(ShowFoldout = true)]
        public List<LifeTimeEditorItem> scriptableLifeTimes = new List<LifeTimeEditorItem>();

        #endregion

        private LifeTime _lifeTime = new LifeTime();
        
        public void Initialize(IReadOnlyList<ILifeTime> lifeTimes)
        {
            UpdateList(lifeTimes);
        }

        public void Dispose() => _lifeTime.Terminate();
        
        private void UpdateList(IReadOnlyList<ILifeTime> lifeTimeValue)
        {
            scriptableLifeTimes.Clear();
            foreach (var lifeTime in lifeTimeValue)
            {
                
            }
        }

    }
}

#endif