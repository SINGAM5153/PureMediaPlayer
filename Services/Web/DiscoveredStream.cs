namespace PureMediaPlayer.Services.Web;

public sealed record DiscoveredStream(
    Uri StreamUri,
    string FormatHint,
    string DiscoverySource
);