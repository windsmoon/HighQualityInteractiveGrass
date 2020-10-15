#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

#define MAX_INTERACTIVE_OBJECT_COUNT 16
// #define MAX_OTHER_LIGHT_COUNT 64

// UNITY_INSTANCING_BUFFER_START(UnityPerMaterial1)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _WindNoise_TexelSize)
// UNITY_INSTANCING_BUFFER_END(UnityPerMaterial1)

CBUFFER_START(GrassInfo)
    int _InteracitveObjectsCount;
    float4 _InteractiveObjects[MAX_INTERACTIVE_OBJECT_COUNT];
    float4 _UniformWindEffect;
    float4 _worldRect;
    float4 _uvOffset;
CBUFFER_END

// float4 GetDisturbedWind(float2 uv)
// {
//     float3 windDir = _UniformWindEffect.xyz;
//     windDir *= windNoise.rgb;
//     windDir.y = 0;
//     windDir = normalize(windDir);
//     float windStrength = _UniformWindEffect.w;
//     windStrength *= windNoise.a * 2;
//     return float4(windDir, windStrength);
// }

float2 GetWindUV(float3 posWS)
{
    return float2((posWS.xz - _worldRect.xy) / _worldRect.zw);
}

void HandleInteractiveGrass(inout float3 posWS, float3 posOS, float4 factor)
{
    float random01 = Random01(posWS); // posWS is near ??
    float maxOffsetScale = GetMaxGrassOffsetScale();
    float maxOffset = posOS.y * maxOffsetScale;
    float maxInteracitveOffset = min(maxOffset * 2, 1);
    
    // wind effect
    // float4 windEffect = GetWindEffect();
    // float windDirection = normalize(windEffect.xyz);
    // float timeScale = 0.5f * sin(_Time.y * windEffect.w + posWS.x) + 0.5f;
    // float3 offset = windDirection * min(windEffect.w, maxOffset) * factor.r * timeScale; // windEffect.w affect the max offset by wind
    float2 windUV = GetWindUV(posWS);
    
    // windUV = windUV + frac(_WindNoise_TexelSize.xy * 3000 * (1.0f / 60.0f) * _Time.y);
    // windUV = windUV + frac(_WindNoise_TexelSize.xy * 3000 * (1.0f / 60.0f) * _Time.y);
    // windUV = windUV + frac(0.1 * _Time.y);
    windUV += _uvOffset;
    float4 windNoise = SAMPLE_TEXTURE2D_LOD(_WindNoise, sampler_WindNoise, windUV, 0);
    float3 offset = normalize(saturate(windNoise.rgb + 0.5) * _UniformWindEffect.xyz) * min(_UniformWindEffect.w * windNoise.a * 2, maxOffset) * factor.r;
    
    // float4 windEffect = GetDisturbedWind(windUV);

    // float4 windEffect = GetDisturbedWind( windUV + frac(0.1 * _Time.y));

    // float3 windDirection = windEffect.xyz;
    // windDirection.y = 0; // to do
    // float timeScale = 0.5f * sin(_Time.y * windEffect.w + posWS.x) + 0.5f;
    // float3 offset = windDirection * min(windEffect.w, maxOffset) * factor.r; // windEffect.w affect the max offset by wind

    
    // interactive objects effect
    for (int i = 0; i < _InteracitveObjectsCount; ++i)
    {
        float4 interactiveObject = _InteractiveObjects[i];
        float3 interactiveObjectPosWS = interactiveObject.xyz;
        float3 interactiveOffset = posWS - interactiveObjectPosWS;
        float3 interactiveDir = normalize(interactiveOffset);
        float squaredInteractiveXZLength = Square(interactiveOffset.x) + Square(interactiveOffset.z);
        float squaredInteractiveXZLengthScale = clamp(squaredInteractiveXZLength / 1, 0, 1); // todo ： add object area
    
        offset.xz += lerp(maxInteracitveOffset * interactiveDir.xz, float2(0, 0), squaredInteractiveXZLengthScale);
    }
    
    // caculate y offset
    float squaredXZOffset = Square(offset.x) + Square(offset.z);
    squaredXZOffset *= random01;
    float squareSlopeLenght = squaredXZOffset + Square(posOS.y);
    float scale = posOS.y / (sqrt(squareSlopeLenght) + 0.00001f);
    offset.y = -(posOS.y - posOS.y * scale);
    posWS += offset;
}

#endif