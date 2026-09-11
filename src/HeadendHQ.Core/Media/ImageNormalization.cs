namespace HeadendHQ.Core.Media.CommandHandlers;

internal static class ImageNormalization
{
    /// <summary>Normalization is chosen by purpose, so every entry point resolves it the same way.</summary>
    public static Task<byte[]> NormalizeAsync(
        this IImageNormalizer normalizer, byte[] bytes, ImagePurpose purpose, CancellationToken ct) =>
        purpose switch
        {
            ImagePurpose.TeamLogo => normalizer.NormalizeTeamLogoAsync(bytes, ct),
            ImagePurpose.LeagueLogo => normalizer.NormalizeLeagueLogoAsync(bytes, ct),
            ImagePurpose.BroadcasterLogo => normalizer.NormalizeStreamingLogoAsync(bytes, ct),
            ImagePurpose.Wordmark => normalizer.NormalizeWordMarkAsync(bytes, ct),
            ImagePurpose.Headshot => normalizer.NormalizeHeadshotAsync(bytes, ct),
            ImagePurpose.Poster => normalizer.NormalizePosterAsync(bytes, ct),
            ImagePurpose.Background or ImagePurpose.Thumbnail => normalizer.NormalizeBackgroundAsync(bytes, ct),
            _ => Task.FromResult(bytes),
        };
}
