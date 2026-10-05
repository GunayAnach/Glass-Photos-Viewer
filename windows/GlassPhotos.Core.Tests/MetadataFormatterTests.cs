using System.Globalization;
using GlassPhotos.Core;

namespace GlassPhotos.Core.Tests;

public sealed class MetadataFormatterTests
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Fact]
    public void FormatsScalarAndMultiValueMetadata()
    {
        Assert.Equal("Canon", MetadataFormatter.FormatPropertyValue("Canon", Culture));
        Assert.Equal("Canon | EOS R5", MetadataFormatter.FormatPropertyValue(new[] { "Canon", "EOS R5" }, Culture));
        Assert.Null(MetadataFormatter.FormatPropertyValue(Array.Empty<string>(), Culture));
    }

    [Fact]
    public void FormatsMetadataDatesUsingTheRequestedCulture()
    {
        var localDate = new DateTime(2026, 10, 5, 14, 30, 0);
        var value = new DateTimeOffset(localDate, TimeZoneInfo.Local.GetUtcOffset(localDate));
        Assert.Equal("10/05/2026 14:30", MetadataFormatter.FormatDate(value, Culture));
    }

    [Theory]
    [InlineData(1u, "sRGB")]
    [InlineData(65535u, "Uncalibrated")]
    [InlineData(2u, "2")]
    public void FormatsColorSpaceCodes(uint value, string expected)
    {
        Assert.Equal(expected, MetadataFormatter.FormatColorSpace(value, Culture));
    }

    [Theory]
    [InlineData(0.008, "1/125s")]
    [InlineData(0.5, "1/2s")]
    [InlineData(1.5, "1.5s")]
    public void FormatsExposureTime(double seconds, string expected)
    {
        Assert.Equal(expected, MetadataFormatter.FormatExposureTime(seconds));
    }

    [Theory]
    [InlineData(500L, "500 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1 MB")]
    public void FormatsFileSize(long bytes, string expected)
    {
        Assert.Equal(expected, MetadataFormatter.FormatFileSize(bytes));
    }
}
