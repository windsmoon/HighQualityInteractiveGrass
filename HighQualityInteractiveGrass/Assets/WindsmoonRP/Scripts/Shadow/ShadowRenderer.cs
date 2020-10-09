using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UI;

namespace WindsmoonRP.Shadow
{
    public class ShadowRenderer
    {
        #region constants
        private const string bufferName = "Shadows";
        private const int maxDirectionalShadowCount = 4;
        private const int maxOtherShadaowCount = 16;
        private const int maxCascadeCount = 4;
        #endregion
        
        #region fields
        private CommandBuffer commandBuffer = new CommandBuffer { name = bufferName };
        private ScriptableRenderContext renderContext;
        private CullingResults cullingResults;
        private ShadowSettings shadowSettings;
        private DirectionalShadow[] directionalShadows = new DirectionalShadow[maxDirectionalShadowCount];
        private int currentDirectionalLightShadowCount;
        private OtherShadow[] otherShadows = new OtherShadow[maxOtherShadaowCount];
        private int currentOtherShadowCount;
        private Matrix4x4[] directionalShadowMatrices = new Matrix4x4[maxDirectionalShadowCount * maxCascadeCount];
        private Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxOtherShadaowCount];
        private static int cascadeCountPropertyID = Shader.PropertyToID("_CascadeCount");
        private static int cascadeCullingSpheresPropertyID = Shader.PropertyToID("_CascadeCullingSpheres");
//        private static int maxShadowDistancePropertyID = Shader.PropertyToID("_MaxShadowDistance");
        private static int shadowDistanceFadePropertyID = Shader.PropertyToID("_ShadowDistanceFade");
        private static int cascadeInfosPropertyID = Shader.PropertyToID("_CascadeInfos");
        private static int shadowMapSizePropertyID = Shader.PropertyToID("_ShadowMapSize");
        
        // macro in Shadow/ShadowSamplingTent.hlsl
        private static string[] directionalPCFKeywords =
        {
            "DIRECTIONAL_PCF3X3",
            "DIRECTIONAL_PCF5X5",
            "DIRECTIONAL_PCF7X7",
        };
        
        private static string[] otherPCFKeywords = 
        {
            "OTHER_PCF3X3",
            "OTHER_PCF5X5",
            "OTHER_PCF7X7",
        };

        private static string[] cascadeBlendKeywords =
        {
            "CASCADE_BLEND_SOFT",
            "CASCADE_BLEND_DITHER"
        };
        
        private static string[] shadowMaskKeywords = 
        {
            "SHADOW_MASK_ALWAYS",
            "SHADOW_MASK_DISTANCE"
        };
        
        private Vector4[] cascadeCullingSpheres = new Vector4[maxCascadeCount];
        private Vector4[] cascadeInfos = new Vector4[maxCascadeCount];
        private Vector4[] otherShadowTiles = new Vector4[maxOtherShadaowCount];
        private bool useShadowMask;
        private Vector4 shadowMapSize;
        #endregion

        #region methods
        public void Setup(ScriptableRenderContext renderContext, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            this.renderContext = renderContext;
            this.cullingResults = cullingResults;
            this.shadowSettings = shadowSettings;
            currentDirectionalLightShadowCount = 0;
            currentOtherShadowCount = 0;
            useShadowMask = false;
        }
        
