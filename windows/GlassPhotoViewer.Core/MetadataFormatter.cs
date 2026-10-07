using System.Collections;
using System.Globalization;

namespace GlassPhotoViewer.Core;

public static class MetadataFormatter
{
    public static string? FormatPropertyValue(object? value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (value is null) return null;
        if (value is string text) return string.IsNullOrWhiteSpace(text) ? null : text;

        if (value is IEnumerable values)
        {
            var items = values.Cast<object?>()
                .Select(item => Convert.ToString(item, culture))
                .Where(item => !string.IsNullOrWhiteSpace(item));
            var joined = string.Join(" | ", items!);
            return string.IsNullOrWhiteSpace(joined) ? null : joined;
        }

        var scalar = Convert.ToString(value, culture);
        return string.IsNullOrWhiteSpace(scalar) ? null : scalar;
    }

    public static string? FormatDate(object? value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        return value switch
        {
            DateTimeOffset offset => offset.ToLocalTime().ToString("g", culture),
            DateTime date => date.ToLocalTime().ToString("g", culture),
            _ when value is not null && DateTimeOffset.TryParse(Convert.ToString(value, culture), culture,
                DateTimeStyles.AssumeLocal, out var parsed) => parsed.ToString("g", culture),
            _ => null
        };
    }

    public static bool TryReadDouble(object? value, out double result)
    {
        try
        {
            if (value is null)
            {
                result = 0;
                return false;
            }
            result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsFinite(result);
        }
        catch (Exception) when (value is not string)
        {
            result = 0;
            return false;
        }
        catch (FormatException)
        {
            result = 0;
            return false;
        }
    }

    public static string FormatColorSpace(object value, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        try
        {
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture) switch
            {
                1 => "sRGB",
                65535 => "Uncalibrated",
                var number => number.ToString(CultureInfo.InvariantCulture)
            };
        }
        catch
        {
            return Convert.ToString(value, culture) ?? "Unknown";
        }
    }

    public static string FormatExposureTime(double seconds) =>
        seconds >= 1
            ? $"{seconds.ToString("0.0", CultureInfo.InvariantCulture)}s"
            : $"1/{Math.Max(1, (int)Math.Round(1 / Math.Max(seconds, double.Epsilon)))}s";

    public static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        var format = unit == 0 ? "0" : "0.#";
        return $"{value.ToString(format, CultureInfo.InvariantCulture)} {units[unit]}";
    }
}
