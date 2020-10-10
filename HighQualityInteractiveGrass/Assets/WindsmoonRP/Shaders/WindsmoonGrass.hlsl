#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

void HandleWindEffect(inout float3 posWS, float3 posOS, float4 factor)
{
    float random = Random(posWS);
    float4 windEffect = GetWindEffect();
    float timeScale = sin(_Time.y * GetWindSpeed() + posWS.x);
    float windDirection = normalize(windEffect.xyz);
    float3 offset = windDirection * windEffect.w * factor.r * timeScale;
    float squaredXZOffset = Square(offset.x) + Square(offset.z);
    squaredXZOffset *= random;
    float squareSlopeLenght = squaredXZOffset + Square(posOS.y);
    float scale = posOS.y / (sqrt(squareSlopeLenght) + 0.00001f);
    offset.y = -(posOS.y - posOS.y * scale);
    posWS += offset;
    
    // return windEffect * timeScale * factor;
    // return factor.r * windEffect.xyz * windEffect.w; 
}

void HandleInteractiveGrass(inout float3 posWS, float3 posOS, float4 factor)
{
    HandleWindEffect(posWS, posOS, factor);
}

#endif