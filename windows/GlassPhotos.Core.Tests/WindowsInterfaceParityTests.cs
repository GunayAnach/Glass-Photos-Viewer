namespace GlassPhotos.Core.Tests;

public sealed class WindowsInterfaceParityTests
{
    [Fact]
    public void WindowsAndMacUseTheSameReleaseVersion()
    {
        var windowsProject = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotos.WinUI/GlassPhotos.WinUI.csproj")));
        var macProject = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../glass photo viewer.xcodeproj/project.pbxproj")));

        Assert.Contains("<Version>1.2.0</Version>", windowsProject);
        Assert.Contains("MARKETING_VERSION = 1.2.0;", macProject);
    }

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
        Assert.Contains("Click=\"RotateLeft_Click\"", xaml);
        Assert.Contains("x:Name=\"RotateRightButton\"", xaml);
        Assert.Contains("Click=\"RotateRight_Click\"", xaml);
        Assert.Contains("x:Name=\"InfoRows\"", xaml);
        Assert.Contains("Click=\"Share_Click\"", xaml);
        Assert.Contains("x:Name=\"FullScreenButton\"", xaml);
        Assert.Contains("x:Name=\"DeleteButton\"", xaml);
        Assert.Contains("x:Name=\"CropButton\"", xaml);
        Assert.Contains("Click=\"Crop_Click\"", xaml);
        Assert.Contains("x:Name=\"CropOverlay\"", xaml);
        Assert.Contains("x:Name=\"CropSelection\"", xaml);
        Assert.Contains("<Grid.KeyboardAccelerators>", xaml);
        Assert.Contains("Key=\"Left\" Invoked=\"Previous_Invoked\"", xaml);
        Assert.Contains("Key=\"Right\" Invoked=\"Next_Invoked\"", xaml);
        Assert.Contains("Key=\"Up\" Invoked=\"RotateLeft_Invoked\"", xaml);
        Assert.Contains("Key=\"Down\" Invoked=\"RotateRight_Invoked\"", xaml);
        Assert.Contains("Key=\"Space\" Invoked=\"ToggleFit_Invoked\"", xaml);
        Assert.Contains("Key=\"Enter\" Invoked=\"Rename_Invoked\"", xaml);
        Assert.Contains("Key=\"Delete\" Invoked=\"Delete_Invoked\"", xaml);
        Assert.Contains("Key=\"Back\" Invoked=\"Delete_Invoked\"", xaml);
        Assert.Contains("Key=\"F\" Invoked=\"FullScreen_Invoked\"", xaml);
        Assert.Contains("Key=\"Escape\" Invoked=\"Escape_Invoked\"", xaml);
        Assert.Contains("Key=\"O\" Modifiers=\"Control\" Invoked=\"OpenFolder_Invoked\"", xaml);
        Assert.DoesNotContain("<CommandBar", xaml);
    }
}
