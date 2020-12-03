using UnityEngine;
using UnityEngine.Rendering;
using WindsmoonRP.GrassSea;
using WindsmoonRP.PostProcessing;
using WindsmoonRP.Shadow;

namespace WindsmoonRP
{
    [CreateAssetMenu(menuName = "Windsmoon/Windsmoon Render Pipeline/Create Windsmoon Render Pipeline")]
    public class WindsmoonRenderPipelineAsset : RenderPipelineAsset
    {
        #region fields
        [SerializeField] 
        private bool allowHDR = true;
        [SerializeField]
        private bool useDynamicBatching = true;
        [SerializeField]
        private bool useGPUInstancing = true;
        [SerializeField]
        private bool useSPRBatcher = true;
        [SerializeField]
        private bool useLightsPerObject = true;
        [SerializeField]
        private ShadowSettings shadowSettings;
        [SerializeField]
        private PostProcessingAsset postProcessingAsset = default;
        [SerializeField]
        private GrassSeaConfig grassSeaConfig;
        #endregion

        #region methods
        protected override RenderPipeline CreatePipeline()
        {
            // postProcessingAsset.BloomSettings.Init();
            return new WindsmoonRenderPipeline(allowHDR, useDynamicBatching, useGPUInstancing, useSPRBatcher, useLightsPerObject, shadowSettings, postProcessingAsset, grassSeaConfig);
        }
        #endregion
    }
}
