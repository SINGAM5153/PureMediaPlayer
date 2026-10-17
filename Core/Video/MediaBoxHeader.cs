namespace PureMediaPlayer.Core.Video;

public readonly record struct MediaBoxHeader(
    string BoxType,
    long PayloadOffset,
    long TotalBoxSize
);