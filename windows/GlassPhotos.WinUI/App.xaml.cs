using Microsoft.UI.Xaml;

namespace GlassPhotos.WinUI;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var initialPath = Environment.GetCommandLineArgs()
            .Skip(1)
            .FirstOrDefault(File.Exists);

        _window = new MainWindow(initialPath);
        _window.Activate();
    }
}
