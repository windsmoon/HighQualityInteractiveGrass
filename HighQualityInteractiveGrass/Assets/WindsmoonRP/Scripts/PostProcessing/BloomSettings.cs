using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace WindsmoonRP.PostProcessing
{
    [Serializable]
    public struct BloomSettings
    {
        #region fields
        [Range(0, 16)]
        public int MaxBloomIterationCount;
        [Min(1)]
        public int MinResolution;
        public bool UseBicubicUpsampling;
        [Min(0)]
        public float Threshold;
        [Range(0, 1)]
        public float ThresholdKnee;
        [Min(0f)]
        public float Intensity;
        public bool FadeFireflies;
        public BloomMode mode;
        [Range(0.05f, 0.95f)]
        public float Scatter;
        #endregion

        #region methods
        // public void Init()
        // {
        //     MaxBloomIterationCount = 16;
        //     MinResolution = 1;
        //     UseBicubicUpsampling = true;
        //     Threshold = 0.5f;
        //     ThresholdKnee = 0.5f;
        //     Intensity = 1;
        // }
        #endregion

        #region enums
        public enum BloomMode
        {
            Additive,
            Scattering
        }
        #endregion
    }
}