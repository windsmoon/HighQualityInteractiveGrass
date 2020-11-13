using System.Security.Authentication.ExtendedProtection;
using UnityEditor;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;
using UnityEngine.Rendering;
using static WindsmoonRP.PostProcessing.PostProcessingAsset;

namespace WindsmoonRP.PostProcessing
{
    public class PostProcessingStack
    {
        #region constants
        private const string commandBufferName = "Windsmoon Post Processing";
        private const int maxBloomIterationCount = 16;
        #endregion

        #region fields
        private CommandBuffer commandBuffer = new CommandBuffer() {name = commandBufferName};
        private ScriptableRenderContext renderContext;
        private Camera camera;
        private PostProcessingAsset postProcessingAsset;
        private bool useHDR;

        // bloom
        private int bloomIteration1PropertyID;
        #endregion

        #region properties
        public bool IsActive // todo : add cache
        {
            get => postProcessingAsset != null;
        }
        #endregion

        #region constructors
        public PostProcessingStack()
        {
            bloomIteration1PropertyID = Shader.PropertyToID("_BloomIteration1");

            for (int i = 2; i <= maxBloomIterationCount * 2; ++i)
            {
                // todo : do not use the api's side effect
                Shader.PropertyToID("_BloomIteration" + i); // when firstly request a shader property, the id will be simply increase one 
            }
        }
        #endregion
        
        #region methods
        public void Setup(ScriptableRenderContext renderContext, Camera camera, PostProcessingAsset postProcessingAsset, bool useHDR)
        {
            this.renderContext = renderContext;
            this.camera = camera;
            this.useHDR = useHDR;

#if UNITY_EDITOR
            // game and scene (scene has a switch)
            this.postProcessingAsset = camera.cameraType <= CameraType.SceneView ? postProcessingAsset : null;

            if (camera.cameraType == CameraType.SceneView && UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.showImageEffects == false)
            {
                this.postProcessingAsset = null;
            }
#else
            this.postProcessingAsset = postProcessingAsset;
#endif
        }

        public void Render(int sourceID)
        {
            // Draw(sourceID, BuiltinRenderTextureType.CameraTarget, PostProcessingPassEnum.Copy);
            if (DoBloom(sourceID))
            {
                DoColorGradingAndToneMapping(ShaderPropertyID.BloomResult);
                commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.BloomResult);
            }

            else
            {
                DoColorGradingAndToneMapping(sourceID);
            }
            
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void Draw(RenderTargetIdentifier source, RenderTargetIdentifier dest, PostProcessingPass pass)
        {
            commandBuffer.SetGlobalTexture(ShaderPropertyID.PostProcessingSource, source);
            commandBuffer.SetRenderTarget(dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.DrawProcedural(Matrix4x4.identity, postProcessingAsset.Material, (int)pass, MeshTopology.Triangles, 3);
        }

        private bool DoBloom(int sourceID)
        {
            // commandBuffer.BeginSample("Bloom");
            BloomSettings bloomSettings = postProcessingAsset.BloomSettings;
            int width = camera.pixelWidth / 2;
            int height = camera.pixelHeight / 2;

            // * 2 regard that there has at least 2 iteration
            if (bloomSettings.MaxBloomIterationCount == 0 || bloomSettings.Intensity <= 0 || width < bloomSettings.MinResolution * 2 || height < bloomSettings.MinResolution * 2)
            {
                // Draw(sourceID, BuiltinRenderTextureType.CameraTarget, PostProcessingPass.Copy);
                // commandBuffer.EndSample("Bloom");
                return false;
            }
            
            commandBuffer.BeginSample("Bloom");
            Vector4 threshold;
            threshold.x = Mathf.GammaToLinearSpace(bloomSettings.Threshold);
            threshold.y = threshold.x * bloomSettings.ThresholdKnee;
            threshold.z = 2f * threshold.y;
            threshold.w = 0.25f / (threshold.y + 0.00001f);
            threshold.y -= threshold.x;
            commandBuffer.SetGlobalVector(ShaderPropertyID.BloomThreshold, threshold);

            RenderTextureFormat rtFormat = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            commandBuffer.GetTemporaryRT(ShaderPropertyID.BloomPreFilter, width, height, 0, FilterMode.Bilinear, rtFormat);
            Draw(sourceID, ShaderPropertyID.BloomPreFilter, bloomSettings.FadeFireflies ? PostProcessingPass.BloomPreFilterFadeFireFlies : PostProcessingPass.BloomPreFilter);
            width /= 2;
            height /= 2;
            int fromID = ShaderPropertyID.BloomPreFilter;
            int toID = bloomIteration1PropertyID + 1;
            int i;

            for (i = 1; i <= bloomSettings.MaxBloomIterationCount; ++i)
            {
                if (width < bloomSettings.MinResolution || height < bloomSettings.MinResolution)
                {
                    break;
                }

                int middleID = toID - 1;
                commandBuffer.GetTemporaryRT(middleID, width, height, 0, FilterMode.Bilinear, rtFormat);
                commandBuffer.GetTemporaryRT(toID, width, height, 0, FilterMode.Bilinear, rtFormat);
                Draw(fromID, middleID, PostProcessingPass.BloomHorizontalBlur);
                Draw(middleID, toID, PostProcessingPass.BloomVerticalBlur);
                fromID = toID;
                toID += 2;
                width /= 2;
                height /= 2;
            }
            
            commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.BloomPreFilter);
            commandBuffer.SetGlobalFloat(ShaderPropertyID.BloomBicubicUpsampling, bloomSettings.UseBicubicUpsampling ? 1.0f : 0.0f);
            // commandBuffer.SetGlobalFloat(ShaderPropertyID.BloomIntensity, 1f); // only the final pass ues the intensity

            PostProcessingPass combinePass;
            PostProcessingPass finalPass;
            float finalIntensity;
            
            if (bloomSettings.mode == BloomSettings.BloomMode.Additive)
            {
                combinePass = PostProcessingPass.BloomAdditive;
                finalPass = combinePass;
                commandBuffer.SetGlobalFloat(ShaderPropertyID.BloomIntensity,  1f);
                finalIntensity = bloomSettings.Intensity;
            }

            else
            {
                combinePass = PostProcessingPass.BloomScattering;
                finalPass = PostProcessingPass.BloomScatteringFinal;
                commandBuffer.SetGlobalFloat(ShaderPropertyID.BloomIntensity, bloomSettings.Scatter);
                finalIntensity = Mathf.Min(bloomSettings.Intensity, 0.95f);
            }
            
            // todo : the greater can be removed or not
            if (i > 2) // means there has at least 2 iterations
            {
                // // this time the fromID is the last rt be written in above for loop
                commandBuffer.ReleaseTemporaryRT(fromID - 1);
                toID -= 5; // after blur, the toID is the last vertical pass rt id + 2, so -5 is the horizontal pass rt id before the last iteration

                // because the final pass is write to the camera target, so the loop need to be finish earlier and write the final result to the camera target manually
                for (i -= 2; i > 0; --i) 
                {
                    commandBuffer.SetGlobalTexture(ShaderPropertyID.PostProcessingSource2, toID +1); // toID + 1 is the vertical pass, as the high resolution rt
                    Draw(fromID, toID, combinePass);
                    commandBuffer.ReleaseTemporaryRT(fromID);
                    commandBuffer.ReleaseTemporaryRT(toID + 1);
                    fromID = toID;
                    toID -= 2; // -2 as the higher res horizontal pass
                }
            }

            else
            {
                commandBuffer.ReleaseTemporaryRT(bloomIteration1PropertyID);
            }
            
            commandBuffer.SetGlobalFloat(ShaderPropertyID.BloomIntensity, finalIntensity);
            commandBuffer.SetGlobalTexture(ShaderPropertyID.PostProcessingSource2, sourceID);
            commandBuffer.GetTemporaryRT(ShaderPropertyID.BloomResult, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, rtFormat);
            Draw(fromID, ShaderPropertyID.BloomResult, finalPass);
            commandBuffer.ReleaseTemporaryRT(fromID);
            commandBuffer.EndSample("Bloom");
            return true;
        }

