using System.Runtime.InteropServices;

namespace PureMediaPlayer.Core.Interop;

public static partial class MfNativeBridge
{
    public const int MF_SDK_VERSION = 0x0002;
    public const int MF_API_VERSION = 0x0070;
    public const int MF_VERSION = (MF_SDK_VERSION << 16) | MF_API_VERSION;
    public const int MFSTARTUP_FULL = 0;

    public const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;
    public const uint MF_SOURCE_READER_FIRST_AUDIO_STREAM = 0xFFFFFFFD;

    [LibraryImport("mfplat.dll", EntryPoint = "MFStartup")]
    public static partial int InitializeMediaFoundation(int version, int startupFlags);

    [LibraryImport("mfplat.dll", EntryPoint = "MFShutdown")]
    public static partial int ShutdownMediaFoundation();

    [LibraryImport("mfreadwrite.dll", EntryPoint = "MFCreateSourceReaderFromURL", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int CreateSourceReaderFromUrl(
        string mediaUrl,
        nint attributeStorePtr,
        out nint sourceReaderPtr);
}