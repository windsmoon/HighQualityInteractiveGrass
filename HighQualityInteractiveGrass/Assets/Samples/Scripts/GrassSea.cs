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
    private Vector2 uvOffset = new Vector2(0, 0);
    private Transform fireRoot;
    private CommandBuffer commandBuffer;
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
        commandBuffer.BeginSample("Area");
        commandBuffer.EndSample("Area");
    }
    #endregion
}
