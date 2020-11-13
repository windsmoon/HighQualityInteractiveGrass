using System;
using UnityEngine;

namespace WindsmoonRP.PostProcessing
{
    [Serializable]
    public struct ColorGradingSettings
    {
        #region fields
        public float PostExposure;
        [Range(-100f, 100f)]
        public float Contrast;
        [ColorUsage(false, true)]
        public Color ColorFilter;
        [Range(-180f, 180f)]
        public float HueShift;
        [Range(-100f, 100f)]
        public float Saturation;
        #endregion    
    }
}