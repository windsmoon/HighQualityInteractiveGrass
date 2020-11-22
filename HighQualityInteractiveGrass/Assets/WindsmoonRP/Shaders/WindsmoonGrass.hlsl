#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

#define MAX_INTERACTIVE_OBJECT_COUNT 16
#define MAX_Fire_OBJECT_COUNT 64
// #define MAX_OTHER_LIGHT_COUNT 64

// UNITY_INSTANCING_BUFFER_START(UnityPerMaterial1)
//     UNITY_DEFINE_INSTANCED_PROP(float4, _WindNoise_TexelSize)
// UNITY_INSTANCING_BUFFER_END(UnityPerMaterial1)

CBUFFER_START(GrassInfo)
    int _InteracitveObjectsCount;
    float4 _InteractiveObjects[MAX_INTERACTIVE_OBJECT_COUNT];
    int _FireCount;
    float4 _FireObjects[MAX_Fire_OBJECT_COUNT];
    float4 _UniformWindEffect;
    float4 _worldRect;
    float4 _uvOffset;
    float _Stability;
CBUFFER_END

float2 GetWindUV(float3 posWS)
{
    return float2((posWS.xz - _worldRect.xy) / _worldRect.zw);
}

void HandleInteractiveGrass(inout float3 posWS, float3 posOS, float4 factor, out float4 interactiveColor)
{
    float3 rootPosWS = TransformObjectToWorld(float3(0, 0, 0));

    // fires
    for (int i = 0; i < _FireCount; ++i)
    {
        float4 fireObjects = _FireObjects[i];
        float2 distance = rootPosWS.xz - fireObjects.xz;

        if (length(distance) < 1)
        {
            interactiveColor = float4(0.05f, 0, 0, 1);
            break;
        }

        else
        {
            interactiveColor = 1;
        }

        // interactiveColor = rootPosWS.xyzz;
    }
    
    float random01 = Random01(posWS.xz); // posWS is near ??
    float maxOffsetScale = GetMaxGrassOffsetScale();
    float originalLength = length(posWS - rootPosWS);
    float maxOffset = originalLength * maxOffsetScale; // todo : set grass height

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

    float noiseWindForce = windNoise.a * 2;
    // float windOffset = min(_UniformWindEffect.w * noiseWindForce, maxOffset) * factor.r * factor.r;
    float windOffsetLength = _UniformWindEffect.w * noiseWindForce * factor.r * factor.r;
    // float3 offset = windDirection * min(_UniformWindEffect.w * noiseWindForce, maxOffset) * factor.r * factor.r;
    float3 offset = windDirection * windOffsetLength;

    float maxInteracitveOffset = min(maxOffset * 2, 1);

    for (int i = 0; i < _InteracitveObjectsCount; ++i)
    {
        float4 interactiveObject = _InteractiveObjects[i];
        float3 interactiveObjectPosWS = interactiveObject.xyz;
        float3 interactiveOffsetDirection = rootPosWS - interactiveObjectPosWS + float3(0.00001, 0, 0.00001);
        interactiveOffsetDirection.y = 0;
        interactiveOffsetDirection = normalize(interactiveOffsetDirection);
        float3 interactiveOffset = posWS - interactiveObjectPosWS;
        // float3 interactiveDir = normalize(interactiveOffset);
        float squaredInteractiveXZLength = Square(interactiveOffset.x) + Square(interactiveOffset.z);
        float squaredInteractiveXZLengthScale = clamp(squaredInteractiveXZLength / 0.8, 0, 0.6); // todo ： add object area

        float interactiveOffsetLength = lerp(maxInteracitveOffset * 0.5, 0, squaredInteractiveXZLengthScale);
        // interactiveOffset = lerp(maxInteracitveOffset * interactiveOffsetDirection.xz, float2(0, 0), squaredInteractiveXZLengthScale);
        interactiveOffset = interactiveOffsetDirection * interactiveOffsetLength;
        offset += interactiveOffset;
    }

    offset = clamp(offset, -maxOffset.xxx, maxOffset.xxx);


    
    // caculate y offset
    // todo : allow smoe strech
    float stretchScale = 1 + GetStretchScale();
    float3 newPosWS = posWS + offset;
    float3 newVertexVector = newPosWS - rootPosWS;
    float newLength = length(newVertexVector);
    float lengthScale = newLength / originalLength;
    lengthScale = min(stretchScale, lengthScale);
    //lengthScale = 1 / lengthScale;
    
    // float lengthScale = stretchScale * originalLenght / newLength;
    posWS = rootPosWS + lengthScale * newVertexVector * originalLength / newLength;
    // posWS = newPosWS;

    // float squaredXZOffset = Square(offset.x) + Square(offset.z);
    // squaredXZOffset *= random01;
    // float squareSlopeLenght = squaredXZOffset + Square(posOS.y);
    // float scale = posOS.y / (sqrt(squareSlopeLenght) + 0.00001f);
    // offset.y = -(posOS.y - posOS.y * scale);
    // posWS += offset;
}

#endif