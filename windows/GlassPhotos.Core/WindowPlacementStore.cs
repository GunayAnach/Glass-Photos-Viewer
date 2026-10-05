using System.Text.Json;

namespace GlassPhotos.Core;

public sealed record WindowPlacement(int X, int Y, int Width, int Height)
{
    public bool IsValid => Width >= 320 && Height >= 240;
}

public static class WindowPlacementStore
{
    public static WindowPlacement? Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var placement = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(path));
            return placement?.IsValid == true ? placement : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public static void Save(string path, WindowPlacement placement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!placement.IsValid) throw new ArgumentOutOfRangeException(nameof(placement));

        var directory = Path.GetDirectoryName(path)
            ?? throw new ArgumentException("The placement path has no parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(placement));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
