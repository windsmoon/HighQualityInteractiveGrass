using System;
using UnityEngine;

namespace WindsmoonRP.PostProcessing
{
    [Serializable]
    public struct SplitToningSettings
    {
        #region fields
        [ColorUsage(false, false)]
        public Color ShadowColor; // in gamma space
        [ColorUsage(false, false)]
        public Color HighLightColor; // in gamma space
        [Range(-100f, 100f)]
        public float Balance;
        #endregion
    }
}