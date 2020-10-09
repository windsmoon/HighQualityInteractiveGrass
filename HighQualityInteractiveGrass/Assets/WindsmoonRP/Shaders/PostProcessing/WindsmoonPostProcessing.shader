Shader "Windsmoon RP/Windsmoon Lit" 
{
	SubShader 
	{
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE
		#include "../WindsmoonCommon.hlsl"
		#include "WindsmoonPostProcessingPass.hlsl"
		ENDHLSL
				
		Pass 
		{
			Name "Copy"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment CopyFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Pre Filter"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomPrefilterPassFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Pre Filter Fade Fire Flies"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomPrefilterFadeFireFliesFragment
			ENDHLSL
		}

		Pass 
		{
			Name "Bloom Horizontal Blur"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomHorizontalBlurFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Vertical Blur"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomVerticalBlurFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Combine Additive"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomAdditiveFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Combine Scattering"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomScatteringFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Bloom Combine Scattering Final"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment BloomScatteringFinalFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Tone Mapping ACES"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment ToneMappingACESFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Tone Mapping Neutral"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment ToneMappingNeutralFragment
			ENDHLSL
		}
		
		Pass 
		{
			Name "Tone Mapping Reinhard"
			
			HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex PostProcessingVertex
			#pragma fragment ToneMappingReinhardFragment
			ENDHLSL
		}
	}
}