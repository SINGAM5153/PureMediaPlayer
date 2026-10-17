using PureMediaPlayer.Views;
using System.Windows;
using System.Windows.Media;

namespace PureMediaPlayer;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = new App();

        // Initialize default application theme resources in code
        app.Resources["WindowBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(15, 17, 23));
        app.Resources["SurfaceCardBrush"] = new SolidColorBrush(Color.FromRgb(26, 29, 38));
        app.Resources["PrimaryTextBrush"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
        app.Resources["SecondaryTextBrush"] = new SolidColorBrush(Color.FromRgb(156, 163, 175));
        app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(56, 189, 248));

        var mainWindow = new MainWindow();
        app.Run(mainWindow);
    }
}