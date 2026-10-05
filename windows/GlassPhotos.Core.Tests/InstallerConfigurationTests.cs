namespace GlassPhotos.Core.Tests;

public sealed class InstallerConfigurationTests
{
    private static readonly string InstallerScript = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../installer/GlassPhotos.iss"));

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
        Assert.Contains("Flags: ignoreversion overwritereadonly recursesubdirs createallsubdirs", script);
        Assert.DoesNotContain("confirmoverwrite", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InstallerRegistersQuotedOpenCommandAndUninstallCleanup()
    {
        var script = File.ReadAllText(InstallerScript);

        Assert.Contains("Software\\Classes\\GlassPhotos.Image\\shell\\open\\command", script);
        Assert.Contains("{app}\\GlassPhotos.WinUI.exe", script);
        Assert.Contains("%1", script);
        Assert.Contains("uninsdeletekey", script);
        Assert.Contains("Software\\RegisteredApplications", script);
    }
}
