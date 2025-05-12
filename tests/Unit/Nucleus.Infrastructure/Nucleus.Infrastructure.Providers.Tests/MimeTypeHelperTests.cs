using Xunit;
using Nucleus.Abstractions; // Required for NucleusConstants
using Nucleus.Infrastructure.Providers.ContentExtraction;

namespace Nucleus.Infrastructure.Providers.Tests.ContentExtraction;

public class MimeTypeHelperTests
{
    [Theory]
    [InlineData("text/plain", NucleusConstants.ExtractorKeys.PlainText)]
    [InlineData("TEXT/PLAIN", NucleusConstants.ExtractorKeys.PlainText)] // Case insensitivity
    [InlineData("text/html", NucleusConstants.ExtractorKeys.Html)]
    [InlineData("text/markdown", NucleusConstants.ExtractorKeys.Markdown)]
    [InlineData("text/csv", NucleusConstants.ExtractorKeys.PlainText)] // CSV maps to PlainText
    [InlineData("application/xml", NucleusConstants.ExtractorKeys.Xml)]
    [InlineData("application/json", NucleusConstants.ExtractorKeys.Json)]
    [InlineData("application/pdf", NucleusConstants.ExtractorKeys.Pdf)]
    [InlineData("application/octet-stream", NucleusConstants.ExtractorKeys.DefaultBinary)]
    // Test with parameters
    [InlineData("text/plain; charset=utf-8", NucleusConstants.ExtractorKeys.PlainText)]
    [InlineData("TEXT/HTML; CHARSET=UTF-8", NucleusConstants.ExtractorKeys.Html)]
    public void GetExtractorKeyForMimeType_KnownTypes_ReturnsCorrectKey(string mimeType, string expectedKey)
    {
        // Act
        var actualKey = MimeTypeHelper.GetExtractorKeyForMimeType(mimeType);

        // Assert
        Assert.Equal(expectedKey, actualKey);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("audio/mp3")]
    [InlineData("application/zip")]
    [InlineData("unknown/type")]
    public void GetExtractorKeyForMimeType_UnknownTypes_ReturnsDefaultBinaryKey(string mimeType)
    {
        // Act
        var actualKey = MimeTypeHelper.GetExtractorKeyForMimeType(mimeType);

        // Assert
        Assert.Equal(NucleusConstants.ExtractorKeys.DefaultBinary, actualKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")] // Whitespace
    public void GetExtractorKeyForMimeType_NullOrEmptyOrWhitespace_ReturnsPlainTextKey(string? mimeType)
    {
        // Note: Current implementation returns PlainText for null/empty. This test verifies that.
        // Act
        var actualKey = MimeTypeHelper.GetExtractorKeyForMimeType(mimeType);

        // Assert
        Assert.Equal(NucleusConstants.ExtractorKeys.PlainText, actualKey);
    }

    [Theory]
    [InlineData("text/plain", true, NucleusConstants.ExtractorKeys.PlainText)]
    [InlineData("TEXT/PLAIN", true, NucleusConstants.ExtractorKeys.PlainText)] // Case insensitivity
    [InlineData("text/html", true, NucleusConstants.ExtractorKeys.Html)]
    [InlineData("text/plain; charset=utf-8", true, NucleusConstants.ExtractorKeys.PlainText)]
    [InlineData("image/jpeg", false, null)] // Unknown type
    [InlineData("application/octet-stream", true, NucleusConstants.ExtractorKeys.DefaultBinary)]
    [InlineData(null, false, null)]
    [InlineData("", false, null)]
    [InlineData("   ", false, null)] // Whitespace
    public void TryGetExtractorKeyForMimeType_VariousInputs_ReturnsExpectedResultAndKey(
        string? mimeType, bool expectedResult, string? expectedKey)
    {
        // Act
        var actualResult = MimeTypeHelper.TryGetExtractorKeyForMimeType(mimeType, out var actualKey);

        // Assert
        Assert.Equal(expectedResult, actualResult);
        Assert.Equal(expectedKey, actualKey);
    }
}
