#ifndef WINDSMOON_SURFACE_INCLUDED
#define WINDSMOON_SURFACE_INCLUDED

struct Surface
{
	float3 position;
	float depth;
    float3 normal;
	float3 interpolatedNormal;
    float3 viewDirection;
    float3 color;
    float alpha;
    float metallic;
	float occlusion;
	float smoothness;
	float fresnelStrength;
	float dither;
};

#endif