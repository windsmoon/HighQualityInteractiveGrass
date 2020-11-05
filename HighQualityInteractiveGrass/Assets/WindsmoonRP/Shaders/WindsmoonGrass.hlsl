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
    float _Stability;
CBUFFER_END

float2 GetWindUV(float3 posWS)
{
    return float2((posWS.xz - _worldRect.xy) / _worldRect.zw);
}

void HandleInteractiveGrass(inout float3 posWS, float3 posOS, float4 factor)
{
    float random01 = Random01(posWS.xz); // posWS is near ??
    float maxOffsetScale = GetMaxGrassOffsetScale();
    float3 rootPosWS = TransformObjectToWorld(float3(0, 0, 0));
    float originalLenght = length(posWS - rootPosWS);

    float maxOffset = originalLenght * maxOffsetScale; // todo : set grass height

    // caculate nosie uv and sample the noise
    float2 windUV = GetWindUV(posWS);
    windUV = windUV + _uvOffset;
    float4 windNoise = SAMPLE_TEXTURE2D_LOD(_WindNoise, sampler_WindNoise, windUV, 0);

    // rotate wind direction by nosie
    // todo : the sin and cos can be removed by some other method
    float radNoiseRotate = (windNoise.r * 2 - 1) * 3.1415926 / 4;
    radNoiseRotate = lerp(radNoiseRotate, 0, _Stability);
    float sinNoise = sin(radNoiseRotate);
    float cosNoise = cos(radNoiseRotate);
    float2x2 nosieMatrix = {cosNoise, -sinNoise, sinNoise, cosNoise};
    float3 windDirection;
    windDirection.y = 0;
    windDirection.xz = mul(nosieMatrix, _UniformWindEffect.xz);
    // float3 resultWindirection = float3(windDirection.x, 0, windDirection.y);

    float3 offset = windDirection * min(_UniformWindEffect.w * windNoise.a, maxOffset) * factor.r * factor.r;

    float maxInteracitveOffset = min(maxOffset * 2, 1);

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
    // todo : allow smoe strech
    float3 newPosWS = posWS + offset;
    float3 newVertexVector = newPosWS - rootPosWS;
    float newLength = length(newVertexVector);
    float lengthScale = originalLenght / newLength;
    posWS = rootPosWS + newVertexVector * lengthScale;
    // posWS = newPosWS;

    // float squaredXZOffset = Square(offset.x) + Square(offset.z);
    // squaredXZOffset *= random01;
    // float squareSlopeLenght = squaredXZOffset + Square(posOS.y);
    // float scale = posOS.y / (sqrt(squareSlopeLenght) + 0.00001f);
    // offset.y = -(posOS.y - posOS.y * scale);
    // posWS += offset;
}

#endif