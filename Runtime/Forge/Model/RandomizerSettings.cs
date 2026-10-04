using System;

namespace DataKeeper.Forge
{
    [Serializable]
    public class RandomizerSettings
    {
        public HarmonyMode Harmony = HarmonyMode.Unison;
        public float VariationAmount = 0.3f;
        public float PhysicsCoupling = 0.7f;
        public bool LockLength;
        public bool LockFx;
        public int CandidateCount = Analysis.CandidateFilter.DefaultCandidates;
    }
}
