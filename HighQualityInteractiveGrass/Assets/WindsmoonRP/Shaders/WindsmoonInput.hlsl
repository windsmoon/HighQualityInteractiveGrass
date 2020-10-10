﻿#ifndef WINDSMOON_INPUT_INCLUDED
#define WINDSMOON_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;

	// todo : support more lights
	real4 unity_LightData; // y is the light count per object ?? why must be defined directly after unity_WorldTransformParams. 
	real4 unity_LightIndices[2]; // 2 x 4 up to 8 lights per object are supported ?? why must be defined directly after unity_WorldTransformParams.
    
    float4 unity_ProbesOcclusion;

	float4 unity_SpecCube0_HDR;

    float4 unity_LightmapST;
	float4 unity_DynamicLightmapST;
	
	float4 unity_SHAr;
	float4 unity_SHAg;
	float4 unity_SHAb;
	float4 unity_SHBr;
	float4 unity_SHBg;
	float4 unity_SHBb;
	float4 unity_SHC;
	
	float4 unity_ProbeVolumeParams;
	float4x4 unity_ProbeVolumeWorldToObject;
	float4 unity_ProbeVolumeSizeInv;
	float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
float3 _WorldSpaceCameraPos;
float4 _Time;

#endif