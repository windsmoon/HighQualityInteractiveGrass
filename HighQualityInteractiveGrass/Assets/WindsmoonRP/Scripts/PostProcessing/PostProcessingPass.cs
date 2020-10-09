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
        ToneMappingACES,
        ToneMappingNeutral,
        ToneMappingReinhard
    }
}