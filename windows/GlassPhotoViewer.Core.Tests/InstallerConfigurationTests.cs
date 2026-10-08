namespace GlassPhotoViewer.Core.Tests;

public sealed class InstallerConfigurationTests
{
    private static readonly string InstallerScript = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../installer/GlassPhotoViewer.iss"));
    private static readonly string LegacyInstallerFixture = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../installer/GlassPhotos.LegacyUpgradeFixture.iss"));
    private static readonly string RepositoryRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../.."));

    [Fact]
    public void InstallerRegistersEverySupportedImageExtension()
    {
        var script = File.ReadAllText(InstallerScript);
        var extensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".heic", ".heif", ".tif", ".tiff",
            ".gif", ".bmp", ".webp", ".dng", ".nef", ".cr2", ".arw", ".raf"
        };

        foreach (var extension in extensions)
        {
            Assert.Contains(
                $"Subkey: \"Software\\GlassPhotos\\Capabilities\\FileAssociations\"; ValueType: string; ValueName: \"{extension}\"",
                script);
            Assert.Contains($"Software\\Classes\\{extension}\\OpenWithProgids", script);
        }
    }

    [Fact]
    public void InstallerAlwaysOverwritesExistingPrivateApplicationFiles()
    {
        var script = File.ReadAllText(InstallerScript);

        Assert.Contains("CloseApplications=force", script);
        Assert.Contains("CloseApplicationsFilter=GlassPhotoViewer.exe,GlassPhotos.WinUI.exe,*.dll,*.chm", script);
        Assert.Contains("Flags: ignoreversion overwritereadonly recursesubdirs createallsubdirs", script);
        Assert.DoesNotContain("confirmoverwrite", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InstallerRegistersQuotedOpenCommandAndUninstallCleanup()
    {
        var script = File.ReadAllText(InstallerScript);

        Assert.Contains("Software\\Classes\\GlassPhotos.Image\\shell\\open\\command", script);
        Assert.Contains("SetupIconFile=..\\GlassPhotoViewer.WinUI\\Assets\\GlassPhotoViewer.ico", script);
        Assert.Contains("UninstallDisplayIcon={app}\\GlassPhotoViewer.exe", script);
        Assert.Contains("Software\\Classes\\GlassPhotos.Image\\DefaultIcon", script);
        Assert.Contains("{app}\\GlassPhotoViewer.exe,0", script);
        Assert.Contains("Software\\RegisteredApplications", script);
    }

    [Fact]
    public void LegacyUpgradeFixtureUsesStableIdentityAndIsExercisedByWindowsCI()
    {
        Assert.True(File.Exists(LegacyInstallerFixture));
        var fixture = File.ReadAllText(LegacyInstallerFixture);
        var upgradeTest = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "windows",
            "test-legacy-upgrade.ps1"));
        var workflow = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "windows.yml"));

        Assert.Contains("AppId={{9A61D15E-34ED-4697-A8DB-773AD0DC8AA4}", fixture);
        Assert.Contains("DefaultDirName={localappdata}\\Programs\\Glass Photos", fixture);
        Assert.Contains("GlassPhotos.WinUI.exe", fixture);
        Assert.Contains("GlassPhotos.LegacyUpgradeFixture.iss", upgradeTest);
        Assert.Contains("GlassPhotos.WinUI.exe", upgradeTest);
        Assert.Contains("dotnet publish", upgradeTest);
        Assert.Contains("--self-contained true", upgradeTest);
        Assert.Contains("PublishSingleFile=true", upgradeTest);
        Assert.Contains("Thread.Sleep(Timeout.Infinite)", upgradeTest);
        Assert.DoesNotContain("Copy-Item $currentExe $legacyExeSource", upgradeTest);
        Assert.Contains("window-placement.json", upgradeTest);
        Assert.Contains("test-legacy-upgrade.ps1", workflow);
    }

    [Fact]
    public void RenamedInstallerKeepsUpgradeIdentityAndRemovesLegacyProductFiles()
    {
        var script = File.ReadAllText(InstallerScript);

        Assert.Contains("AppId={{9A61D15E-34ED-4697-A8DB-773AD0DC8AA4}", script);
        Assert.Contains("[InstallDelete]", script);
        Assert.Contains("{app}\\GlassPhotos.*", script);
        Assert.Contains("{app}\\Assets\\GlassPhotos.*", script);
    }
}
