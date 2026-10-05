namespace GlassPhotos.Core;

public sealed class PhotoCollection
{
    private readonly List<string> _files;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".heic", ".heif", ".tif", ".tiff",
        ".gif", ".bmp", ".webp", ".dng", ".nef", ".cr2", ".arw", ".raf"
    };

    private PhotoCollection(IEnumerable<string> files, int currentIndex)
    {
        _files = files.ToList();
        CurrentIndex = currentIndex;
    }

    public IReadOnlyList<string> Files => _files;

    public int CurrentIndex { get; private set; }

    public string CurrentPath => _files[CurrentIndex];

    public string RenameCurrent(string newBaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newBaseName);
        if (newBaseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || newBaseName.Contains(Path.DirectorySeparatorChar)
            || newBaseName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("The new name contains invalid filename characters.", nameof(newBaseName));
        }

        var source = CurrentPath;
        var destination = Path.Combine(
            Path.GetDirectoryName(source)!,
            newBaseName.Trim() + Path.GetExtension(source));

        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.Equals(source, destination, StringComparison.Ordinal))
            {
                var temporary = Path.Combine(
                    Path.GetDirectoryName(source)!,
                    $".glassphotos-rename-{Guid.NewGuid():N}{Path.GetExtension(source)}");
                File.Move(source, temporary);
                try
                {
                    File.Move(temporary, destination);
                }
                catch
                {
                    File.Move(temporary, source);
                    throw;
                }
                _files[CurrentIndex] = destination;
            }
            return _files[CurrentIndex];
        }

        if (File.Exists(destination))
        {
            throw new IOException($"A file named '{Path.GetFileName(destination)}' already exists.");
        }

        File.Move(source, destination);
        _files[CurrentIndex] = destination;
        return destination;
    }

    public string? RemoveCurrentAfterDeletion()
    {
        _files.RemoveAt(CurrentIndex);
        if (_files.Count == 0)
        {
            CurrentIndex = -1;
            return null;
        }

        if (CurrentIndex >= _files.Count)
        {
            CurrentIndex = _files.Count - 1;
        }

        return CurrentPath;
    }

    public bool MovePrevious()
    {
        if (CurrentIndex == 0) return false;
        CurrentIndex--;
        return true;
    }

    public bool MoveNext()
    {
        if (CurrentIndex >= _files.Count - 1) return false;
        CurrentIndex++;
        return true;
    }

    public static PhotoCollection Open(string selectedPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedPath);

        var fullPath = Path.GetFullPath(selectedPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The selected photo does not exist.", fullPath);
        }

        if (!SupportedExtensions.Contains(Path.GetExtension(fullPath)))
        {
            throw new NotSupportedException($"'{Path.GetExtension(fullPath)}' is not a supported image type.");
        }

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("The selected photo has no containing folder.");

        var files = EnumerateSupportedFiles(directory);

        var index = Array.FindIndex(files, path => string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new InvalidOperationException("The selected photo was not found in its containing folder.");
        }

        return new PhotoCollection(files, index);
    }

    public static PhotoCollection OpenDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        var directory = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"The folder '{directory}' does not exist.");
        }

        var files = EnumerateSupportedFiles(directory);
        if (files.Length == 0)
        {
            throw new InvalidOperationException("The selected folder contains no supported photos.");
        }

        return new PhotoCollection(files, currentIndex: 0);
    }

    private static string[] EnumerateSupportedFiles(string directory) =>
        Directory.EnumerateFiles(directory)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => Path.GetFileName(path), NaturalFileNameComparer.Instance)
            .Select(Path.GetFullPath)
            .ToArray();

    private sealed class NaturalFileNameComparer : IComparer<string>
    {
        public static NaturalFileNameComparer Instance { get; } = new();

        public int Compare(string? left, string? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;

            var leftIndex = 0;
            var rightIndex = 0;
            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                if (char.IsDigit(left[leftIndex]) && char.IsDigit(right[rightIndex]))
                {
                    var leftStart = leftIndex;
                    var rightStart = rightIndex;
                    while (leftIndex < left.Length && char.IsDigit(left[leftIndex])) leftIndex++;
                    while (rightIndex < right.Length && char.IsDigit(right[rightIndex])) rightIndex++;

                    var leftDigits = left.AsSpan(leftStart, leftIndex - leftStart).TrimStart('0');
                    var rightDigits = right.AsSpan(rightStart, rightIndex - rightStart).TrimStart('0');
                    var lengthComparison = leftDigits.Length.CompareTo(rightDigits.Length);
                    if (lengthComparison != 0) return lengthComparison;

                    var digitComparison = leftDigits.CompareTo(rightDigits, StringComparison.Ordinal);
                    if (digitComparison != 0) return digitComparison;
                    continue;
                }

                var characterComparison = char.ToUpperInvariant(left[leftIndex]).CompareTo(char.ToUpperInvariant(right[rightIndex]));
                if (characterComparison != 0) return characterComparison;
                leftIndex++;
                rightIndex++;
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
