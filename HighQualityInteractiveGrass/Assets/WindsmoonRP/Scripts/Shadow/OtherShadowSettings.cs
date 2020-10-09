using System;
using UnityEngine;

namespace WindsmoonRP.Shadow
{
    [Serializable]
    public struct OtherShadowSettings
    {
        #region fields
        [SerializeField]
        public TextureSize ShadowMapSize;
        [SerializeField]
        public PCFMode PCFMode;
        #endregion
    }
}