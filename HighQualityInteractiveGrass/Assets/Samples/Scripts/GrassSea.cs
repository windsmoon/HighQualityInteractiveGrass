using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassSea : MonoBehaviour
{
    #region fields
    [SerializeField]
    private Texture2D windNoise;
    [SerializeField]
    private Vector3 uniformWindDirection;
    [SerializeField] 
    private float uniformWindStrength;
    [SerializeField]    
    private Rect worldRect;
    #endregion

    #region unity methods
    private void Update()
    {
        Shader.SetGlobalTexture("_WindNoise", windNoise);
        Shader.SetGlobalVector("_UniformWindEffect", new Vector4(uniformWindDirection.x, uniformWindDirection.y, uniformWindDirection.z, uniformWindStrength));
        Shader.SetGlobalVector("_worldRect", new Vector4(worldRect.x, worldRect.y, worldRect.width, worldRect.height));
    }
    #endregion
}
