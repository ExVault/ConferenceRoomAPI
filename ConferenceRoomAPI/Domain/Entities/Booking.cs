namespace ConferenceRoomAPI.Domain.Entities;

/// <summary>
/// A room reservation for a specific time period, including the rates and total price recorded at booking time.
/// </summary>
public class Booking
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public Room Room { get; init; } = null!;
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public decimal HourlyRateSnapshot { get; init; }
    public decimal TotalPrice { get; init; }
    public ICollection<BookingExtraService> ExtraServices { get; } = [];
}
