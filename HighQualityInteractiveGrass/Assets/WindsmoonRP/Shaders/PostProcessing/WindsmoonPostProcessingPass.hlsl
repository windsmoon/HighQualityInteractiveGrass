#ifndef WINDSMOON_POST_PROCESSING_INCLUDED
#define WINDSMOON_POST_PROCESSING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

TEXTURE2D(_PostProcessingSource);
TEXTURE2D(_PostProcessingSource2);
SAMPLER(sampler_linear_clamp);
float4 _PostProcessingSource_TexelSize;
float _BloomIntensity;

float4 _ProjectionParams; // x : if x is less than 0, the v is from top to bottom
bool _BloomBicubicUpsampling;
float4 _BloomThreshold;

struct Varyings
{
    float4 positionCS: SV_POSITION;
    float2 uv : VAR_UV;
};

Varyings PostProcessingVertex(uint vertexID : SV_VertexID) // the order is (-1, -1) (-1. 3) (3, -1)
{
    Varyings output;
    output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0: -1.0, 0.0, 1.0);
    output.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);

    if (_ProjectionParams.x < 0.0)
    {
        output.uv.y = 1.0 - output.uv.y;
    }
    
    return output;
}

float4 GetSourceTexelSize()
{
    return _PostProcessingSource_TexelSize;
}

float4 GetSource(float2 uv)
{
    return SAMPLE_TEXTURE2D(_PostProcessingSource, sampler_linear_clamp, uv);
}

float4 GetSource2(float2 uv)
{
    return SAMPLE_TEXTURE2D(_PostProcessingSource2, sampler_linear_clamp, uv);
}

float4 GetSourceBicubic(float2 uv)
{
    return SampleTexture2DBicubic(TEXTURE2D_ARGS(_PostProcessingSource, sampler_linear_clamp), uv, _PostProcessingSource_TexelSize.zwxy, 1.0, 0.0);
}


float3 ApplyBloomThreshold(float3 color)
{
    float brightness = Max3(color.r, color.g, color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

float4 CopyFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.uv);
}

float4 BloomPrefilterPassFragment(Varyings input) : SV_TARGET
{
    float3 color = ApplyBloomThreshold(GetSource(input.uv).rgb);
    return float4(color, 1.0);
}

// At the end we divide the sample sum by the sum of those weights
// This effectively spreads out the brightness of the fireflies across all other samples
// If those other samples are dark the firefly fades (from catlike)
float4 BloomPrefilterFadeFireFliesFragment(Varyings input) : SV_TARGET
{
    float3 totalColor = 0.0;
    float weightSum = 0.0;

    // because there has Gaussian blur after pre filter, so remove the 4 samples to improve the performance
    // todo : optimal the cache miss
    float2 offsets[] =
    {
        float2(0.0, 0.0),
        // float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0),
        // float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0)
        float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)
    };

    
    for (int i = 0; i < 5; i++)
    {
        float3 color = GetSource(input.uv + offsets[i] * GetSourceTexelSize() * 2.0).rgb;
        color = ApplyBloomThreshold(color);
        float weight = 1.0 / (Luminance(color) + 1.0); // the more luminace, the less , and the weight is always less than 1 
        totalColor += color * weight;
        weightSum += weight;
    }
    
    totalColor /= weightSum;
    return float4(totalColor, 1.0);
}

float4 BloomHorizontalBlurFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
    
    float weights[] = // the weights come form the Pascal's triangle, row 13 (remove the edge 2 columns) 
    {
        0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
        0.19459459, 0.12162162, 0.05405405, 0.01621622
    };

    for (int i = 0; i < 9; ++i)
    {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += GetSource(input.uv + float2(offset, 0.0)).rgb * weights[i];
    }

    return float4(color, 1.0);
}

