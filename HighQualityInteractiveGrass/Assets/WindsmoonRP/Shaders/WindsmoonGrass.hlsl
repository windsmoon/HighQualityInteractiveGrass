#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

void HandleWindEffect(inout float3 pos, float4 factor)
{
    float4 windEffect = GetWindEffect();
    float timeScale = sin(_Time.y);
    float windDirection = normalize(windEffect.xyz);
    float3 offset = windDirection * windEffect.w * factor.r * timeScale;
    float squaredXZOffset = Square(offset.x) + Square(offset.z);
    float squareSlopLenght = squaredXZOffset + Square(pos.y);
    float scale = pos.y / sqrt(squareSlopLenght);
    offset.y = -(pos.y - pos.y * scale);
    pos += offset;
    
    // return windEffect * timeScale * factor;
    // return factor.r * windEffect.xyz * windEffect.w; 
}

void HandleInteractiveGrass(inout float3 pos, float4 factor)
{
    HandleWindEffect(pos, factor * saturate(Random(pos) + 0.5));
}

#endif