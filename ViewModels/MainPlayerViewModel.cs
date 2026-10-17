using Microsoft.Win32;
using PureMediaPlayer.Core.Video;
using PureMediaPlayer.Mvvm;
using PureMediaPlayer.Security;
using PureMediaPlayer.Services.Theming;
using PureMediaPlayer.Services.Web;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace PureMediaPlayer.ViewModels;

public sealed class MainPlayerViewModel : ViewModelBase, IDisposable
{
    private readonly NativeVideoEngine _videoEngine = new();
    private readonly StandaloneWebVideoExtractor _webExtractor = new();
    private readonly Aes256EncryptionService _encryptionService = new();
    private readonly ThemeService _themeService = new();

    private string _inputUrlOrPath = string.Empty;
    private string _encryptionPassphrase = string.Empty;
    private string _statusBanner = "Ready - Zero-Dependency Native Engine Initialized (96kHz/32-bit Float DSP)";
    private float _audioGain = 1.0f;
    private ThemeMode _selectedTheme = ThemeMode.SystemDefault;
    private DiscoveredStream? _selectedStream;

    public ObservableCollection<DiscoveredStream> DiscoveredStreams { get; } = [];

    public WriteableBitmap VideoBitmap => _videoEngine.VideoSurface;

    public string InputUrlOrPath
    {
        get => _inputUrlOrPath;
        set => MutateField(ref _inputUrlOrPath, value);
    }

    public string EncryptionPassphrase
    {
        get => _encryptionPassphrase;
        set => MutateField(ref _encryptionPassphrase, value);
    }

    public string StatusBanner
    {
        get => _statusBanner;
        set => MutateField(ref _statusBanner, value);
    }

    public float AudioGain
    {
        get => _audioGain;
        set
        {
            if (MutateField(ref _audioGain, value))
            {
                _videoEngine.AudioEngine.Dsp.MasterVolume = value;
            }
        }
    }

    public ThemeMode SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (MutateField(ref _selectedTheme, value))
            {
                _themeService.ApplyTheme(value);
            }
        }
    }

    public DiscoveredStream? SelectedStream
    {
        get => _selectedStream;
        set
        {
            if (MutateField(ref _selectedStream, value) && value is not null)
            {
                _ = PlayResolvedStreamAsync(value.StreamUri);
            }
        }
    }

    public ActionCommand BrowseLocalFileCommand { get; }
    public AsyncActionCommand LoadOrScrapeUrlCommand { get; }
    public ActionCommand TogglePlayPauseCommand { get; }
    public AsyncActionCommand EncryptCurrentMediaCommand { get; }

    public MainPlayerViewModel()
    {
        _themeService.ApplyTheme(ThemeMode.SystemDefault);

        BrowseLocalFileCommand = new ActionCommand(_ => BrowseLocalFile());
        LoadOrScrapeUrlCommand = new AsyncActionCommand((_, ct) => ResolveAndPlayUrlAsync(ct));
        TogglePlayPauseCommand = new ActionCommand(_ => TogglePlaybackState());
        EncryptCurrentMediaCommand = new AsyncActionCommand((_, ct) => EncryptActiveFileWithAes256Async(ct));
    }

    private void BrowseLocalFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Media File or Encrypted Container",
            Filter = "All Media Formats|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            InputUrlOrPath = dialog.FileName;
            _ = PlayResolvedStreamAsync(new Uri(dialog.FileName));
        }
    }

    private async Task ResolveAndPlayUrlAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(InputUrlOrPath)) return;

        try
        {
            StatusBanner = "Scanning target URL for playable video streams...";
            DiscoveredStreams.Clear();

            IReadOnlyList<DiscoveredStream> streams = await _webExtractor.ResolvePlayableStreamsAsync(InputUrlOrPath, ct);
            foreach (DiscoveredStream stream in streams)
            {
                DiscoveredStreams.Add(stream);
            }

            if (DiscoveredStreams.Count > 0)
            {
                SelectedStream = DiscoveredStreams[0];
                StatusBanner = $"Found {DiscoveredStreams.Count} stream(s). Playing {SelectedStream.FormatHint}.";
            }
            else
            {
                StatusBanner = "No embedded video streams discovered on the webpage.";
            }
        }
        catch (Exception ex)
        {
            StatusBanner = $"Stream extraction error: {ex.Message}";
        }
    }

    private async Task PlayResolvedStreamAsync(Uri streamUri)
    {
        await _videoEngine.LoadMediaSourceAsync(streamUri);
        StatusBanner = $"Playing [{_videoEngine.ActiveContainerSummary}] -> {streamUri}";
    }

    private void TogglePlaybackState()
    {
        if (_videoEngine.IsPlaying)
        {
            _videoEngine.StopPlayback();
            StatusBanner = "Playback Paused.";
        }
        else
        {
            _videoEngine.StartPlaybackLoop();
            StatusBanner = "Playback Resumed.";
        }
    }

    private async Task EncryptActiveFileWithAes256Async(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(InputUrlOrPath) || !System.IO.File.Exists(InputUrlOrPath))
        {
            StatusBanner = "Select a local media file first to encrypt with AES-256-GCM.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EncryptionPassphrase))
        {
            StatusBanner = "Enter an encryption passphrase before sealing the media file.";
            return;
        }

        string outputPath = InputUrlOrPath + ".pmpe";
        StatusBanner = "Sealing media with 256-bit AES-GCM (100k PBKDF2-SHA256 iterations)...";
        await _encryptionService.EncryptFileAsync(InputUrlOrPath, outputPath, EncryptionPassphrase, ct);
        StatusBanner = $"Encrypted 256-bit container written to: {outputPath}";
    }

    public void Dispose()
    {
        _videoEngine.Dispose();
        _webExtractor.Dispose();
    }
}