        public Vector4 ReserveDirectionalShadows(Light light, int visibleLightIndex)
        {
            if (currentDirectionalLightShadowCount < maxDirectionalShadowCount && light.shadows != LightShadows.None && light.shadowStrength > 0f) 
            {
                float maskChannel;
                LightBakingOutput lightBakingOutput = light.bakingOutput;
                
                if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed && lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
                {
                    useShadowMask = true;
                    maskChannel = lightBakingOutput.occlusionMaskChannel; // can not use the light index because it may be changed in runtime
                }

                else
                {
                    maskChannel = -1;
                }
                
                // GetShadowCasterBounds now returns true for directional lights even when there is nothing within the shadow range
                if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) 
                {
                    // the shadow strength of light is negative, see GetDirectionalShadowAttenuation in WindsmoonShadow.hlsl
                    // if the stength is positive, the shader may be handle the shadow as realtime shadow
                    return new Vector4(-light.shadowStrength, 0f, 0, maskChannel);
                }
                
                directionalShadows[currentDirectionalLightShadowCount] = new DirectionalShadow(){visibleLightIndex = visibleLightIndex, slopeScaleBias = light.shadowBias, nearPlaneOffset = light.shadowNearPlane};
                return new Vector4(light.shadowStrength, shadowSettings.DirectionalShadowSetting.CascadeCount * currentDirectionalLightShadowCount++, light.shadowNormalBias, maskChannel);
            }
            
