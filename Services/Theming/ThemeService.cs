using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace PureMediaPlayer.Services.Theming;

public sealed class ThemeService
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValue = "AppsUseLightTheme";

    public ThemeMode CurrentMode { get; private set; } = ThemeMode.SystemDefault;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    public void ApplyTheme(ThemeMode requestedMode)
    {
        CurrentMode = requestedMode;
        bool useDarkPalette = requestedMode switch
        {
            ThemeMode.Dark => true,
            ThemeMode.Light => false,
            _ => !IsWindowsInLightMode()
        };

        ResourceDictionary resources = Application.Current.Resources;

        if (useDarkPalette)
        {
            resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(15, 17, 23));
            resources["SurfaceCardBrush"] = new SolidColorBrush(Color.FromRgb(26, 29, 38));
            resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
            resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(156, 163, 175));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));
        }
        else
        {
            resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(248, 250, 252));
            resources["SurfaceCardBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(15, 23, 42));
            resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(100, 116, 139));
            resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(2, 132, 199));
        }
    }

    private static bool IsWindowsInLightMode()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        object? rawValue = key?.GetValue(AppsUseLightThemeValue);
        return rawValue is int intVal ? intVal != 0 : true;
    }

    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (CurrentMode == ThemeMode.SystemDefault)
        {
            Application.Current?.Dispatcher.Invoke(() => ApplyTheme(ThemeMode.SystemDefault));
        }
    }
}