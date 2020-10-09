#ifndef WINDSMOON_SHADOW_PASS_CASTER
#define WINDSMOON_SHADOW_PASS_CASTER

//#include "WindsmoonCommon.hlsl"

//TEXTURE2D(_BaseMap);
//SAMPLER(sampler_BaseMap);

//UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
//    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
//    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
//	UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
//UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

bool _ShadowPancaking;

Varyings ShadowCasterVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(positionWS);

    if (_ShadowPancaking) // ?? why only directional shadow use this
    {
    	#if UNITY_REVERSED_Z
    	output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);    
    	#else
    	output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);    
    	#endif
    }
    
    //float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    //output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

void ShadowCasterFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    ClipForLOD(input.positionCS.xy, unity_LODFade.x);
    //float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    //float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    //baseColor *= baseMap;
    InputConfig config = GetInputConfig(input.baseUV);
    float4 baseColor = GetBaseColor(config);
    
	#if defined(_SHADOW_MODE_CLIP)
		clip(baseColor.a - GetCutoff(config));
	#elif defined(_SHADOW_MODE_DITHER)
	    float dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	    clip(baseColor.a - dither);
	    // note : because the resulting pattern is noisy it suffers a lot more from temporal artifacts when the shadow matrix changes, which can make the shadows appear to tremble.
	#endif
}

#endif