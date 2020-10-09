#ifndef WINDSMOON_META_PASS_INCLUDED
#define WINDSMOON_META_PASS_INCLUDED

#include "WindsmoonSurface.hlsl"
#include "WindsmoonShadow.hlsl"
#include "WindsmoonLight.hlsl"
#include "WindsmoonBRDF.hlsl"

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

struct Attributes
{
    float3 positionOS : POSITION; // we still need the object-space vertex attribute as input because shaders expect it to exist (from catlike)
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_IV;
};

Varyings MetaVertex(Attributes input)
{
    Varyings output;
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    // it seems that OpenGL doesn't work unless it explicitly uses the Z coordinate. 
    // We'll use the same dummy assignment that Unity's own meta pass uses (from catlike)
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
	output.positionCS = TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

// transparency and alpha test is determined by render queue, and have not to handled in meta fragment
// emission, transparency and alpha test can only be considered for material, per-instance properties are ignored
float4 MetaFragment(Varyings input) : SV_Target
{
    InputConfig config = GetInputConfig(input.baseUV);
    float4 baseColor = GetBaseColor(config);
    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = baseColor.rgb;
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    
    if (unity_MetaFragmentControl.x) // request diffuse
    {
        meta = float4(brdf.diffuse, 1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5; // highly specular but rough materials also pass along some indirect light (from catlike)
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue); // ??
    }
    
    else if (unity_MetaFragmentControl.y) // request emission
    {
        meta = float4(GetEmission(config), 1.0);
    }
    
    return meta;
}

#endif