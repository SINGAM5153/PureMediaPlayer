using PureMediaPlayer.Views;
using System.Windows;
using System.Windows.Media;

namespace PureMediaPlayer;

public partial class App : Application
{
    public App()
    {
        // Initialize default application theme resources in code
        Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(15, 17, 23));
        Resources["SurfaceCardBrush"] = new SolidColorBrush(Color.FromRgb(26, 29, 38));
        Resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
        Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(156, 163, 175));
        Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));
    }
}
