using PureMediaPlayer.Core.Interop;
using System.Runtime.InteropServices;

namespace PureMediaPlayer.Core.Audio;

public sealed class HighResAudioEngine : IDisposable
{
    private readonly AudioDspProcessor _dspProcessor = new();
    private nint _deviceHandle;
    private bool _isDeviceInitialized;

    public AudioDspProcessor Dsp => _dspProcessor;

    public void InitializeHardwareEndpoint(int sampleRate = 96000, short channels = 2)
    {
        CloseEndpoint();

        var floatFormat = new WinmmAudioBridge.WaveFormatFloat
        {
            FormatTag = WinmmAudioBridge.WAVE_FORMAT_IEEE_FLOAT,
            Channels = channels,
            SamplesPerSec = sampleRate,
            BitsPerSample = 32,
            BlockAlign = (short)(channels * sizeof(float)),
            AvgBytesPerSec = sampleRate * channels * sizeof(float),
            ExtraSize = 0
        };

        int result = WinmmAudioBridge.OpenWaveDevice(
            out _deviceHandle,
            WinmmAudioBridge.WAVE_MAPPER,
            ref floatFormat,
            nint.Zero,
            nint.Zero,
            0);

        _isDeviceInitialized = (result == 0 && _deviceHandle != nint.Zero);
    }

    public unsafe void SubmitFloatSamples(Span<float> pcmSamples)
    {
        if (!_isDeviceInitialized || pcmSamples.IsEmpty) return;

        _dspProcessor.ProcessHighResFloatBuffer(pcmSamples);

        int byteCount = pcmSamples.Length * sizeof(float);
        nint unmanagedAudioBuffer = Marshal.AllocHGlobal(byteCount);
        nint unmanagedHeaderPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WinmmAudioBridge.WaveBufferHeader>());

        fixed (float* srcPtr = pcmSamples)
        {
            Buffer.MemoryCopy(srcPtr, (void*)unmanagedAudioBuffer, byteCount, byteCount);
        }

        var header = new WinmmAudioBridge.WaveBufferHeader
        {
            DataBufferPtr = unmanagedAudioBuffer,
            BufferByteLength = (uint)byteCount,
            Flags = 0
        };

        Marshal.StructureToPtr(header, unmanagedHeaderPtr, false);
        WinmmAudioBridge.PrepareBufferHeader(_deviceHandle, unmanagedHeaderPtr, Marshal.SizeOf<WinmmAudioBridge.WaveBufferHeader>());
        WinmmAudioBridge.WriteBufferToDevice(_deviceHandle, unmanagedHeaderPtr, Marshal.SizeOf<WinmmAudioBridge.WaveBufferHeader>());
    }

    public void CloseEndpoint()
    {
        if (_isDeviceInitialized && _deviceHandle != nint.Zero)
        {
            WinmmAudioBridge.ResetWaveDevice(_deviceHandle);
            WinmmAudioBridge.CloseWaveDevice(_deviceHandle);
            _deviceHandle = nint.Zero;
            _isDeviceInitialized = false;
        }
    }

    public void Dispose() => CloseEndpoint();
}