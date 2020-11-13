#ifndef WINDSMOON_POST_PROCESSING_INCLUDED
#define WINDSMOON_POST_PROCESSING_INCLUDED

TEXTURE2D(_PostProcessingSource);
TEXTURE2D(_PostProcessingSource2);
SAMPLER(sampler_linear_clamp);
float4 _PostProcessingSource_TexelSize;
float4 _ProjectionParams; // x : if x is less than 0, the v is from top to bottom

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

float4 CopyFragment(Varyings input) : SV_TARGET
{
    return GetSource(input.uv);
}

#endif