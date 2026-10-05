namespace GlassPhotoViewer.Core.Tests;

public sealed class RepositoryLayoutTests
{
    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../.."));

    [Fact]
    public void PlatformSourcesUseDedicatedOSxAndWindowsDirectories()
    {
        Assert.True(Directory.Exists(Path.Combine(RepositoryRoot, "OSx", "GlassPhotoViewer")));
        Assert.True(Directory.Exists(Path.Combine(RepositoryRoot, "OSx", "GlassPhotoViewer.xcodeproj")));
        Assert.True(File.Exists(Path.Combine(
            RepositoryRoot,
            "OSx",
            "GlassPhotoViewer.xcodeproj",
            "xcshareddata",
            "xcschemes",
            "Glass Photo Viewer.xcscheme")));
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "OSx", "Package.swift")));
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "OSx", "build-release.sh")));
        Assert.True(Directory.Exists(Path.Combine(RepositoryRoot, "OSx", "Tests", "GlassPhotoViewerCoreTests")));

        Assert.True(Directory.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotoViewer.Core")));
        Assert.True(Directory.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotoViewer.Core.Tests")));
        Assert.True(Directory.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotoViewer.WinUI")));
        Assert.True(File.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotoViewer.Windows.sln")));

        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, "Glass Photos")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, "Tests")));
        Assert.False(File.Exists(Path.Combine(RepositoryRoot, "Package.swift")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, "glass photo viewer.xcodeproj")));
        Assert.False(File.Exists(Path.Combine(RepositoryRoot, "OSx", "create_dmg_simple.sh")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotos.Core")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotos.Core.Tests")));
        Assert.False(Directory.Exists(Path.Combine(RepositoryRoot, "windows", "GlassPhotos.WinUI")));
    }

    [Fact]
    public void MacReleaseBuildIsUniversalAndCoveredByCI()
    {
        var releaseScript = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "OSx",
            "build-release.sh"));
        var macProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "OSx",
            "GlassPhotoViewer.xcodeproj",
            "project.pbxproj"));
        var workflowPath = Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "macos.yml");

        Assert.Contains("-destination generic/platform=macOS", releaseScript);
        Assert.Contains("ARCHS=\"arm64 x86_64\"", releaseScript);
        Assert.Contains("lipo \"$EXECUTABLE\" -verify_arch arm64", releaseScript);
        Assert.Contains("lipo \"$EXECUTABLE\" -verify_arch x86_64", releaseScript);
        Assert.Contains("PBXFileSystemSynchronizedBuildFileExceptionSet", macProject);
        Assert.Contains("membershipExceptions = (", macProject);
        Assert.True(File.Exists(workflowPath));

        var workflow = File.ReadAllText(workflowPath);
        Assert.Contains("runs-on: macos-latest", workflow);
        Assert.Contains("./OSx/build-release.sh", workflow);
    }

    [Fact]
    public void ProductBrandingUsesGlassPhotoViewerAcrossBothPlatforms()
    {
        var macProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "OSx",
            "GlassPhotoViewer.xcodeproj",
            "project.pbxproj"));
        var macApp = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "OSx",
            "GlassPhotoViewer",
            "GlassPhotoViewerApp.swift"));
        var windowsProject = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "windows",
            "GlassPhotoViewer.WinUI",
            "GlassPhotoViewer.WinUI.csproj"));
        var windowsXaml = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "windows",
            "GlassPhotoViewer.WinUI",
            "MainWindow.xaml"));
        var installer = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "windows",
            "installer",
            "GlassPhotoViewer.iss"));

        Assert.Contains("Glass Photo Viewer.app", macProject);
        Assert.Contains("INFOPLIST_KEY_CFBundleDisplayName = \"Glass Photo Viewer\";", macProject);
        Assert.Contains("WindowGroup(\"Glass Photo Viewer\")", macApp);

        Assert.Contains("<AssemblyName>GlassPhotoViewer</AssemblyName>", windowsProject);
        Assert.Contains("<Product>Glass Photo Viewer</Product>", windowsProject);
        Assert.Contains("<AssemblyTitle>Glass Photo Viewer</AssemblyTitle>", windowsProject);
        Assert.Contains("<ApplicationIcon>Assets\\GlassPhotoViewer.ico</ApplicationIcon>", windowsProject);
        Assert.Contains("Title=\"Glass Photo Viewer\"", windowsXaml);
        Assert.Contains("Text=\"Glass Photo Viewer\"", windowsXaml);

        Assert.Contains("AppName=Glass Photo Viewer", installer);
        Assert.Contains("OutputBaseFilename=Glass-Photo-Viewer-Windows-x64-Setup", installer);
        Assert.Contains("{app}\\GlassPhotoViewer.exe", installer);
    }
}
