#ifndef WINDSMOON_UNLIT_INPUT_INCLUDED
#define WINDSMOON_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    float2 uv;
};

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    return baseUV * baseST.xy + baseST.zw;
}

InputConfig GetInputConfig(float2 uv)
{
    InputConfig config;
    config.uv = uv;
    return config;
}

float4 GetBaseColor(InputConfig config)
{
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.uv);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    return baseMap * color;
}

float3 GetEmission(InputConfig config) 
{
	return GetBaseColor(config).rgb;
}

float GetCutoff(InputConfig config)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(InputConfig config)
{
    return 0.0;
}

float GetSmoothness(InputConfig config)
{
    return 0.0;
}

float GetFresnel(InputConfig config)
{
    return 0.0;
}

#endif