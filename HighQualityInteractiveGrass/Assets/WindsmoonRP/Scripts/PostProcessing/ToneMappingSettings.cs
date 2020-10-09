using System;
using UnityEngine;

namespace WindsmoonRP.PostProcessing
{
    [Serializable]
    public struct ToneMappingSettings
    {
        #region fields
        public ToneMappingMode Mode;
        #endregion
        
        #region enums
        public enum ToneMappingMode
        {
            None = -1,
            ACES,
            Neutral,
            Reinhard
        }
        #endregion
    }
}