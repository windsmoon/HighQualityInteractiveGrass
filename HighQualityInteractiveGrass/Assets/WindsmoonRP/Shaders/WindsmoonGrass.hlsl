#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

void HandleWindEffect(inout float3 posWS, float3 posOS, float4 factor)
{
    float random01 = Random01(posWS);
    float4 windEffect = GetWindEffect();
    float timeScale = 0.5f * sin(_Time.y * windEffect.w + posWS.x) + 0.5f;
    // float timeScale = sin(_Time.y * windEffect.w + posWS.x) ;

    float windDirection = normalize(windEffect.xyz);
    float3 offset = windDirection * min(windEffect.w, posOS.y * GetMaxWindEffect()) * factor.r * timeScale; // windEffect.w affect the max offset by wind
    float squaredXZOffset = Square(offset.x) + Square(offset.z);
    squaredXZOffset *= random01;
    // squaredXZOffset = min(squaredXZOffset, 0.01 * posOS.y); // max offset xz is 0.5 grass height
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