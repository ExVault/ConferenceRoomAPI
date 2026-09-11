using System.Globalization;
using ConferenceRoomAPI.Common.Types;

namespace ConferenceRoomAPI.UnitTests.Common.Types;

public class StrictDateTimeOffsetTests
{
    [Theory]
    [InlineData("2024-01-15T10:30:00+05:00", 300)]
    [InlineData("2024-01-15T10:30:00.1+05:00", 300)]
    [InlineData("2024-01-15T10:30:00.1234567-03:30", -210)]
    [InlineData("2024-01-15T10:30:00Z", 0)]
    [InlineData("2024-01-15T10:30:00.123Z", 0)]
    public void TryParse_WithSupportedFormat_ReturnsParsedValue(string value, int expectedOffsetMinutes)
    {
        var parsed = StrictDateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            out var result);

        Assert.True(parsed);
        Assert.Equal(TimeSpan.FromMinutes(expectedOffsetMinutes), result.Value.Offset);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2024-01-15T10:30:00")]
    [InlineData("2024-01-15T10:30:00.12345678+05:00")]
    [InlineData("2024-01-15 10:30:00+05:00")]
    [InlineData("2024-01-15T10:30:00+05")]
    [InlineData("2024-01-15T10:30:00z")]
    public void TryParse_WithUnsupportedFormat_ReturnsFalse(string? value)
    {
        var parsed = StrictDateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            out var result);

        Assert.False(parsed);
        Assert.Equal(default, result);
    }

    [Fact]
    // Invalid formats are covered above, this checks only that Parse also reports the failure by throwing.
    public void Parse_WithUnsupportedFormat_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() =>
            StrictDateTimeOffset.Parse("2024-01-15T10:30:00", CultureInfo.InvariantCulture));
    }
}
