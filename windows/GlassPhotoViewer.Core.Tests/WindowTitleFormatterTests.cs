using GlassPhotoViewer.Core;

namespace GlassPhotoViewer.Core.Tests;

public sealed class WindowTitleFormatterTests
{
    [Fact]
    public void FullPathIsShownAfterProductNameWhenItFits()
    {
        const string path = @"C:\Users\Alex\Pictures\photo.jpg";

        var title = WindowTitleFormatter.Format(path, maximumCharacters: 200);

        Assert.Equal(@"Glass Photo Viewer - C:\Users\Alex\Pictures\photo.jpg", title);
    }

    [Fact]
    public void LongPathKeepsTwoFoldersAndFilename()
    {
        const string path = @"C:\Users\Alex\Pictures\Trips\Italy\photo.jpg";

        var title = WindowTitleFormatter.Format(path, maximumCharacters: 35);

        Assert.Equal(@"Glass Photo Viewer - …\Trips\Italy\photo.jpg", title);
    }

    [Fact]
    public void ShortPathDoesNotInventMissingFoldersWhenCondensed()
    {
        const string path = @"C:\Photos\photo.jpg";

        var title = WindowTitleFormatter.Format(path, maximumCharacters: 10);

        Assert.Equal(@"Glass Photo Viewer - …\Photos\photo.jpg", title);
    }
}
