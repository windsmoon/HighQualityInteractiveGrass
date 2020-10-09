#ifndef WINDSMOON_BRDF_INCLUDED
#define WINDSMOON_BRDF_INCLUDED

#define MIN_REFLECTIVITY 0.04

struct BRDF
{
	float3 diffuse;
	float3 specular;
	float roughness;
	float perceptualRoughness;
	float fresnel;
};

float OneMinusReflectivity(float metallic) 
{
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

BRDF GetBRDF(Surface surface) 
{
	BRDF brdf;
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
	brdf.diffuse = surface.color * oneMinusReflectivity;
	
	#if defined(PREMULTIPLY_ALPHA)
		brdf.diffuse *= surface.alpha;
	#endif
	
	//brdfLight.specular = 0.0;
	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	// float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness); // disne
	brdf.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness); // disne
	brdf.roughness = PerceptualRoughnessToRoughness(brdf.perceptualRoughness);	
	//brdfLight.roughness = 1.0;

	// We use a variant Schlick's approximation for Fresnel.
	// It replaces the specular BRDF color with solid white in the ideal case, but roughness can prevent reflections from showing up.
	// We arrive at the final color by adding the surface smoothness and reflectivity together, with a maximum of 1.
	// (from catlike)
	brdf.fresnel = saturate(surface.smoothness + 1.0 - oneMinusReflectivity);
	return brdf;
}

float GetSpecularStrength(Surface surface, BRDF brdf, Light light) 
{
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 GetIndirectBRDF(Surface surface, BRDF brdf, float3 diffuse, float3 specular)
{
	float fresnelStrength = surface.fresnelStrength * Pow4(1.0 - saturate(dot(surface.normal, surface.viewDirection)));
	// float3 reflection = specular * brdf.specular;
	float3 reflection = specular * lerp(brdf.specular, brdf.fresnel, fresnelStrength);
	reflection /= brdf.roughness * brdf.roughness + 1.0; // when roughness is 0, nothing happend, but if the roughness is 1, the reflection will be halved
	return (diffuse * brdf.diffuse + reflection) * surface.occlusion; // ambient occlusion only affect the indirect light
}

float3 GetDirectBRDF(Surface surface, BRDF brdf, Light light) 
{
	return GetSpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

#endif