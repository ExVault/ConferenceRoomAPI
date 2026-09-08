namespace ConferenceRoomAPI.Domain.Entities;

/// <summary>
/// Links an extra service to a booking and keeps the service price recorded at booking time.
/// </summary>
public class BookingExtraService
{
    public int BookingId { get; init; }
    public Booking Booking { get; init; } = null!;
    public int ExtraServiceId { get; init; }
    public ExtraService ExtraService { get; init; } = null!;
    public decimal PriceSnapshot { get; init; }
}
