using System;
using UnityEngine;

namespace WindsmoonRP.GrassSea
{
    [Serializable]
    public struct WindFieldSettings
    {
        #region fields
        public Texture2D WindNoise;
        public Vector3 UniformWindDirection;
        [Range(0, 1)]
        public float UniformWindForce;
        public Rect WorldRect;
        [Range(0, 1)]
        public float Stablility;
        #endregion
    }
}