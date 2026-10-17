using System.Runtime.InteropServices;

namespace PureMediaPlayer.Core.Interop;

public static partial class WinmmAudioBridge
{
    public const int WAVE_FORMAT_IEEE_FLOAT = 0x0003;
    public const uint WAVE_MAPPER = 0xFFFFFFFF;

    [StructLayout(LayoutKind.Sequential)]
    public struct WaveFormatFloat
    {
        public short FormatTag;
        public short Channels;
        public int SamplesPerSec;
        public int AvgBytesPerSec;
        public short BlockAlign;
        public short BitsPerSample;
        public short ExtraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WaveBufferHeader
    {
        public nint DataBufferPtr;
        public uint BufferByteLength;
        public uint BytesRecorded;
        public nint UserContext;
        public uint Flags;
        public uint LoopCount;
        public nint NextPtr;
        public nint Reserved;
    }

    [LibraryImport("winmm.dll", EntryPoint = "waveOutOpen")]
    public static partial int OpenWaveDevice(
        out nint waveOutHandle,
        uint deviceId,
        ref WaveFormatFloat format,
        nint callbackPtr,
        nint instancePtr,
        uint openFlags);

    [LibraryImport("winmm.dll", EntryPoint = "waveOutPrepareHeader")]
    public static partial int PrepareBufferHeader(nint waveOutHandle, nint headerPtr, int headerByteSize);

    [LibraryImport("winmm.dll", EntryPoint = "waveOutWrite")]
    public static partial int WriteBufferToDevice(nint waveOutHandle, nint headerPtr, int headerByteSize);

    [LibraryImport("winmm.dll", EntryPoint = "waveOutReset")]
    public static partial int ResetWaveDevice(nint waveOutHandle);

    [LibraryImport("winmm.dll", EntryPoint = "waveOutClose")]
    public static partial int CloseWaveDevice(nint waveOutHandle);
}