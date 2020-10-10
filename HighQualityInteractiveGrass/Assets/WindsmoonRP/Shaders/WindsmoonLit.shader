Shader "Windsmoon RP/Windsmoon Lit"
{
    Properties
    {
        _BaseMap("Base Texture", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(ALPHA_CLIPPING)] _AlphaClipping ("Alpha Clipping", Float) = 0
        [Toggle(PREMULTIPLY_ALPHA)] _PremultiplyAlpha ("Premultiply Alpha", Float) = 0
		[Toggle(MASK_MAP)] _MaskMapToggle("Use Mask Map", Float) = 0
		[NoScaleOffset] _MaskMap("Mask Map (MODS)", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 0
		_Occlusion("Occlusion", Range(0, 1)) = 0
		_Smoothness("Smoothness", Range(0, 1)) = 0.5
		_Fresnel("Fresnel", Range(0, 1)) = 1
		[Toggle(NORMAL_MAP)] _NormalMapToggle("Use Normal Map", Float) = 0
		[NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
		_NormalScale("Normal Scale", Range(0, 1)) = 1
		[NoScaleOffset] _EmissionMap("Emmision Map", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 0.0)
		[Toggle(DETAIL_MAP)] _DetailMapToggle("Use Detail Map", Float) = 0
		_DetailMap("Detail Map", 2D) = "linearGrey" {}
		[NoScaleOffset] _DetailNormalMap("Detail Normal Map", 2D) = "bump" {}
		_DetailAlbedo("Detail Albedo", Range(0, 1)) = 1
		_DetailSmoothness("Detail Smoothness", Range(0, 1)) = 1
		_DetailNormalScale("Detail Normal Scale", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
		[KeywordEnum(On, Clip, Dither, Off)] _Shadow_Mode("Shadow Mode", Float) = 0
		[Toggle(RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
    	[Toggle(GRASS)] _Grass ("Grass", Float) = 0
    	_WindEffect("Wind Effect (RGB Direction A Strength)", Vector) = (1, 0, 0, 1)
    	_MaxWindEffect("Max Wind Effect (decide the max offset)", Float) = 0.3
		[HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    
    SubShader
    {
        HLSLINCLUDE
        #include "WindsmoonCommon.hlsl"
        #include "WindsmoonLitInput.hlsl"
        ENDHLSL
        
        Pass
        {
            Tags
            {
                "LightMode" = "WindsmoonLit"
            }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
//        	Cull Off
            
            HLSLPROGRAM
            #pragma target 3.5 // for loops which are use a variable length
            #pragma multi_compile _ ALPHA_CLIPPING
            #pragma multi_compile _ PREMULTIPLY_ALPHA
            #pragma multi_compile _ DIRECTIONAL_PCF3X3 DIRECTIONAL_PCF5X5 DIRECTIONAL_PCF7X7
            #pragma multi_compile _ OTHER_PCF3X3 OTHER_PCF5X5 OTHER_PCF7X7
            #pragma multi_compile _ CASCADE_BLEND_SOFT CASCADE_BLEND_DITHER
            #pragma multi_compile _ SHADOW_MASK_ALWAYS SHADOW_MASK_DISTANCE
            #pragma multi_compile _ RECEIVE_SHADOWS
            #pragma multi_compile _ GRASS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ NORMAL_MAP
            #pragma multi_compile _ MASK_MAP
            #pragma multi_compile _ DETAIL_MAP
            #pragma multi_compile _ LIGHTS_PER_OBJECT
            #pragma multi_compile_instancing
            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include "WindsmoonGrass.hlsl"
            #include "WindsmoonLitPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 3.5 // for loops which are use a variable length
//            #pragma multi_compile _ ALPHA_CLIPPING
            #pragma multi_compile _ _SHADOW_MODE_CLIP _SHADOW_MODE_DITHER
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ GRASS

            #pragma multi_compile_instancing
			#pragma vertex ShadowCasterVertex
			#pragma fragment ShadowCasterFragment

            #include "WindsmoonGrass.hlsl"
			#include "WindsmoonShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags
            {
                "LightMode" = "Meta"
            }
            
            Cull Off
            
            HLSLPROGRAM
			#pragma target 3.5
			#pragma vertex MetaVertex
			#pragma fragment MetaFragment
			
			#include "WindsmoonMetaPass.hlsl"
			ENDHLSL
        }
    }
    
    CustomEditor "WindsmoonRP.Editor.WindsmoonShaderGUI"
}