        private void DoColorGradingAndToneMapping(int sourceID)
        {
            ConfigureColorAdjustments();
            ConfigureWhiteBalance();
            ConfigureSplitToning();
            DoToneMapping(sourceID);
        }
        
        private void ConfigureColorAdjustments()
        {
            ColorGradingSettings colorGradingSettings = postProcessingAsset.ColorGradingSettings;
            commandBuffer.SetGlobalVector(ShaderPropertyID.ColorAdjustmentsDataPropertyID, new Vector4(
                Mathf.Pow(2f, colorGradingSettings.PostExposure),
                colorGradingSettings.Contrast * 0.01f + 1f,
                colorGradingSettings.HueShift * (1f / 360f),
                colorGradingSettings.Saturation * 0.01f + 1f
            ));
            
            commandBuffer.SetGlobalColor(ShaderPropertyID.ColorFilterPropertyID, colorGradingSettings.ColorFilter.linear);
        }

        private void ConfigureWhiteBalance()
        {
            // LMS : It describes colors as the responses of the three photoreceptor cone types in the human eye (from catlike)
            // The tint can be used to compensate for undesired color balance, pushing the image toward either green or magenta (from catlike)
            WhiteBalanceSettings whiteBalanceSettings = postProcessingAsset.WhiteBalanceSettings;
            commandBuffer.SetGlobalVector(ShaderPropertyID.WhiteBalancePropertyID, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalanceSettings.Temperature, whiteBalanceSettings.Tint));
        }

        private void ConfigureSplitToning()
        {
            SplitToningSettings splitToningSettings = postProcessingAsset.SplitToningSettings;
            Color shadowColor = splitToningSettings.ShadowColor;
            shadowColor.a = splitToningSettings.Balance * 0.01f;
            commandBuffer.SetGlobalColor(ShaderPropertyID.SplitToningShadowColorPropertyID, shadowColor);
            commandBuffer.SetGlobalColor(ShaderPropertyID.SplitToningHighLightColorPropertyID, splitToningSettings.HighLightColor);
        }

        private void DoToneMapping(int sourceID)
        {
            ToneMappingSettings toneMappingSettings = postProcessingAsset.ToneMappingSettings;
            PostProcessingPass pass = PostProcessingPass.ToneMappingNone + (int)toneMappingSettings.Mode;
            Draw(sourceID, BuiltinRenderTextureType.CameraTarget, pass);
        }
        #endregion
    }
}