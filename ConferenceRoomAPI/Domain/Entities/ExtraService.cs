namespace ConferenceRoomAPI.Domain.Entities;

/// <summary>
///     An optional paid service that can be offered with rooms and added to bookings.
/// </summary>
public class ExtraService
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public decimal Price { get; init; }
    public ICollection<RoomExtraService> Rooms { get; } = [];
    public ICollection<BookingExtraService> Bookings { get; } = [];
}