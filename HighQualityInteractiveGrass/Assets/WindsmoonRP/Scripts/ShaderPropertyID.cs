using UnityEngine;

namespace WindsmoonRP
{
    // todo : all the fields will be load to memory together
    public static class ShaderPropertyID
    {
        #region fields
        public static int DirectionalShadowMap = Shader.PropertyToID("_DirectionalShadowMap");
        public static int DirectionalShadowMatrices = Shader.PropertyToID("_DirectionalShadowMatrices");
        public static int OtherShadowMap = Shader.PropertyToID("_OtherShadowMap");
        public static int OtherShadowMatrices = Shader.PropertyToID("_OtherShadowMatrices");
        public static int ShadowPancaking = Shader.PropertyToID("_ShadowPancaking");
        public static int OtherShadowTiles = Shader.PropertyToID("_OtherShadowTiles");
        
        // begin post processing
        public static int PostProcessingSource = Shader.PropertyToID("_PostProcessingSource");
        public static int PostProcessingSource2 = Shader.PropertyToID("_PostProcessingSource2");
        
        // bloom
        public static int BloomBicubicUpsampling = Shader.PropertyToID("_BloomBicubicUpsampling");
        public static int BloomPreFilter = Shader.PropertyToID("_BloomPreFilter");
        public static int BloomThreshold = Shader.PropertyToID("_BloomThreshold");
        public static int BloomIntensity = Shader.PropertyToID("_BloomIntensity");
        public static int BloomResult = Shader.PropertyToID("_BloomResult");
        
        // begin color grading
        // color adjustments
        public static int ColorAdjustmentsDataPropertyID = Shader.PropertyToID("_ColorAdjustmentData");
        public static int ColorFilterPropertyID = Shader.PropertyToID("_ColorFilter");
        // color filter
        public static int WhiteBalancePropertyID = Shader.PropertyToID("_WhiteBalanceData");
        // split toning
        public static int SplitToningShadowColorPropertyID = Shader.PropertyToID("_SplitToningShadowColor");
        public static int SplitToningHighLightColorPropertyID = Shader.PropertyToID("_SplitToningHighLightColor");
        // end color grading
        // end post processing

        #endregion
    }
}