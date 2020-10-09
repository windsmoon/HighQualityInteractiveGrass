#ifndef WINDSMOON_UNLIT_INCLUDED
#define WINDSMOON_UNLIT_INCLUDED

//CBUFFER_START(UnityPerMaterial)
  //  float4 _BaseColor;
//CBUFFER_END

//TEXTURE2D(_BaseMap);
//SAMPLER(sampler_BaseMap);

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
//	UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attribute
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_Position;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings UnlitVertex(Attribute input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 worldPos = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(worldPos);
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

float4 UnlitFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
	InputConfig config = GetInputConfig(input.baseUV);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, config.uv);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 color = baseMap * baseColor;
	
	#if defined(ALPHA_CLIPPING)
	    clip(color.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#endif
	
	return color;
}
#endif