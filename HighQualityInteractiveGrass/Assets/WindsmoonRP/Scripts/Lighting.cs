using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using WindsmoonRP.Shadow;


namespace WindsmoonRP
{
    public class Lighting // todo : rename to LightRenderer
    {
        #region contants
        private static string lightsPerObjectKeyword = "LIGHTS_PER_OBJECT";
        private const string bufferName = "Lighting";
        private const int maxDirectionalLightCount = 4;
        private const int maxOtherLightCount = 64;
        #endregion
        
        #region fields
        private CommandBuffer commandBuffer = new CommandBuffer();
        private static int directionalLightColorsPropertyID = Shader.PropertyToID("_DirectionalLightColors");
        private static int directionalLightDirectionsPropertyID = Shader.PropertyToID("_DirectionalLightDirections");
        private static int directionalLightCountPropertyID = Shader.PropertyToID("_DirectionalLightCount");
        private static int directionalShadowInfosPropertyID = Shader.PropertyToID("_DirectionalShadowInfos");

        private static Vector4[] directionalLightColors = new Vector4[maxDirectionalLightCount]; 
        private static Vector4[] directionalLightDirections = new Vector4[maxDirectionalLightCount];
        private static Vector4[] _DirectionalShadowInfos = new Vector4[maxDirectionalLightCount];
        
        private static int otherLightColorsPropertyID = Shader.PropertyToID("_OtherLightColors");
        private static int otherLightPositionsProoertyID = Shader.PropertyToID("_OtherLightPositions");
        private static int otherLightCountPropertyID = Shader.PropertyToID("_OtherLightCount");
        private static int otherLightDirectionsPropertyID = Shader.PropertyToID("_OtherLightDirections");
        private static int otherLightSpotAnglesPropertyID = Shader.PropertyToID("_OtherLightSpotAngles");
        private static int otherLightShadowDatasPropertyID = Shader.PropertyToID("_OtherLightShadowDatas");
            
        private static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
        private static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
        private static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
        private static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
        private static Vector4[] otherLightShadowDatas = new Vector4[maxOtherLightCount];

        private CullingResults cullingResults;
        private ShadowRenderer shadowRenderer = new ShadowRenderer();
        #endregion
        
        #region methods
        public void Setup(ScriptableRenderContext renderContext, CullingResults cullingResults, ShadowSettings shadowSettings, bool useLightsPerObject)
        {
            this.cullingResults = cullingResults;
            commandBuffer.BeginSample(bufferName);
            shadowRenderer.Setup(renderContext, cullingResults, shadowSettings);
            SetupLights(useLightsPerObject);
            shadowRenderer.Render();
            commandBuffer.EndSample(bufferName);
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        public void Cleanup()
        {
            shadowRenderer.Cleanup();
        }
        
        private void SetupLights(bool useLightsPerObject)
        {
            // lightIndexMap contains light indices, matching the visible light indices plus all other active lights in the scene (from catlike)
            // we only use lightIndexMap for point and spot light
            NativeArray<int> lightIndexMap = useLightsPerObject ? cullingResults.GetLightIndexMap(Allocator.Temp) : default;
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            int directionalLightCount = 0;
            int otherLightCount = 0;
            int i = 0;

            for (; i < visibleLights.Length; ++i)
            {
                int newIndex = -1;
                VisibleLight visibleLight = visibleLights[i];

                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                    {
                        if (directionalLightCount < maxDirectionalLightCount)
                        {
                            SetupDirectionalLight(directionalLightCount++, i, ref visibleLight);
                        }
                        
                        break;
                    }

                    case LightType.Point:
                    {
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, i, ref visibleLight);
                        }
                        
                        break;
                    }

                    case LightType.Spot:
                    {
                        if (otherLightCount < maxOtherLightCount)
                        {
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, i, ref visibleLight);
                        }
                        
                        break;
                    }
                }

                if (useLightsPerObject)
                {
                    lightIndexMap[i] = newIndex;
                }
            }
            
            if (useLightsPerObject) 
            {
                for (; i < lightIndexMap.Length; ++i)
                {
                    lightIndexMap[i] = -1;
                }
                //
                cullingResults.SetLightIndexMap(lightIndexMap);
                Shader.EnableKeyword(lightsPerObjectKeyword);
                lightIndexMap.Dispose();
            }

            else
            {
                Shader.DisableKeyword(lightsPerObjectKeyword);
            }
            
            // Light light = RenderSettings.sun;
            // commandBuffer.SetGlobalVector(directionalLightColorPropertyID, light.color.linear * light.intensity);
            // commandBuffer.SetGlobalVector(directionalLightDirectionPropertyID, -light.transform.forward);

            commandBuffer.SetGlobalInt(directionalLightCountPropertyID, directionalLightCount);

            if (directionalLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(directionalLightColorsPropertyID, directionalLightColors);
                commandBuffer.SetGlobalVectorArray(directionalLightDirectionsPropertyID, directionalLightDirections);
                commandBuffer.SetGlobalVectorArray(directionalShadowInfosPropertyID, _DirectionalShadowInfos);
            }
            
            commandBuffer.SetGlobalInt(otherLightCountPropertyID, otherLightCount);

            if (otherLightCount > 0)
            {
                commandBuffer.SetGlobalVectorArray(otherLightColorsPropertyID, otherLightColors);
                commandBuffer.SetGlobalVectorArray(otherLightPositionsProoertyID, otherLightPositions);
                commandBuffer.SetGlobalVectorArray(otherLightDirectionsPropertyID, otherLightDirections);
                commandBuffer.SetGlobalVectorArray(otherLightSpotAnglesPropertyID, otherLightSpotAngles);
                commandBuffer.SetGlobalVectorArray(otherLightShadowDatasPropertyID, otherLightShadowDatas);
            }
        }

        private void SetupDirectionalLight(int index, int visibleIndex, ref VisibleLight visiblelight)
        {
            directionalLightColors[index] = visiblelight.finalColor; // final color already usedthe light's intensity
            directionalLightDirections[index] = -visiblelight.localToWorldMatrix.GetColumn(2); // ?? remeber to revise
            _DirectionalShadowInfos[index] = shadowRenderer.ReserveDirectionalShadows(visiblelight.light, visibleIndex);
        }
        
        private void SetupPointLight (int index, int visibleIndex, ref VisibleLight visibleLight) 
        {
            otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            otherLightPositions[index] = position;
            otherLightDirections[index] = Vector4.zero;
            otherLightSpotAngles[index] = new Vector4(0f, 1f);
            otherLightShadowDatas[index] = shadowRenderer.ReserveOtherShadows(visibleLight.light, visibleIndex);
        }
        
        private void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight) 
        {
            otherLightColors[index] = visibleLight.finalColor;
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            otherLightPositions[index] = position;
            otherLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2); // ?? remeber to revise
            
            Light light = visibleLight.light;
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);
            otherLightShadowDatas[index] = shadowRenderer.ReserveOtherShadows(visibleLight.light, visibleIndex);
        }
        #endregion
    }
}