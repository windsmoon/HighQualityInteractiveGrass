using System.Net.Configuration;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using WindsmoonRP.PostProcessing;
using WindsmoonRP.Shadow;

namespace WindsmoonRP
{
    public class CameraRenderer
    {
        #region constants
        private const string defaultCommandBufferName = "Camera Renderer";
        #endregion

        #region fields
        private ScriptableRenderContext renderContext;
        private Camera camera;
        private bool useHDR;
        private CommandBuffer commandBuffer = new CommandBuffer();
        private CullingResults cullingResults;
        private static ShaderTagId unlitShaderTagID = new ShaderTagId("SRPDefaultUnlit");
        private static ShaderTagId litShaderTagID = new ShaderTagId("WindsmoonLit");
        private static int cameraFrameBufferPropertyID = Shader.PropertyToID("_CameraFrameBuffer");
        private string commandBufferName;
        private Lighting lighting = new Lighting();
        private PostProcessingStack postProcessingStack = new PostProcessingStack();

        #if UNITY_EDITOR || DEBUG
        private static ShaderTagId[] legacyShaderTagIDs = 
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        private static Material errorMaterial;
        #endif
        #endregion
        
        #region methods
        public void Render(ScriptableRenderContext renderContext, Camera camera, bool allowHDR, bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject, ShadowSettings shadowSettings, PostProcessingAsset postProcessingAsset)
        {
            this.renderContext = renderContext;
            this.camera = camera;
            
            #if UNITY_EDITOR || DEBUG
            SetCommandBufferName();
            DrawSceneView(); // draw it before culling or it may be culled
            #endif
            
            if (Cull(shadowSettings.MaxDistance) == false)
            {
                return;
            }

            useHDR = allowHDR && camera.allowHDR;
            commandBuffer.BeginSample(commandBufferName);
            ExecuteCommandBuffer(); // ?? why do this ? maybe begin sample must be execute before next sample
            lighting.Setup(renderContext, cullingResults, shadowSettings, useLightsPerObject);
            postProcessingStack.Setup(renderContext, camera, postProcessingAsset, useHDR);
            commandBuffer.EndSample(commandBufferName);
            Setup(shadowSettings);
            DrawVisibleObjects(useDynamicBatching, useGPUInstancing, useLightsPerObject);

            #if UNITY_EDITOR || DEBUG
            DrawUnsupportedShaderObjects();
            DrawGizmos(true);
            // DrawGizmos(false);
            #endif

            if (postProcessingStack.IsActive)
            {
                postProcessingStack.Render(cameraFrameBufferPropertyID);
            }
            
            #if UNITY_EDITOR || DEBUG
            DrawGizmos(false);
            #endif

            Cleanup();
            Submit();
        }

        private bool Cull(float maxShadowDistance)
        {
            ScriptableCullingParameters scriptableCullingParameters;
            
            if (camera.TryGetCullingParameters(out scriptableCullingParameters)) // note: this method check if camera setting is invalid, return false 
            {
                scriptableCullingParameters.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
                cullingResults = renderContext.Cull(ref scriptableCullingParameters);
                return true;
            }

            return false;
        }
        
        private void Setup(ShadowSettings shadowSettings)
        {
            renderContext.SetupCameraProperties(camera); // ?? this method must be called before excute commandbuffer, or clear command will call GL.Draw to clear
            CameraClearFlags cameraClearFlags = camera.clearFlags;

            if (postProcessingStack.IsActive)
            {
                // clear color and depth unless use the skybox clear flag (sky box flag is the smallest value of the clearFlags enum)
                if (cameraClearFlags > CameraClearFlags.Color)
                {
                    cameraClearFlags = CameraClearFlags.Color;
                }
                
                commandBuffer.GetTemporaryRT(cameraFrameBufferPropertyID, camera.pixelWidth, camera.pixelHeight, 32, 
                    FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                
                commandBuffer.SetRenderTarget(cameraFrameBufferPropertyID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            }
            
            commandBuffer.ClearRenderTarget(cameraClearFlags <= CameraClearFlags.Depth, cameraClearFlags == CameraClearFlags.Color, cameraClearFlags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear); // ?? tbdr resolve
            commandBuffer.BeginSample(commandBufferName);
            ExecuteCommandBuffer();
        }
        
        private void Cleanup()
        {
            lighting.Cleanup();

            if (postProcessingStack.IsActive)
            {
                commandBuffer.ReleaseTemporaryRT(cameraFrameBufferPropertyID);
            }
        }
        
        private void DrawVisibleObjects(bool useDynamicBatching, bool useGPUInstancing, bool useLightsPerObject)
        {
            PerObjectData lightsPerObjectFlag = useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
            SortingSettings sortingSettings = new SortingSettings(camera) {criteria = SortingCriteria.CommonOpaque};
            // Lightmaps enable LIGHTMAP_ON, ShadowMask enable shadow mask texture, OcclusionProbe enable unity_ProbesOcclusion
            DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagID, sortingSettings)
                {enableDynamicBatching = useDynamicBatching, enableInstancing = useGPUInstancing, 
                    perObjectData = PerObjectData.Lightmaps | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume 
                                    | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume | PerObjectData.ReflectionProbes | lightsPerObjectFlag};
            drawingSettings.SetShaderPassName(1, litShaderTagID);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            renderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            renderContext.DrawSkybox(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            renderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        #if UNITY_EDITOR || DEBUG
        private void DrawSceneView()
        {
            if (camera.cameraType != CameraType.SceneView)
            {
                return;
            }
            
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
        
        private void DrawUnsupportedShaderObjects()
        {
            if (errorMaterial == null)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }

            DrawingSettings drawingSettings = new DrawingSettings();
            drawingSettings.sortingSettings = new SortingSettings(camera);
            drawingSettings.overrideMaterial = errorMaterial;
            
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            for (int i = 0; i < legacyShaderTagIDs.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIDs[i]);
            }
            
            renderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void DrawGizmos(bool isBeforePostProcessing) // ?? nothing happened, do not call this methods also has gizmos
        {
            if (Handles.ShouldRenderGizmos() == false)
            {
                return;
            }

            if (isBeforePostProcessing)
            {
                renderContext.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            }

            else
            {
                renderContext.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
        #endif

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
        
        private void SetCommandBufferName()
        {
            #if UNITY_EDITOR || DEBUG
            Profiler.BeginSample("Editor Only");
            commandBufferName = camera.name;
            Profiler.EndSample();
            #else
            commandBufferName = defaultCommandBufferName;
            #endif

            commandBuffer.name = commandBufferName;
        }
        #endregion
    }
}
