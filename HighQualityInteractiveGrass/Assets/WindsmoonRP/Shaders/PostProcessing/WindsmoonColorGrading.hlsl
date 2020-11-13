#ifndef WINDSMOON_COLOR_GRADING_INCLUDED
#define WINDSMOON_COLOR_GRADING_INCLUDED

// x : Pow(2f, PostExposure)
// y : Contrast * 0.01f + 1f, contrast si -100 ~ 100, so the y is 0 ~ 2
// z : HueShift * (1f / 360f), HueShift is -180 ~ 180
// w : saturation is the same as contrast
float4 _ColorAdjustmentData;
float4 _ColorFilter;
float4 _WhiteBalanceData;
float4 _SplitToningShadowColor; // w is balance * 0.01f (-1 ~ 1)
float4 _SplitToningHighLightColor;

float3 ColorGradingPostExposure(float3 color)
{
    return color * _ColorAdjustmentData.x;
}

float3 ColorGradingWhiteBalance(float3 color)
{
    // LMS : It describes colors as the responses of the three photoreceptor cone types in the human eye. (from catlike)
    color = LinearToLMS(color);
    color *= _WhiteBalanceData.rgb;
    return LMSToLinear(color);
}

float3 ColorGradingContrast(float3 color)
{
    // convert to logc space can get better result
    color = LinearToLogC(color);
    color = (color - ACEScc_MIDGRAY) * _ColorAdjustmentData.y + ACEScc_MIDGRAY;
    return LogCToLinear(color);
}

float3 ColorGradingColorFilter(float3 color)
{
    return color * _ColorFilter.rgb;
}

float3 ColorGradingSplitToning(float3 color)
{
    color = PositivePow(color, 1.0f / 2.2f);
    float t = saturate(Luminance(saturate(color)) + _SplitToningShadowColor.w);
    float3 shadows = lerp(0.5, _SplitToningShadowColor.rgb, 1.0 - t);
    float3 highlights = lerp(0.5, _SplitToningHighLightColor.rgb, t);
    color = SoftLight(color, shadows);
    color = SoftLight(color, highlights);
    return PositivePow(color, 2.2f);
}

float3 ColorGradingHueShift(float3 color)
{
    color = RgbToHsv(color);
    float hue = color.x + _ColorAdjustmentData.z;
    color.x = RotateHue(hue, 0.0, 1.0);
    return HsvToRgb(color);
}

float3 ColorGradingSaturation (float3 color)
{
    float luminance = Luminance(color);
    return (color - luminance) * _ColorAdjustmentData.w + luminance;
}

float3 ColorGrading (float3 color)
{
    color = min(color, 60.0);
    color = ColorGradingPostExposure(color);
    color = ColorGradingWhiteBalance(color);
    color = ColorGradingContrast(color);     // contrast can make negative color
    color = ColorGradingColorFilter(color); // filter can work with negative color so we can apply it before eliminating negative valuses
    color = max(color, 0.0);
    // ?? wtf it is
    // color = ColorGradingSplitToning(color); // after color filter and eliminate the negative values
    color = ColorGradingHueShift(color); // hue shift muse wokr with positive values
    color = ColorGradingSaturation(color); // saturation is the last work, and it can make negative color
    color = max(color, 0.0);
    return color;
}

#endif