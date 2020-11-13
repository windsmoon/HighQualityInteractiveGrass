namespace WindsmoonRP.PostProcessing
{
    public enum PostProcessingPass
    {
        Copy,
        BloomPreFilter,
        BloomPreFilterFadeFireFlies,
        BloomHorizontalBlur,
        BloomVerticalBlur,
        BloomAdditive,
        BloomScattering,
        BloomScatteringFinal,
        ToneMappingNone,
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard
    }
}