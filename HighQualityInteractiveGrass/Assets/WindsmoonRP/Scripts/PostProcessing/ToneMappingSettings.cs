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
            None,
            ACES,
            Neutral,
            Reinhard
        }
        #endregion
    }
}