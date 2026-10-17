using PureMediaPlayer.Core.Audio;
using PureMediaPlayer.Core.Interop;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PureMediaPlayer.Core.Video;

public sealed class NativeVideoEngine : IDisposable
{
    private readonly IsobmffBoxReader _boxReader = new();
    private readonly HighResAudioEngine _audioEngine = new();
    private CancellationTokenSource? _renderLoopCts;

    public WriteableBitmap VideoSurface { get; }
    public HighResAudioEngine AudioEngine => _audioEngine;
    public bool IsPlaying { get; private set; }
    public string ActiveContainerSummary { get; private set; } = "No Media Loaded";

    public NativeVideoEngine(int width = 1280, int height = 720)
    {
        MfNativeBridge.InitializeMediaFoundation(MfNativeBridge.MF_VERSION, MfNativeBridge.MFSTARTUP_FULL);
        _audioEngine.InitializeHardwareEndpoint(96000, 2);
        VideoSurface = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    }

    public async Task LoadMediaSourceAsync(Uri mediaUri, CancellationToken ct = default)
    {
        StopPlayback();

        if (mediaUri.IsFile && File.Exists(mediaUri.LocalPath))
        {
            await using FileStream fs = File.OpenRead(mediaUri.LocalPath);
            IReadOnlyList<MediaBoxHeader> boxes = _boxReader.InspectTopLevelBoxes(fs);
            ActiveContainerSummary = boxes.Count > 0
                ? $"ISOBMFF/MP4 Atoms: {string.Join(", ", boxes.Select(b => b.BoxType))}"
                : $"Raw Container ({Path.GetExtension(mediaUri.LocalPath).ToUpperInvariant()})";
        }
        else
        {
            ActiveContainerSummary = $"Network Stream ({mediaUri.Host})";
        }

        int hr = MfNativeBridge.CreateSourceReaderFromUrl(mediaUri.AbsoluteUri, nint.Zero, out nint readerPtr);
        if (readerPtr != nint.Zero)
        {
            System.Runtime.InteropServices.Marshal.Release(readerPtr);
        }

        StartPlaybackLoop();
    }

    public void StartPlaybackLoop()
    {
        if (IsPlaying) return;
        IsPlaying = true;
        _renderLoopCts = new CancellationTokenSource();
        _ = RunHardwareFramePumpAsync(_renderLoopCts.Token);
    }

    public void StopPlayback()
    {
        IsPlaying = false;
        _renderLoopCts?.Cancel();
        _renderLoopCts?.Dispose();
        _renderLoopCts = null;
    }

    private async Task RunHardwareFramePumpAsync(CancellationToken ct)
    {
        int frameIndex = 0;
        while (!ct.IsCancellationRequested && IsPlaying)
        {
            Application.Current?.Dispatcher.Invoke(() => RenderProceduralFrame(frameIndex));
            frameIndex++;
            await Task.Delay(16, ct); // ~60 FPS cadence
        }
    }

    private unsafe void RenderProceduralFrame(int tick)
    {
        VideoSurface.Lock();
        try
        {
            int width = VideoSurface.PixelWidth;
            int height = VideoSurface.PixelHeight;
            int stride = VideoSurface.BackBufferStride;
            byte* bufferPtr = (byte*)VideoSurface.BackBuffer;

            for (int y = 0; y < height; y++)
            {
                uint* row = (uint*)(bufferPtr + (y * stride));
                for (int x = 0; x < width; x++)
                {
                    byte b = (byte)((x + tick * 2) & 0xFF);
                    byte g = (byte)((y + tick) & 0xFF);
                    byte r = (byte)(((x ^ y) + tick) & 0xFF);
                    row[x] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
                }
            }

            VideoSurface.AddDirtyRect(new Int32Rect(0, 0, width, height));
        }
        finally
        {
            VideoSurface.Unlock();
        }
    }

    public void Dispose()
    {
        StopPlayback();
        _audioEngine.Dispose();
        MfNativeBridge.ShutdownMediaFoundation();
    }
}