float4 BloomVerticalBlurFragment(Varyings input) : SV_TARGET
{
    float3 color = 0.0;
    // float offsets[] = {-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0};
    //
    // float weights[] = // the weights come form the Pascal's triangle, row 13 (remove the edge 2 columns) 
    // {
    //     0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
    //     0.19459459, 0.12162162, 0.05405405, 0.01621622
    // };

    // vertical can only sample 5 point use bilinear, but horizontal pass can not, because it has been used for  ??
    float offsets[] = {-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923};
    float weights[] = {0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027};

    for (int i = 0; i < 5; ++i)
    {
        float offset = offsets[i] * GetSourceTexelSize().y; // the downsample has been did in the horizontal blur, so this time do not double the texel size
        color += GetSource(input.uv + float2(0.0, offset)).rgb * weights[i];
    }

    return float4(color, 1.0);
}

float4 BloomAdditiveFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;

    if (_BloomBicubicUpsampling)
    {
        // todo : look the function impl
        // the lowRes added to the result will give the blicky appearance, especially glow the dark area
        lowRes = GetSourceBicubic(input.uv).rgb; 
    }

    else
    {
        lowRes = GetSource(input.uv).rgb;
    }
    
    float3 hightRes = GetSource2(input.uv).rgb;
    return float4(lowRes * _BloomIntensity + hightRes, 1.0);
}

float4 BloomScatteringFragment(Varyings input) : SV_TARGET
{
    float3 lowRes;

    if (_BloomBicubicUpsampling)
    {
        // todo : look the function impl
        // the lowRes added to the result will give the blicky appearance, especially glow the dark area
        lowRes = GetSourceBicubic(input.uv).rgb; 
    }

    else
    {
        lowRes = GetSource(input.uv).rgb;
    }
    
    float3 highRes = GetSource2(input.uv).rgb;
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

float4 BloomScatteringFinalFragment(Varyings input) :SV_TARGET
{
    float3 lowRes;

    if (_BloomBicubicUpsampling)
    {
        // todo : look the function impl
        // the lowRes added to the result will give the blicky appearance, especially glow the dark area
        lowRes = GetSourceBicubic(input.uv).rgb; 
    }

    else
    {
        lowRes = GetSource(input.uv).rgb;
    }
    
    float3 highRes = GetSource2(input.uv).rgb;
    lowRes += highRes - ApplyBloomThreshold(highRes); // compensate the missing light
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0);
}

// ACES adds a hue shift to very bright colors, pushing them toward white.
// This also happens when cameras or eyes get overwhelmed by too much light. (from catlike)
float4 ToneMappingACESFragment(Varyings input) : SV_Target
{
    float4 color = GetSource(input.uv);
    // because the precision limition, the very large values end up at 1 much earlier than infinity
    // It can become a problem for some functions when half values are used.
    // Due to a bug in the shader compiler this happens in some cases with the Metal API, even when float is used explicitly.
    // This also affects some MacBooks, not only mobiles. (from catlike)
    // 60 is a good limition
    color.rgb = min(color.rgb, 60.0); 
    color.rgb = AcesTonemap(unity_to_ACES(color.rgb)); // todo : the function impl
    return color;
}

float4 ToneMappingNeutralFragment(Varyings input) : SV_Target
{
    float4 color = GetSource(input.uv);
    // because the precision limition, the very large values end up at 1 much earlier than infinity
    // It can become a problem for some functions when half values are used.
    // Due to a bug in the shader compiler this happens in some cases with the Metal API, even when float is used explicitly.
    // This also affects some MacBooks, not only mobiles. (from catlike)
    // 60 is a good limition
    color.rgb = min(color.rgb, 60.0); 
    color.rgb = NeutralTonemap(color.rgb); // todo : the function impl
    return color;
}

float4 ToneMappingReinhardFragment(Varyings input) : SV_Target
{
    float4 color = GetSource(input.uv);
    // because the precision limition, the very large values end up at 1 much earlier than infinity
    // It can become a problem for some functions when half values are used.
    // Due to a bug in the shader compiler this happens in some cases with the Metal API, even when float is used explicitly.
    // This also affects some MacBooks, not only mobiles. (from catlike)
    // 60 is a good limition
    color.rgb = min(color.rgb, 60.0); 
    color.rgb /= (1 + color.rgb);
    return color;
}
#endif