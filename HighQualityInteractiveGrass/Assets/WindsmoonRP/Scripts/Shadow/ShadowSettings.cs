using UnityEngine;
using System;
using System.Reflection;

namespace WindsmoonRP.Shadow
{
    [Serializable]
    public class ShadowSettings
    {
        #region fields
        [Min(0.001f), SerializeField]
        private float maxDistance = 100f;
        [Range(0.001f, 1f), SerializeField]
        private float distanceFade = 0.1f;
        [SerializeField]
        private DirectionalShadowSetting directionalShadowSetting = new DirectionalShadowSetting() {ShadowMapSize = TextureSize._2048, CascadeCount = 4,
            CascadeRatio1 = 0.1f, CascadeRatio2 = 0.25f, CascadeRatio3 = 0.5f, CascadeFade = 0.1f, PCFMode = PCFMode.PCF2X2, CascadeBlendMode = CascadeBlendMode.Hard};
        [SerializeField]
        private OtherShadowSettings otherShadowSettings = new OtherShadowSettings() {ShadowMapSize = TextureSize._1024, PCFMode = PCFMode.PCF2X2};
        #endregion

        #region properties
        public float MaxDistance
        {
            get { return maxDistance; }
            set { maxDistance = value; }
        }

        public float DistanceFade
        {
            get { return distanceFade; }
            set { distanceFade = value; }
        }

        public DirectionalShadowSetting DirectionalShadowSetting
        {
            get { return directionalShadowSetting; }
            set { directionalShadowSetting = value; }
        }

        public OtherShadowSettings OtherShadowSettings
        {
            get { return otherShadowSettings; }
            set { otherShadowSettings = value; }
        }
        #endregion
    }
}
