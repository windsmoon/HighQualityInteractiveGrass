using UnityEngine;

namespace WindsmoonRP.PostProcessing
{
    [CreateAssetMenu(menuName = "WindsmoonRP/Create Post Processing Asset")]
    public class PostProcessingAsset : ScriptableObject
    {
        #region fields
        [SerializeField]
        private Shader shader;
        private Material material;
        
        // bloom
        [SerializeField] 
        private BloomSettings bloomSettings = new BloomSettings {Scatter = 0.7f};
        [SerializeField]
        private ColorGradingSettings colorGradingSettings = new ColorGradingSettings() {ColorFilter = Color.white};
        [SerializeField]
        private WhiteBalanceSettings whiteBalanceSettings = default;
        [SerializeField]
        private SplitToningSettings splitToningSettings = new SplitToningSettings() {ShadowColor = Color.gray, HighLightColor = Color.gray};
        [SerializeField]
        private ToneMappingSettings toneMappingSettings = new ToneMappingSettings();
        #endregion

        #region properties
        public Material Material
        {
            get
            {
                if (material == null && shader != null)
                {
                    material = new Material(shader);
                    material.hideFlags = HideFlags.HideAndDontSave;
                }

                return material;
            }
        }

        public BloomSettings BloomSettings => bloomSettings;
        public ColorGradingSettings ColorGradingSettings => colorGradingSettings;
        public WhiteBalanceSettings WhiteBalanceSettings => whiteBalanceSettings;
        public SplitToningSettings SplitToningSettings => splitToningSettings;
        public ToneMappingSettings ToneMappingSettings => toneMappingSettings;
        #endregion
    }
}