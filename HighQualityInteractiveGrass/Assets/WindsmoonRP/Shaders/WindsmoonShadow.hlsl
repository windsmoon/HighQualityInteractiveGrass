#ifndef WINDSMOON_SHADOW_INCLUDED
#define WINDSMOON_SHADOW_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#if defined(DIRECTIONAL_PCF3X3)
	#define DIRECTIONAL_FILTER_SAMPLES 4
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(DIRECTIONAL_PCF5X5)
	#define DIRECTIONAL_FILTER_SAMPLES 9
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(DIRECTIONAL_PCF7X7)
	#define DIRECTIONAL_FILTER_SAMPLES 16
	#define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(OTHER_PCF3X3)
    #define OTHER_FILTER_SAMPLES 4
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(OTHER_PCF5X5)
    #define OTHER_FILTER_SAMPLES 9
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(OTHER_PCF7X7)
    #define OTHER_FILTER_SAMPLES 16
    #define OTHER_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_DIRECTIONAL_SHADOW_COUNT 4
#define MAX_OTHER_SHADOW_LIGHT_COUNT 16
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowMap); // ?? what does TEXTURE2D_SHADOW mean ?
TEXTURE2D_SHADOW(_OtherShadowMap);
#define SHADOW_SAMPLER sampler_linear_clamp_compare // ??
SAMPLER_CMP(SHADOW_SAMPLER); // ?? note : use a special SAMPLER_CMP macro to define the sampler state, as this does define a different way to sample shadow maps, because regular bilinear filtering doesn't make sense for depth data.

struct ShadowMask 
{
	bool useAlwaysShadowMask;
	bool useDistanceShadowMask;
	float4 shadows;
};

CBUFFER_START(ShadowProperty)
    int _CascadeCount;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_DIRECTIONAL_SHADOW_COUNT * MAX_CASCADE_COUNT];
	float4x4 _OtherShadowMatrices[MAX_OTHER_SHADOW_LIGHT_COUNT];
	float4 _OtherShadowTiles[MAX_OTHER_SHADOW_LIGHT_COUNT];
    //float _MaxShadowDistance;
    float4 _ShadowDistanceFade; // x means 1/maxShadowDistance, y means 1/distanceFade
    float4 _CascadeInfos[MAX_CASCADE_COUNT]; // x : 1 / (radius of cullingSphere) ^ 2
    float4 _ShadowMapSize;
CBUFFER_END 

struct DirectionalShadowData // the info of the direcctional light
{
    float shadowStrength; // if surface is not in any culling sphere, global shadowStrength set to 0 to avoid any shadow 
    int tileIndex;
    float normalBias;
	int shadowMaskChannel;
};

struct OtherShadowData
{
	float strength;
	int tileIndex;
	bool isPoint;
	int shadowMaskChannel;
	float3 lightPositionWS;
	float3 lightDirectionWS;
	float3 spotDirectionWS;
};

struct ShadowData // the info of the fragment
{
    int cascadeIndex;
    float cascadeBlend;
    float strength;
    ShadowMask shadowMask;
};

// fadeScale is from 0 to 1 but not equal 0
// scale means 1 / maxDistancce
// fadeScale control the begin point of fade
float GetFadedShadowStrength(float depth, float scale, float fadeScale) 
{
    // (1 - depth / maxDistance) / fadeScale
    // (1 - depth / maxDistance) means from 0 to 1, the shadow strength from 1 to 0 linearly
    // divided by fadeScale and saturate the resulit means the fade begin at the point (1 - fadeScale) in the line form 0 to maxDisatance
    return saturate((1.0 - depth * scale) * fadeScale);
}

