Shader "Windsmoon RP/Windsmoon Unlit"
{
    Properties
    {
        _BaseMap("Base Texture", 2D) = "white" {}
        [HDR] _BaseColor("Base Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Toggle(ALPHA_CLIPPING)] _alphaClipping ("Alpha Clipping", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
		[KeywordEnum(On, Clip, Dither, Off)] _Shadow_Mode("Shadow Mode", Float) = 0
		[HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
		[HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
    }
    
    SubShader
    {
        HLSLINCLUDE
        #include "WindsmoonCommon.hlsl"
        #include "WindsmoonUnlitInput.hlsl"
        ENDHLSL
    
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile _ ALPHA_CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment
            
            #include "WindsmoonUnlit.hlsl"
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
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterVertex
			#pragma fragment ShadowCasterFragment
			
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