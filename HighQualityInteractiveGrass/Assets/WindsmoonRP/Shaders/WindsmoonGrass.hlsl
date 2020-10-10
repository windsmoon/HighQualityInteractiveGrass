#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

#define MAX_INTERACTIVE_OBJECT_COUNT 16
// #define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(LightInfo)
    int _InteracitveObjectsCount;
    float4 _InteractiveObjects[MAX_INTERACTIVE_OBJECT_COUNT];
CBUFFER_END

void HandleInteractiveGrass(inout float3 posWS, float3 posOS, float4 factor)
{
    float random01 = Random01(posWS * 100); // posWS is near ??
    float maxOffsetScale = GetMaxGrassOffsetScale();
    float maxOffset = posOS.y * maxOffsetScale;
    
    // wind effect
    float4 windEffect = GetWindEffect();
    float windDirection = normalize(windEffect.xyz);
    float timeScale = 0.5f * sin(_Time.y * windEffect.w + posWS.x) + 0.5f;
    float3 offset = windDirection * min(windEffect.w, maxOffset) * factor.r * timeScale; // windEffect.w affect the max offset by wind

    // interactive objects effect
    for (int i = 0; i < _InteracitveObjectsCount; ++i)
    {
        float4 interactiveObject = _InteractiveObjects[i];
        float3 interactiveObjectPosWS = interactiveObject.xyz;
        float3 interactiveOffset = posWS - interactiveObjectPosWS;
        float squareInteractiveXZOffset = Square(interactiveOffset.x) + Square(interactiveOffset.z);
        float3 squareInteractiveOffsetScale = saturate(squareInteractiveXZOffset / (0.25f * 0.25f)); // todo ： add object area
        //
        // if ((interactiveOffset.x * interactiveOffset.x + interactiveOffset.z + interactiveOffset.z) > (0.25 * 0.25)) 
        // {
        // }
        //
        // else
        // {
        //     
        // }

        // offset = lerp(maxOffset, offset, )
        // todo : wind max offset and interactive max offset
        offset.x = lerp(interactiveOffset.x > 0 ? maxOffset : -maxOffset, offset.x, squareInteractiveOffsetScale.x);
        offset.z = lerp(interactiveOffset.z > 0 ? maxOffset : -maxOffset, offset.z, squareInteractiveOffsetScale.z);
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