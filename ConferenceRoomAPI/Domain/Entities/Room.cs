namespace ConferenceRoomAPI.Domain.Entities;

/// <summary>
/// A conference room available for booking, with its capacity, hourly rate, and current availability status.
/// </summary>
public class Room
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<RoomExtraService> ExtraServices { get; } = [];
    public ICollection<Booking> Bookings { get; } = [];
}
