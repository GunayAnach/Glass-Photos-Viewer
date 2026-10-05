using GlassPhotos.Core;

namespace GlassPhotos.Core.Tests;

public sealed class WindowPlacementStoreTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"glass-photos-placement-{Guid.NewGuid():N}");

    [Fact]
    public void PlacementRoundTripsAcrossApplicationLaunches()
    {
        var path = Path.Combine(_directory, "window-placement.json");
        var expected = new WindowPlacement(120, 80, 640, 480);

        WindowPlacementStore.Save(path, expected);
        var restored = WindowPlacementStore.Load(path);

        Assert.Equal(expected, restored);
    }

    [Fact]
    public void CorruptOrInvalidPlacementIsIgnored()
    {
        Directory.CreateDirectory(_directory);
        var corruptPath = Path.Combine(_directory, "corrupt.json");
        File.WriteAllText(corruptPath, "not json");
        var invalidPath = Path.Combine(_directory, "invalid.json");
        File.WriteAllText(invalidPath, "{\"X\":0,\"Y\":0,\"Width\":0,\"Height\":-2}");
        var unusablePath = Path.Combine(_directory, "unusable.json");
        File.WriteAllText(unusablePath, "{\"X\":0,\"Y\":0,\"Width\":1,\"Height\":1}");

        Assert.Null(WindowPlacementStore.Load(corruptPath));
        Assert.Null(WindowPlacementStore.Load(invalidPath));
        Assert.Null(WindowPlacementStore.Load(unusablePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
