namespace GlassPhotoViewer.Core;

public static class WindowTitleFormatter
{
    private const string Prefix = "Glass Photo Viewer - ";

    public static string Format(string filePath, int maximumCharacters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var fullTitle = Prefix + filePath;
        if (fullTitle.Length <= maximumCharacters)
        {
            return fullTitle;
        }

        var separator = filePath.Contains('\\') ? '\\' : '/';
        var components = filePath
            .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (components.Count > 0 && components[0].EndsWith(':'))
        {
            components.RemoveAt(0);
        }

        var visibleComponents = components.TakeLast(Math.Min(3, components.Count));
        return Prefix + "…" + separator + string.Join(separator, visibleComponents);
    }
}
