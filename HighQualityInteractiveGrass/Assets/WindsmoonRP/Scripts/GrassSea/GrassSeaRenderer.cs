using UnityEngine;
using UnityEngine.Rendering;

namespace WindsmoonRP.GrassSea
{
    public class GrassSeaRenderer
    {
        #region contants
        private const string commandBufferName = "Grass Sea";
        #endregion
        
        #region fields
        private CommandBuffer commandBuffer = new CommandBuffer() {name = commandBufferName};
        private Vector2 windUVOffset = new Vector2(0, 0);
        private ScriptableRenderContext renderContext;
        private GrassSeaConfig grassSeaConfig;

        private int windNoisePropertyID = Shader.PropertyToID("_WindNoise");
        private int uniformWindPropertyID = Shader.PropertyToID("_UniformWindEffect");
        private int worldRectPropertyID = Shader.PropertyToID("_worldRect");
        private int uvOffsetPropertyID = Shader.PropertyToID("_uvOffset");
        private int stablityPropertyID = Shader.PropertyToID("_Stability");
        #endregion

        #region methods
        public void Render(ScriptableRenderContext renderContext, GrassSeaConfig grassSeaConfig)
        {
            this.renderContext = renderContext;
            this.grassSeaConfig = grassSeaConfig;
            SetWindField();
            Submit();
            // commandBuffer.GetTemporaryRT(ShaderPropertyID.OtherShadowMap, otherShadowMapSize, otherShadowMapSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            // commandBuffer.SetRenderTarget(ShaderPropertyID.OtherShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        }

        private void SetWindField()
        {
            WindFieldSettings windFieldSettings = grassSeaConfig.WindFieldSettings;
            Vector3 uniformWindDirection = windFieldSettings.UniformWindDirection;
            Vector2 windDirectionXZ = new Vector2(uniformWindDirection.x, uniformWindDirection.z).normalized;
            float uniformWindForce = windFieldSettings.UniformWindForce;
            windUVOffset -= windDirectionXZ * uniformWindForce * 0.3f * Time.deltaTime;
            windUVOffset = new Vector2(windUVOffset.x - Mathf.Floor(windUVOffset.x), windUVOffset.y - Mathf.Floor(windUVOffset.y));
            commandBuffer.SetGlobalTexture(windNoisePropertyID, windFieldSettings.WindNoise);
            commandBuffer.SetGlobalVector(uniformWindPropertyID, new Vector4(windDirectionXZ.x, 0, windDirectionXZ.y, uniformWindForce));
            Rect worldRect = windFieldSettings.WorldRect;
            commandBuffer.SetGlobalVector(worldRectPropertyID, new Vector4(worldRect.x, worldRect.y, worldRect.width, worldRect.height));
            commandBuffer.SetGlobalVector(uvOffsetPropertyID, windUVOffset);
            commandBuffer.SetGlobalFloat(stablityPropertyID, windFieldSettings.Stablility);
        }

        private void Submit()
        {
            commandBuffer.EndSample(commandBufferName);
            ExecuteCommandBuffer();
            renderContext.Submit();
        }

        private void ExecuteCommandBuffer()
        {
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        #endregion
    }
}