            return new Vector4(0f, 0f, 0f, -1);
        }

        public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
        {
            if (light.shadows == LightShadows.None || light.shadowStrength <= 0)
            {
                return new Vector4(0f, 0f, 0f, -1f);
            }

            int occlusionMaskChannel = -1;
            LightBakingOutput lightBakingOutput = light.bakingOutput;
           
            if (lightBakingOutput.lightmapBakeType == LightmapBakeType.Mixed && lightBakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                occlusionMaskChannel = lightBakingOutput.occlusionMaskChannel;
            }

            bool isPoint = light.type == LightType.Point;
            int newLightCount = currentOtherShadowCount + (isPoint ? 6 : 1);
            
            if (newLightCount >= maxOtherShadaowCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            {
                // the shadow strength of light is negative, see GetDirectionalShadowAttenuation in WindsmoonShadow.hlsl
                // if the stength is positive, the shader may be handle the shadow as realtime shadow
                return new Vector4(-light.shadowStrength, 0f, 0f, occlusionMaskChannel);
            }
            
            otherShadows[currentOtherShadowCount] = new OtherShadow()
            {
                VisibleLightIndex = visibleLightIndex,
                SlopeScaleBias = light.shadowBias,
                NoramlBias = light.shadowNormalBias,
                IsPoint = isPoint
            };
            
            Vector4 data = new Vector4(light.shadowStrength, currentOtherShadowCount, isPoint ? 1f : 0f, occlusionMaskChannel);
            currentOtherShadowCount = newLightCount;
            return data;
        }

        public void Render()
        {
            if (currentDirectionalLightShadowCount > 0)
            {
                RenderDirectionalShadow();
            }

            if (currentOtherShadowCount > 0)
            {
                RenderOtherShadow();
            }
            
            commandBuffer.BeginSample(bufferName);
            SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0 : 1 : -1);
            
            // direction and other light use the same way to fade shadow
            commandBuffer.SetGlobalInt(cascadeCountPropertyID, currentDirectionalLightShadowCount > 0 ? shadowSettings.DirectionalShadowSetting.CascadeCount : 0);
            float cascadefade = 1 - shadowSettings.DirectionalShadowSetting.CascadeFade;
            commandBuffer.SetGlobalVector(shadowDistanceFadePropertyID, new Vector4(1 / shadowSettings.MaxDistance, 1 / shadowSettings.DistanceFade, 1f / (1f - cascadefade * cascadefade)));
            
            commandBuffer.SetGlobalVector(shadowMapSizePropertyID, shadowMapSize);            
            commandBuffer.EndSample(bufferName);
            ExecuteBuffer();
        }

        public void Cleanup()
        {
            if (currentDirectionalLightShadowCount > 0)
            {
                commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.DirectionalShadowMap);
                ExecuteBuffer();    
            }

            if (currentOtherShadowCount > 0)
            {
                commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.OtherShadowMap);
                ExecuteBuffer();
            }
        }

        private void RenderDirectionalShadow()
        {
            int directionalShadowMapSize = (int)shadowSettings.DirectionalShadowSetting.ShadowMapSize;
            shadowMapSize.x = directionalShadowMapSize;
            shadowMapSize.y = 1f / directionalShadowMapSize;
            commandBuffer.GetTemporaryRT(ShaderPropertyID.DirectionalShadowMap, directionalShadowMapSize, directionalShadowMapSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commandBuffer.SetRenderTarget(ShaderPropertyID.DirectionalShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalInt(ShaderPropertyID.ShadowPancaking, 1);
            commandBuffer.BeginSample(bufferName);
            ExecuteBuffer();

            int tileCount = currentDirectionalLightShadowCount * shadowSettings.DirectionalShadowSetting.CascadeCount;
            // todo : squared tile will waste texture space
            int splitCount = tileCount <= 1 ? 1 : tileCount <= 4 ? 2 : 4; // max tile count is 4 x 4 = 16, now the tile is squared
            int tileSize = directionalShadowMapSize / splitCount;

            // ?? : An alternative approach is to apply a slope-scale bias, which is done by using a nonzero value for the second argument of SetGlobalDepthBias.
            // This value is used to scale the highest of the absolute clip-space depth derivative along the X and Y dimensions.
            // So it is zero for surfaces that are lit head-on, it's 1 when the light hits at a 45° angle in at least one of the two dimensions, and approaches infinity when the dot product of the surface normal and light direction reaches zero.
            // So the bias increases automatically when more is needed, but there's no upper bound. 
            //commandBuffer.SetGlobalDepthBias(0f, 3f); // ??
            
            for (int i = 0; i < currentDirectionalLightShadowCount; ++i)
            {
                RenderDirectionalShadow(i, splitCount, tileSize); // this methods also set global shader properties
            }
            
//            commandBuffer.SetGlobalDepthBias(0f, 0f);
            // commandBuffer.SetGlobalInt(cascadeCountPropertyID, shadowSettings.DirectionalShadowSetting.CascadeCount);
            commandBuffer.SetGlobalVectorArray(cascadeCullingSpheresPropertyID, cascadeCullingSpheres);
            commandBuffer.SetGlobalVectorArray(cascadeInfosPropertyID, cascadeInfos);
            commandBuffer.SetGlobalMatrixArray(ShaderPropertyID.DirectionalShadowMatrices, directionalShadowMatrices);
//            commandBuffer.SetGlobalFloat(maxShadowDistancePropertyID, shadowSettings.MaxDistance);
            // float cascadefade = 1 - shadowSettings.DirectionalShadowSetting.CascadeFade;
            // commandBuffer.SetGlobalVector(shadowDistanceFadePropertyID, new Vector4(1 / shadowSettings.MaxDistance, 1 / shadowSettings.DistanceFade, 1f / (1f - cascadefade * cascadefade)));
            // SetDirectionalShadowKeyword();
            SetKeywords(directionalPCFKeywords, (int)shadowSettings.DirectionalShadowSetting.PCFMode - 1);
            SetKeywords(cascadeBlendKeywords, (int)shadowSettings.DirectionalShadowSetting.CascadeBlendMode - 1);
            commandBuffer.EndSample(bufferName);
            ExecuteBuffer();
        }

        private void RenderOtherShadow()
        {
            int otherShadowMapSize = (int)shadowSettings.OtherShadowSettings.ShadowMapSize;
            shadowMapSize.z = otherShadowMapSize;
            shadowMapSize.w = 1f / otherShadowMapSize;
            commandBuffer.GetTemporaryRT(ShaderPropertyID.OtherShadowMap, otherShadowMapSize, otherShadowMapSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            commandBuffer.SetRenderTarget(ShaderPropertyID.OtherShadowMap, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.SetGlobalInt(ShaderPropertyID.ShadowPancaking, 0);
            commandBuffer.SetGlobalInt(ShaderPropertyID.ShadowPancaking, 0);
            commandBuffer.BeginSample(bufferName);
            ExecuteBuffer();

            int tileCount = currentOtherShadowCount;
            // todo : squared tile will waste texture space
            // todo : unlike the cascaded shadow, there could have at most 16 of the other shadow, so the splitCount is not perfect
            int splitCount = tileCount <= 1 ? 1 : tileCount <= 4 ? 2 : 4; // max tile count is 4 x 4 = 16, now the tile is squared
            int tileSize = otherShadowMapSize / splitCount;

            // ?? : An alternative approach is to apply a slope-scale bias, which is done by using a nonzero value for the second argument of SetGlobalDepthBias.
            // This value is used to scale the highest of the absolute clip-space depth derivative along the X and Y dimensions.
            // So it is zero for surfaces that are lit head-on, it's 1 when the light hits at a 45° angle in at least one of the two dimensions, and approaches infinity when the dot product of the surface normal and light direction reaches zero.
            // So the bias increases automatically when more is needed, but there's no upper bound. 
            //commandBuffer.SetGlobalDepthBias(0f, 3f); // ??

            for (int i = 0; i < currentOtherShadowCount;)
            {
                if (otherShadows[i].IsPoint)
                {
                    RenderPointShadow(i, splitCount, tileSize);
                    i += 6;
                }

                else
                {
                    RenderSpotShadow(i, splitCount, tileSize);
                    i += 1;
                }
            }
            
            commandBuffer.SetGlobalMatrixArray(ShaderPropertyID.OtherShadowMatrices, otherShadowMatrices);
            commandBuffer.SetGlobalVectorArray(ShaderPropertyID.OtherShadowTiles, otherShadowTiles);
            SetKeywords(otherPCFKeywords, (int)shadowSettings.OtherShadowSettings.PCFMode - 1);
            commandBuffer.EndSample(bufferName);
            ExecuteBuffer();
        }
        
        private void RenderDirectionalShadow(int index, int splitCount, int tileSize)
        {
            DirectionalShadow directionalShadow = directionalShadows[index];
            ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, directionalShadow.visibleLightIndex);

            int cascadeCount = shadowSettings.DirectionalShadowSetting.CascadeCount;
            int tileOffset = index * cascadeCount;
            Vector3 cascadeRatios = shadowSettings.DirectionalShadowSetting.CascadeRatios;
            float cascadeCullingFactor = Mathf.Max(0f, 0.8f - shadowSettings.DirectionalShadowSetting.CascadeFade); // control how much shadow casters will cast shadow in larger cascade
            float inversedSplitCount = 1f / splitCount;

            for (int i = 0; i < cascadeCount; ++i)
            {
                // note : the split data contains information about how shadow caster objects should be culled
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(directionalShadow.visibleLightIndex, i, cascadeCount,
                    cascadeRatios, tileSize, directionalShadow.nearPlaneOffset, out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
                shadowSplitData.shadowCascadeBlendCullingFactor = cascadeCullingFactor;
                shadowDrawingSettings.splitData = shadowSplitData;

                if (index == 0) // set culling spheres, all directional light use only one group of culling spheres
                {
                    // note :  as the shadow projections are orthographic and square they end up closely fitting their culling sphere, but also cover some space around them
                    // that's why some shadows can be seen outside the culling regions
                    // also the light direction doesn't matter to the sphere, so all directional lights end up using the same culling spheres
                    // the camera is not at the sphere's center, but the surface of the sphere, all spheres will intersect at this point
                    Vector4 cullingSphere = shadowSplitData.cullingSphere; // w means sphere's radius
                    SetCascadeInfo(i, cullingSphere, tileSize);
                }

                int tileIndex = tileOffset + i;
                SetShadowMapViewport(tileIndex, splitCount, tileSize, out Vector2 offset);
                // Matrix4x4 vpMatrix = projectionMatrix * viewMatrix
                directionalShadowMatrices[tileIndex] = ConvertClipSpaceToTileSpace(projectionMatrix * viewMatrix, offset, inversedSplitCount);
                // directionalShadowMatrices[index] = projectionMatrix * viewMatrix;
                commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                commandBuffer.SetGlobalDepthBias(0f, directionalShadow.slopeScaleBias);
                ExecuteBuffer();
                renderContext.DrawShadows(ref shadowDrawingSettings);
                commandBuffer.SetGlobalDepthBias(0f, 0f);
            }
        }

        public void RenderSpotShadow(int index, int splitCount, int tileSize)
        {
            OtherShadow otherShadow = otherShadows[index];
            ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, otherShadow.VisibleLightIndex);
            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(otherShadow.VisibleLightIndex,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
            shadowDrawingSettings.splitData = shadowSplitData;
            SetShadowMapViewport(index, splitCount, tileSize, out Vector2 offset);
            float inversedSplitCount = 1f / splitCount;
            
            // !!
            // ??
            float texelSize = 2f / (tileSize * projectionMatrix.m00);
            float filterSize = texelSize * ((float)shadowSettings.OtherShadowSettings.PCFMode + 1f);
            float bias = otherShadow.NoramlBias * filterSize * 1.4142136f;
            SetOtherTileData(index, offset, inversedSplitCount, bias);
            
            otherShadowMatrices[index] = ConvertClipSpaceToTileSpace(projectionMatrix * viewMatrix, offset, inversedSplitCount);
            commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            commandBuffer.SetGlobalDepthBias(0f, otherShadow.SlopeScaleBias);
            ExecuteBuffer();
            renderContext.DrawShadows(ref shadowDrawingSettings);
            commandBuffer.SetGlobalDepthBias(0f, 0f);
        }

        public void RenderPointShadow(int index, int splitCount, int tileSize)
        {
            OtherShadow otherShadow = otherShadows[index];
            ShadowDrawingSettings shadowDrawingSettings = new ShadowDrawingSettings(cullingResults, otherShadow.VisibleLightIndex);

            // !!
            // ??
            float texelSize = 2f / tileSize;
            float filterSize = texelSize * ((float)shadowSettings.OtherShadowSettings.PCFMode + 1f);
            float bias = otherShadow.NoramlBias * filterSize * 1.4142136f;
            float inversedSplitCount = 1f / splitCount;
            float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;

            for (int i = 0; i < 6; ++i)
            {
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(otherShadow.VisibleLightIndex, (CubemapFace)i, fovBias,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData shadowSplitData);
                
                // ??
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
                
                shadowDrawingSettings.splitData = shadowSplitData;
                int tileIndex = index + i;
                
                SetShadowMapViewport(tileIndex, splitCount, tileSize, out Vector2 offset);
                SetOtherTileData(tileIndex, offset, inversedSplitCount, bias);
                otherShadowMatrices[tileIndex] = ConvertClipSpaceToTileSpace(projectionMatrix * viewMatrix, offset, inversedSplitCount);

                commandBuffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
                commandBuffer.SetGlobalDepthBias(0f, otherShadow.SlopeScaleBias);
                ExecuteBuffer();
                renderContext.DrawShadows(ref shadowDrawingSettings);
                commandBuffer.SetGlobalDepthBias(0f, 0f);
            }
        }
        
        private void SetOtherTileData(int index, Vector2 offset, float inversedSplitCount, float bias)
        {
            float halfTexel = shadowMapSize.w * 0.5f; // half texel
            Vector4 data;
            data.x = offset.x * inversedSplitCount + halfTexel; // left bound, move left half texel so that the coord is always in the bound 
            data.y = offset.y * inversedSplitCount + halfTexel; // like x
            data.z = inversedSplitCount - halfTexel - halfTexel;  // z means 1 / split count, it is the tile length in 01, so it need to be move left one texel, otherwise the rigth bound will over the tile
            data.w = bias;
            otherShadowTiles[index] = data;
        }

        private void SetKeywords(string[] keywords, int enableIndex)
        {
            for (int i = 0; i < keywords.Length; ++i)
            {
                if (i == enableIndex)
                {
                    commandBuffer.EnableShaderKeyword(keywords[i]);
                }

                else
                {
                    commandBuffer.DisableShaderKeyword(keywords[i]);
                }
            }
        }
        
        // private void SetDirectionalShadowKeyword()
        // {
        //     int pcfIndex = (int) shadowSettings.DirectionalShadowSetting.PCFMode - 1;
        //
        //     for (int i = 0; i < directionalPCFKeywords.Length; ++i)
        //     {
        //         if (i == pcfIndex)
        //         {
        //             commandBuffer.EnableShaderKeyword(directionalPCFKeywords[i]);
        //         }
        //
        //         else
        //         {
        //             commandBuffer.DisableShaderKeyword(directionalPCFKeywords[i]);
        //         }
        //     }
        // }

        private void SetCascadeInfo(int index, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            // ??
            //Increasing the filter size makes shadows smoother, but also causes acne to appear again.
            //We have to increase the normal bias to match the filter size.
            //We can do this automatically by multiplying the texel size by one plus the filter mode in SetCascadeData.
            float filterSize = texelSize * ((float)shadowSettings.DirectionalShadowSetting.PCFMode + 1f); // ??
            
            // ??
            //Besides that, increasing the sample region also means that we can end up sampling outside of the cascade's culling sphere.
            //We can avoid that by reducing the sphere's radius by the filter size before squaring it.
            cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeInfos[index] = new Vector4(1f / cullingSphere.w, filterSize * 1.4142136f);
            cascadeCullingSpheres[index] = cullingSphere;
        }

        private void SetShadowMapViewport(int index, int split, float tileSize, out Vector2 offset)
        {
            // left to right then up to down / down to up
            offset = new Vector2(index % split, index / split);
            commandBuffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        }

        // ??
        private Matrix4x4 ConvertClipSpaceToTileSpace(Matrix4x4 matrix, Vector2 offset, float inversedSplitCount)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                matrix.m20 = -matrix.m20;
                matrix.m21 = -matrix.m21;
                matrix.m22 = -matrix.m22;
                matrix.m23 = -matrix.m23;
            }
            
            matrix.m00 = (0.5f * (matrix.m00 + matrix.m30) + offset.x * matrix.m30) * inversedSplitCount;
            matrix.m01 = (0.5f * (matrix.m01 + matrix.m31) + offset.x * matrix.m31) * inversedSplitCount;
            matrix.m02 = (0.5f * (matrix.m02 + matrix.m32) + offset.x * matrix.m32) * inversedSplitCount;
            matrix.m03 = (0.5f * (matrix.m03 + matrix.m33) + offset.x * matrix.m33) * inversedSplitCount;
            matrix.m10 = (0.5f * (matrix.m10 + matrix.m30) + offset.y * matrix.m30) * inversedSplitCount;
            matrix.m11 = (0.5f * (matrix.m11 + matrix.m31) + offset.y * matrix.m31) * inversedSplitCount;
            matrix.m12 = (0.5f * (matrix.m12 + matrix.m32) + offset.y * matrix.m32) * inversedSplitCount;
            matrix.m13 = (0.5f * (matrix.m13 + matrix.m33) + offset.y * matrix.m33) * inversedSplitCount;
            matrix.m20 = 0.5f * (matrix.m20 + matrix.m30);
            matrix.m21 = 0.5f * (matrix.m21 + matrix.m31);
            matrix.m22 = 0.5f * (matrix.m22 + matrix.m32);
            matrix.m23 = 0.5f * (matrix.m23 + matrix.m33);
            return matrix;
        }
        
        private void ExecuteBuffer() 
        {
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        #endregion

        #region structs
        private struct DirectionalShadow
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset; // to solve shadow pancaking
        }
        
        private struct OtherShadow
        {
            public int VisibleLightIndex;
            public float SlopeScaleBias;
            public float NoramlBias;
            public bool IsPoint;
        }
        #endregion
    }
}