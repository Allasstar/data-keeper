using System;
using UnityEngine.Profiling;

namespace DataKeeper.Extra
{
    public class AllocCounter 
    {
        UnityEngine.Profiling.Recorder rec;

        public AllocCounter() {
            rec = Recorder.Get("GC.Alloc");
            rec.enabled = false;

#if !UNITY_WEBGL
            rec.FilterToCurrentThread();
#endif
        
            rec.enabled = true;
        }

        public int Stop() {
            if (rec == null) throw new InvalidOperationException("AllocCounter was not started.");
        
            rec.enabled = false;
        
#if !UNITY_WEBGL
            rec.CollectFromAllThreads();
#endif
        
            int result = rec.sampleBlockCount;
            rec = null;
            return result;
        }
    }
}