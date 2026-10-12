namespace GlassPhotoViewer.Core;

public static class PhotoPrintLayout
{
    public static PrintOrientation Orientation(double width, double height) =>
        width > height ? PrintOrientation.Landscape : PrintOrientation.Portrait;
}

public enum PrintOrientation
{
    Portrait,
    Landscape
}
