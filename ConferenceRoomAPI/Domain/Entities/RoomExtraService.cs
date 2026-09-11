namespace ConferenceRoomAPI.Domain.Entities;

/// <summary>
///     Links a room to an extra service that can be selected when booking that room.
/// </summary>
public class RoomExtraService
{
    public int RoomId { get; init; }
    public Room Room { get; init; } = null!;
    public int ExtraServiceId { get; init; }
    public ExtraService ExtraService { get; init; } = null!;
}