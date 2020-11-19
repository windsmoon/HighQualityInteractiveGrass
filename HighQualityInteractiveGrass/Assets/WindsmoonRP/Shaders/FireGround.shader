Shader "Unlit/FireGround"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        HLSLINCLUDE
        #include "WindsmoonCommon.hlsl"
        ENDHLSL
        
        Pass
        {
            HLSLPROGRAM
            struct Attribute
            {
                float3 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_Position;
	            float4 color : VAR_COLOR;
            };

            #pragma multi_compile_instancing
            #pragma vertex Vert
			#pragma fragment Frag

            Varyings Vert(Attribute input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = 1;
                return output;
            }
            
            float4 Frag(Varyings input) : SV_TARGET
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}
