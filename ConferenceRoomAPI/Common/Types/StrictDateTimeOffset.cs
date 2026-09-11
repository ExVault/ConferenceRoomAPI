using System.Globalization;

namespace ConferenceRoomAPI.Common.Types;

/// <summary>
/// DateTimeOffset parsed only from strings that include explicit UTC offset.
/// </summary>
public readonly struct StrictDateTimeOffset : IParsable<StrictDateTimeOffset>
{
    // Accepts all valid numeric offsets like +05:00, +00:00, -03:30
    private const string OffsetFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz";
    
    // Accepts when UTC is specified as Z at the end
    private const string UtcFormat = "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'";

    public DateTimeOffset Value { get; }

    public StrictDateTimeOffset(DateTimeOffset value)
    {
        Value = value;
    }

    public static StrictDateTimeOffset Parse(string value, IFormatProvider? provider)
    {
        if (TryParse(value, provider, out var result))
            return result;

        throw new FormatException("Value must be an ISO 8601 date and time with an explicit UTC offset.");
    }

    public static bool TryParse(string? value, IFormatProvider? provider, out StrictDateTimeOffset result)
    {
        var parsed = DateTimeOffset.TryParseExact(
            value,
            OffsetFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateTimeOffset);
        
        if (!parsed)
        {
            parsed = DateTimeOffset.TryParseExact(
                value,
                UtcFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out dateTimeOffset);
        }

        result = parsed ? new StrictDateTimeOffset(dateTimeOffset) : default;
        
        return parsed;
    }
}