// todo : add cascade keyword
ShadowData GetShadowData(Surface surfaceWS)
{
    ShadowData shadowData;
	shadowData.shadowMask.useAlwaysShadowMask = false;
    shadowData.shadowMask.useDistanceShadowMask = false;
	shadowData.shadowMask.shadows = 1.0;
    
    // ?? : is this fade meaningful ?
    
    // the outermost culling sphere doesn't end exactly at the max shadow distance but extends a bit beyond it
    
    //if (surfaceWS.depth >= _MaxShadowDistance)
    //{
    //    shadowInfo.cascadeIndex = 0;
    //    shadowInfo.strength = 0.0f;
    //    return shadowInfo;
    //}
    
    shadowData.cascadeBlend = 1.0;
    shadowData.strength = GetFadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    
    for (int i = 0; i < _CascadeCount; ++i)
    {
        float4 cullingSphere = _CascadeCullingSpheres[i];
        float squaredDistance = GetDistanceSquared(cullingSphere.xyz, surfaceWS.position);
        
        if (squaredDistance < cullingSphere.w)
        {
            // todo : I think it is useless because there has already have distance fade 
            float fade = GetFadedShadowStrength(squaredDistance, _CascadeInfos[i].x, _ShadowDistanceFade.z);
            
            if (i == _CascadeCount - 1)
            {
                // shadowData.strength *= GetFadedShadowStrength(squaredDistance, _CascadeInfos[i].x, _ShadowDistanceFade.z);
                shadowData.strength *= fade;
            }
            
            else
            {
                shadowData.cascadeBlend = fade;
            }
            
            break;
        }
    }
    
    if (i == _CascadeCount && _CascadeCount > 0)
    {
        shadowData.strength = 0.0;
    }
    
    #if defined(CASCADE_BLEND_DITHER)
		else if (shadowData.cascadeBlend < surfaceWS.dither) 
		{
			i += 1;
		}
	#endif
	
	#if !defined(CASCADE_BLEND_SOFT)
		shadowData.cascadeBlend = 1.0;
	#endif
    
    shadowData.cascadeIndex = i;
    return shadowData;
}

float SampleDirectionalShadow(float3 positionShadowMap)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowMap, SHADOW_SAMPLER, positionShadowMap); // ?? why directly use shadow map value than cmpare their depth
}

float SampleOtherShadow(float3 positionShadowMap, float3 bound)
{
	// we need clamp the bound, otherwise there may have incorrect shadow when the bias or pcf and so on are used
	positionShadowMap.xy = clamp(positionShadowMap.xy, bound.xy, bound.xy + bound.z); // z means 1 / split count, it is the tile length in 01
	return SAMPLE_TEXTURE2D_SHADOW(_OtherShadowMap, SHADOW_SAMPLER, positionShadowMap);
}

float FilterDirectionalShadow(float3 positionSTS)
{
	#if defined(DIRECTIONAL_FILTER_SETUP)
		float weights[DIRECTIONAL_FILTER_SAMPLES];
		float2 positions[DIRECTIONAL_FILTER_SAMPLES];
		float4 size = _ShadowMapSize.yyxx;
		DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
		float shadow = 0;
		
		for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) 
		{
			shadow += weights[i] * SampleDirectionalShadow(float3(positions[i].xy, positionSTS.z));
		}
		
		return shadow;
	#else
		return SampleDirectionalShadow(positionSTS);
	#endif
}

float FilterOtherShadow(float3 positionShadowMap, float3 bound)
{
	#if defined(OTHER_FILTER_SETUP)
		float weights[OTHER_FILTER_SAMPLES];
		float2 positions[OTHER_FILTER_SAMPLES];
		float4 size = _ShadowMapSize.wwzz;
		OTHER_FILTER_SETUP(size, positionShadowMap.xy, weights, positions);
		float shadow = 0;

		for (int i = 0; i < OTHER_FILTER_SAMPLES; ++i)
		{
			shadow += weights[i] * SampleOtherShadow(float3(positions[i].xy, positionShadowMap.z), bound);
		}

		return shadow;
	#else
		return SampleOtherShadow(positionShadowMap, bound);
	#endif
}

float GetCascadedShadow(DirectionalShadowData directionalShadowData, ShadowData globalShadowData, Surface surfaceWS)
{
	float3 normalBias = surfaceWS.interpolatedNormal  * directionalShadowData.normalBias * _CascadeInfos[globalShadowData.cascadeIndex].y;
	float3 positionShadowMap = mul(_DirectionalShadowMatrices[directionalShadowData.tileIndex], float4(surfaceWS.position + normalBias, 1.0f));
	float shadow = FilterDirectionalShadow(positionShadowMap);
        
	if (globalShadowData.cascadeBlend < 1.0) // ??
	{
		normalBias = surfaceWS.interpolatedNormal  * (directionalShadowData.normalBias * _CascadeInfos[globalShadowData.cascadeIndex + 1].y);
		positionShadowMap = mul(_DirectionalShadowMatrices[directionalShadowData.tileIndex + 1], float4(surfaceWS.position + normalBias, 1.0f));
		shadow = lerp(FilterDirectionalShadow(positionShadowMap), shadow, globalShadowData.cascadeBlend);
	}

	return shadow;
}

float GetBakedShadow(ShadowMask shadowMask, int maskChannel)
{
	float shadow = 1.0;
	
	if (shadowMask.useDistanceShadowMask || shadowMask.useAlwaysShadowMask)
	{
		if (maskChannel >= 0)
		{
			shadow = shadowMask.shadows[maskChannel];
		}
	}
	
	return shadow;
}

