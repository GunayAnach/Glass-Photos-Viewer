using System.Text.RegularExpressions;

namespace GlassPhotoViewer.Core.Tests;

public sealed class WindowsInterfaceParityTests
{
    [Fact]
    public void AppIconIsConfiguredForTheExecutableAndRuntimeWindow()
    {
        var projectDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI"));
        var project = File.ReadAllText(Path.Combine(projectDirectory, "GlassPhotoViewer.WinUI.csproj"));
        var windowCode = File.ReadAllText(Path.Combine(projectDirectory, "MainWindow.xaml.cs"));

        Assert.Contains("<ApplicationIcon>Assets\\GlassPhotoViewer.ico</ApplicationIcon>", project);
        Assert.True(File.Exists(Path.Combine(projectDirectory, "Assets", "GlassPhotoViewer.ico")));
        Assert.Contains("_appWindow.SetIcon", windowCode);
        Assert.Contains("AppContext.BaseDirectory", windowCode);
    }

    [Fact]
    public void WindowTitleUsesResponsivePathFormatting()
    {
        var windowCode = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml.cs")));

        Assert.Contains("WindowTitleFormatter.Format", windowCode);
        Assert.Contains("_appWindow.Changed +=", windowCode);
        Assert.Contains("DidSizeChange", windowCode);
    }

    [Fact]
    public void WindowPlacementIsRestoredAndSaved()
    {
        var windowCode = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml.cs")));

        Assert.Contains("WindowPlacementStore.Load", windowCode);
        Assert.Contains("WindowPlacementStore.Save", windowCode);
        Assert.Contains("DidPositionChange", windowCode);
        Assert.Contains("MoveAndResize", windowCode);
        Assert.Contains("OverlappedPresenterState.Restored", windowCode);
        Assert.Contains("\"Glass Photo Viewer\"", windowCode);
        Assert.Contains("_legacyWindowPlacementPath", windowCode);
        Assert.Contains("\"Glass Photos\"", windowCode);
        Assert.DoesNotContain("Math.Max(placement.Width, 800)", windowCode);
        Assert.DoesNotContain("Math.Max(placement.Height, 600)", windowCode);
    }

