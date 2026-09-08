using Kasko.Business.Services;

namespace Kasko.Business.Tests.Services;

public class TurkishPlateNumberTests
{
    [Theory]
    [InlineData("34 ABC 123", "34ABC123")]
    [InlineData("34abc123", "34ABC123")]
    [InlineData("34-ABC-123", "34ABC123")]
    [InlineData(" 34 ABC 123 ", "34ABC123")]
    public void Normalize_ShouldReturnCanonicalPlate(
        string input,
        string expected)
    {
        var result =
            TurkishPlateNumber.Normalize(input);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("34 ABC 123")]
    [InlineData("06 AB 1234")]
    [InlineData("35 A 12345")]
    [InlineData("81 ABC 123")]
    public void IsValid_ShouldAcceptValidPlateFormats(
        string plate)
    {
        Assert.True(
            TurkishPlateNumber.IsValid(plate));
    }

    [Theory]
    [InlineData("00 ABC 123")]
    [InlineData("82 ABC 123")]
    [InlineData("34 ABC")]
    [InlineData("34 123 ABC")]
    [InlineData("ABC 123")]
    public void IsValid_ShouldRejectInvalidPlateFormats(
        string plate)
    {
        Assert.False(
            TurkishPlateNumber.IsValid(plate));
    }

    [Fact]
    public void FormatForDisplay_ShouldReturnFormattedPlate()
    {
        var result =
            TurkishPlateNumber.FormatForDisplay(
                "34abc123");

        Assert.Equal(
            "34 ABC 123",
            result);
    }
}