float GetBakedShadow(ShadowMask shadowMask, int maskChannel, float lightShadowStrength)
{
	if (shadowMask.useDistanceShadowMask || shadowMask.useAlwaysShadowMask)
	{
		return lerp(1.0, GetBakedShadow(shadowMask, maskChannel), lightShadowStrength); // ?? the baked shadow is not consider the shadow strength of the light
	}
	
	return 1.0;
}

float MixBakedAndRealtimeShadows(ShadowData globalShadowData, float shadow, int maskChannel, float lightStrength)
{
	float bakedShadow = GetBakedShadow(globalShadowData.shadowMask, maskChannel);
	//bakedShadow = 1; // debug

	if (globalShadowData.shadowMask.useAlwaysShadowMask)
	{
		shadow = lerp(1.0, shadow, globalShadowData.strength);
		shadow = min(bakedShadow, shadow);
		return lerp(1.0, shadow, lightStrength);
	}
	
	if (globalShadowData.shadowMask.useDistanceShadowMask)
	{
		shadow = lerp(bakedShadow, shadow, globalShadowData.strength);
		return lerp(1.0, shadow, lightStrength);
	}

	return lerp(1.0, shadow, lightStrength * globalShadowData.strength);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directionalShadowData, ShadowData globalShadowData, Surface surfaceWS)
{
    #if !defined(RECEIVE_SHADOWS)
        return 1.0f;
    #else
        if (directionalShadowData.shadowStrength * globalShadowData.strength <= 0.0f) // todo : when strength is less then zero, this light should be discard in c# part 
        {
        	// if there has no realt time shadow, then use the baked shadow
        	return GetBakedShadow(globalShadowData.shadowMask, directionalShadowData.shadowMaskChannel, abs(directionalShadowData.shadowStrength));
	    }
	    
	    float shadow = GetCascadedShadow(directionalShadowData, globalShadowData, surfaceWS);
		return MixBakedAndRealtimeShadows(globalShadowData, shadow, directionalShadowData.shadowMaskChannel, directionalShadowData.shadowStrength);
	#endif
}

static const float3 pointShadowPlanes[6] =
{
	float3(-1.0, 0.0, 0.0),
    float3(1.0, 0.0, 0.0),
    float3(0.0, -1.0, 0.0),
    float3(0.0, 1.0, 0.0),
    float3(0.0, 0.0, -1.0),
    float3(0.0, 0.0, 1.0)
};

float GetOtherShadow(OtherShadowData otherShadowData, ShadowData globalShadowData, Surface surfaceWS)
{
	float tileIndex = otherShadowData.tileIndex;
	float3 lightPlane = otherShadowData.spotDirectionWS;

	if (otherShadowData.isPoint)
	{	
    	float faceOffset = CubeMapFaceID(-otherShadowData.lightDirectionWS); // todo : see the impl
    	tileIndex += faceOffset;
		lightPlane = pointShadowPlanes[faceOffset];
    }
	
	float4 otherShadowTile = _OtherShadowTiles[tileIndex];
	float3 surfaceToLight = otherShadowData.lightPositionWS - surfaceWS.position;
	float distanceToLightPlane = dot(surfaceToLight, lightPlane); // ?? caculate spot shadow bias, dot(surfaceToLight, otherShadowData.spotDirectionWS) is the length of the projection of light-surface distance to spot direction
	float3 normalBias = surfaceWS.interpolatedNormal * (distanceToLightPlane * otherShadowTile.w);
	float4 position = mul(_OtherShadowMatrices[tileIndex], float4(surfaceWS.position + normalBias, 1.0));
	return FilterOtherShadow(position.xyz / position.w, otherShadowTile.xyz); // ?? shadow map coord
}

float GetOtherShadowAttenuation(OtherShadowData otherShadowData, ShadowData globaleShadowData, Surface surfaceWS)
{
	#if !defined(RECEIVE_SHADOWS)
		return 1.0;
	#endif

	float shadow;

	if (otherShadowData.strength * globaleShadowData.strength <= 0.0)
	{
		// if there has no realt time shadow, then use the baked shadow
		shadow = GetBakedShadow(globaleShadowData.shadowMask, otherShadowData.shadowMaskChannel, abs(otherShadowData.strength));
	}

	else
	{
		shadow = GetOtherShadow(otherShadowData, globaleShadowData, surfaceWS);
		shadow = MixBakedAndRealtimeShadows(globaleShadowData, shadow, otherShadowData.shadowMaskChannel, otherShadowData.strength);
	}

	return shadow;
}
#endif