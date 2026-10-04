namespace GlassPhotos.Core.Tests;

public sealed class WindowsInterfaceParityTests
{
    [Fact]
    public void ViewerUsesTheSameVisualStructureAsTheMacInterface()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotos.WinUI/MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"TopHeader\"", xaml);
        Assert.Contains("x:Name=\"InfoSidebar\"", xaml);
        Assert.Contains("Content=\"Open Folder…\"", xaml);
        Assert.Contains("Text=\"Glass Photos\"", xaml);
        Assert.Contains("x:Name=\"RenamePanel\"", xaml);
        Assert.Contains("x:Name=\"RotateLeftButton\"", xaml);
        Assert.Contains("x:Name=\"RotateRightButton\"", xaml);
        Assert.Contains("x:Name=\"FullScreenButton\"", xaml);
        Assert.Contains("x:Name=\"DeleteButton\"", xaml);
        Assert.DoesNotContain("<CommandBar", xaml);
    }
}
