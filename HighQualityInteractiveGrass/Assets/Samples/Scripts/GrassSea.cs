using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GrassSea : MonoBehaviour
{
    #region fields
    [SerializeField]
    private Texture2D windNoise;
    [SerializeField]
    private Vector3 uniformWindDirection;
    [SerializeField, Range(0, 1)]
    private float uniformWindForce;
    [SerializeField]    
    private Rect worldRect;
    [SerializeField, Range(0, 1)]
    private float stablility = 0;
    [SerializeField]
    private Mesh areaMesh;
    [SerializeField]
    private Material fireAreaMaterial;
    private Vector2 uvOffset = new Vector2(0, 0);
    private Transform fireRoot;
    private CommandBuffer commandBuffer;
    private int areaPropertyID = Shader.PropertyToID("Area");
    #endregion

    #region unity methods
    private void Awake()
    {
        fireRoot = transform.Find("FireRoot");
    }

    private void OnValidate()
    {
        // uniformWindDirection.y = 0;
        // uniformWindDirection = uniformWindDirection.normalized;
    }

    private void Update()
    {
        SetFirePosition();
        
        Vector2 windDirectionXZ = new Vector2(uniformWindDirection.x, uniformWindDirection.z).normalized;
        uvOffset -= windDirectionXZ * uniformWindForce * 0.3f * Time.deltaTime;
        uvOffset = new Vector2(uvOffset.x - Mathf.Floor(uvOffset.x), uvOffset.y - Mathf.Floor(uvOffset.y));
        Vector3 windDirection = new Vector4(uniformWindDirection.x, 0, uniformWindDirection.z).normalized;
        Shader.SetGlobalTexture("_WindNoise", windNoise);
        Shader.SetGlobalVector("_UniformWindEffect", new Vector4(windDirection.x, windDirection.y, windDirection.z, uniformWindForce));
        Shader.SetGlobalVector("_worldRect", new Vector4(worldRect.x, worldRect.y, worldRect.width, worldRect.height));
        Shader.SetGlobalVector("_uvOffset", uvOffset);
        Shader.SetGlobalFloat("_Stability", stablility);
    }

    private void RenderFire()
    {
        // Graphics.DrawMeshInstanced(mesh, 0, material, matrices, meshCount, materialPropertyBlock,
            // ShadowCastingMode.On, true, 0, null, llpv ? LightProbeUsage.UseProxyVolume : LightProbeUsage.CustomProvided, llpv);
        List<Transform> fireList = new List<Transform>();

        for (int i = 0; i < fireRoot.childCount; ++i)
        {
            fireList.Add(fireRoot.GetChild(i));
        }

        Matrix4x4[] matrices = new Matrix4x4[fireList.Count];

        for (int i = 0; i < matrices.Length; ++i)
        {
            Transform fireAreaTransform = fireList[i];
            
            matrices[i] = Matrix4x4.TRS(fireAreaTransform.position,
                Quaternion.Euler(90, 0, 0),
                Vector3.one);
        }
        
        commandBuffer.BeginSample("Area");
        commandBuffer.GetTemporaryRT(areaPropertyID, 256, 256, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        commandBuffer.SetRenderTarget(areaPropertyID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        commandBuffer.DrawMeshInstanced(areaMesh, 0, fireAreaMaterial, 0, matrices);
        commandBuffer.EndSample("Area");
    }

    private void SetFirePosition()
    {
        // List<Transform> fireList = new List<Transform>();
        Vector4[] positions = new Vector4[fireRoot.childCount];

        for (int i = 0; i < fireRoot.childCount; ++i)
        {
            // fireList.Add(fireRoot.GetChild(i));
            positions[i] = fireRoot.GetChild(i).transform.position;
        }

        Shader.SetGlobalInt("_FireCount", fireRoot.childCount);
        Shader.SetGlobalVectorArray("_FireObjects", positions);
    }
    #endregion
}