    [Fact]
    public void WindowsInfoSidebarMatchesMacInformationFields()
    {
        var windowsSource = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml.cs")));
        var macSource = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../OSx/GlassPhotoViewer/GlassPhotoViewerApp.swift")));

        static string[] DistinctLabels(string source, string pattern) =>
            Regex.Matches(source, pattern)
                .Select(match => match.Groups[1].Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

        var macLabels = new[] { "File Name", "File Path" }
            .Concat(DistinctLabels(macSource, "exifData\\.append\\(\\(\"([^\"]+)\""))
            .ToArray();
        var windowsLabels = new[] { "File Name", "File Path" }
            .Concat(DistinctLabels(windowsSource, "rows\\.Add\\(\\(\"([^\"]+)\""))
            .ToArray();

        Assert.Equal(macLabels, windowsLabels);
    }

    [Fact]
    public void WindowsAndMacUseTheSameReleaseVersion()
    {
        var windowsProject = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/GlassPhotoViewer.WinUI.csproj")));
        var macProject = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../OSx/GlassPhotoViewer.xcodeproj/project.pbxproj")));

        Assert.Contains("<Version>1.2.1</Version>", windowsProject);
        Assert.Contains("MARKETING_VERSION = 1.2.1;", macProject);
    }

    [Fact]
    public void ViewerUsesTheSameVisualStructureAsTheMacInterface()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml"));
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"TopHeader\"", xaml);
        Assert.Contains("x:Name=\"InfoSidebar\"", xaml);
        Assert.Contains("Content=\"Open Folder…\"", xaml);
        Assert.Contains("Text=\"Glass Photo Viewer\"", xaml);
        Assert.Contains("x:Name=\"RenamePanel\"", xaml);
        Assert.Contains("x:Name=\"RotateLeftButton\"", xaml);
        Assert.Contains("Click=\"RotateLeft_Click\"", xaml);
        Assert.Contains("x:Name=\"RotateRightButton\"", xaml);
        Assert.Contains("Click=\"RotateRight_Click\"", xaml);
        Assert.Contains("x:Name=\"InfoRows\"", xaml);
        Assert.Contains("Click=\"Share_Click\"", xaml);
        Assert.Contains("x:Name=\"FullScreenButton\"", xaml);
        Assert.Contains("x:Name=\"DeleteButton\"", xaml);
        Assert.True(
            xaml.IndexOf("x:Name=\"DeleteButton\"", StringComparison.Ordinal) <
            xaml.IndexOf("x:Name=\"FullScreenButton\"", StringComparison.Ordinal),
            "Fullscreen must appear after Delete so it is the rightmost toolbar action.");
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
        Assert.Contains("Key=\"F2\" Invoked=\"RenameOnly_Invoked\"", xaml);
        Assert.Contains("Key=\"I\" Invoked=\"Info_Invoked\"", xaml);
        Assert.Contains("Key=\"C\" Invoked=\"Crop_Invoked\"", xaml);
        Assert.Contains("Key=\"Delete\" Invoked=\"Delete_Invoked\"", xaml);
        Assert.Contains("Key=\"Back\" Invoked=\"Delete_Invoked\"", xaml);
        Assert.Contains("Key=\"F\" Invoked=\"FullScreen_Invoked\"", xaml);
        Assert.Contains("Key=\"F11\" Invoked=\"FullScreen_Invoked\"", xaml);
        Assert.Contains("Key=\"Escape\" Invoked=\"Escape_Invoked\"", xaml);
        Assert.Contains("Key=\"O\" Modifiers=\"Control\" Invoked=\"OpenFolder_Invoked\"", xaml);
        Assert.DoesNotContain("<CommandBar", xaml);
    }

    [Fact]
    public void ToolbarPlacesFullscreenAtTheRightEdgeOnBothPlatforms()
    {
        var windowsXaml = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml")));
        var macSource = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../OSx/GlassPhotoViewer/GlassPhotoViewerApp.swift")));

        Assert.True(
            windowsXaml.IndexOf("x:Name=\"DeleteButton\"", StringComparison.Ordinal) <
            windowsXaml.IndexOf("x:Name=\"FullScreenButton\"", StringComparison.Ordinal));
        Assert.True(
            macSource.IndexOf("Button(action: onDelete)", StringComparison.Ordinal) <
            macSource.IndexOf("Button(action: onFullScreen)", StringComparison.Ordinal));
    }

    [Fact]
    public void EscapeClosesTheWindowsApplication()
    {
        var windowCode = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml.cs")));

        Assert.Contains("private void Escape_Invoked", windowCode);
        Assert.Contains("Close();", windowCode);
    }

    [Fact]
    public void TooltipsShowTheMatchingKeyboardShortcuts()
    {
        var xaml = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml")));

        Assert.Contains("ToolTipService.ToolTip=\"Rename (Enter / F2)\"", xaml);
        Assert.Contains("ToolTipService.ToolTip=\"Rotate Counter-Clockwise (↑)\"", xaml);
        Assert.Contains("ToolTipService.ToolTip=\"Rotate Clockwise (↓)\"", xaml);
        Assert.Contains("ToolTipService.ToolTip=\"Info (I)\"", xaml);
        Assert.Contains("ToolTipService.ToolTip=\"Crop Photo (C)\"", xaml);
        Assert.Contains("ToolTipService.ToolTip=\"Delete Photo (Del)\"", xaml);
        Assert.Contains("ToolTipService.ToolTip=\"Full Screen (F / F11)\"", xaml);
    }

    [Fact]
    public void WindowsInfoPanelRequestsMacMetadataFields()
    {
        var windowCode = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../GlassPhotoViewer.WinUI/MainWindow.xaml.cs")));

        foreach (var property in new[]
                 {
                     "System.Image.ColorSpace", "System.Photo.ExposureTime", "System.Photo.FNumber",
                     "System.Photo.ISOSpeed", "System.Photo.FocalLength", "System.Photo.LensModel"
                 })
        {
            Assert.Contains(property, windowCode);
        }
    }
}
