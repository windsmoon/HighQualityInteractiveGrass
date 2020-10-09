#ifndef WINDSMOON_GRASS_INCLUDED
#define WINDSMOON_GRASS_INCLUDED

float3 GetWindEffect(float4 factor, float4 windEffect)
{
    return factor.r * windEffect.xyz * windEffect.w; 
}

float3 HandleInteractiveGrass(float3 pos, float factor)
{
    // pos
}

#endif