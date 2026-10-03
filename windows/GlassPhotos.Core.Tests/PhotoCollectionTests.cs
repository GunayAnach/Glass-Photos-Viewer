using GlassPhotos.Core;

namespace GlassPhotos.Core.Tests;

public sealed class PhotoCollectionTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"glass-photos-{Guid.NewGuid():N}");

    public PhotoCollectionTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void OpenSelectedPhotoLoadsSupportedNeighboursInNaturalOrder()
    {
        var first = CreateFile("photo1.jpg");
        var selected = CreateFile("photo2.jpg");
        var tenth = CreateFile("photo10.jpg");
        CreateFile("notes.txt");

        var collection = PhotoCollection.Open(selected);

        Assert.Equal(new[] { first, selected, tenth }, collection.Files);
        Assert.Equal(1, collection.CurrentIndex);
        Assert.Equal(selected, collection.CurrentPath);
    }

    [Fact]
    public void NavigationStopsAtFolderBoundaries()
    {
        var first = CreateFile("one.jpg");
        var second = CreateFile("two.jpg");
        var collection = PhotoCollection.Open(first);

        Assert.False(collection.MovePrevious());
        Assert.Equal(first, collection.CurrentPath);
        Assert.True(collection.MoveNext());
        Assert.Equal(second, collection.CurrentPath);
        Assert.False(collection.MoveNext());
        Assert.Equal(second, collection.CurrentPath);
    }

    [Fact]
    public void RenameCurrentPersistsAndPreservesExtension()
    {
        var selected = CreateFile("before.jpg");
        var collection = PhotoCollection.Open(selected);

        var renamed = collection.RenameCurrent("after");

        Assert.Equal(Path.Combine(_directory, "after.jpg"), renamed);
        Assert.Equal(renamed, collection.CurrentPath);
        Assert.False(File.Exists(selected));
        Assert.True(File.Exists(renamed));
    }

    [Fact]
    public void RemovingCurrentPhotoSelectsTheNextNeighbour()
    {
        CreateFile("photo1.jpg");
        var selected = CreateFile("photo2.jpg");
        var next = CreateFile("photo3.jpg");
        var collection = PhotoCollection.Open(selected);
        File.Delete(selected);

        var replacement = collection.RemoveCurrentAfterDeletion();

        Assert.Equal(next, replacement);
        Assert.Equal(next, collection.CurrentPath);
        Assert.Equal(2, collection.Files.Count);
    }

    private string CreateFile(string name)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, []);
        return path;
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }
}
