namespace ConferenceRoomAPI.Domain.Entities;

/// <summary>
///     A room reservation for a specific time period, including the rates and total price recorded at booking time.
/// </summary>
public class Booking
{
    public int Id { get; init; }
    public int RoomId { get; init; }
    public Room Room { get; init; } = null!;
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public decimal HourlyRateSnapshot { get; init; }
    public decimal TotalPrice { get; init; }
    public ICollection<BookingExtraService> ExtraServices { get; } = [];
}