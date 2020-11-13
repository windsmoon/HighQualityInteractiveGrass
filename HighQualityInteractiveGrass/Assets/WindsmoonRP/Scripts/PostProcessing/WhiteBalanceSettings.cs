using System;
using UnityEngine;

namespace WindsmoonRP.PostProcessing
{
    [Serializable]
    public struct WhiteBalanceSettings
    {
        #region fields
        [Range(-100f, 100f)] 
        public float Temperature;
        [Range(-100f, 100f)]
        public float Tint;
        #endregion
    }
}