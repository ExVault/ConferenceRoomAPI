namespace ConferenceRoomAPI.Features.Rooms;

public enum CreateRoomStatus
{
    Created,
    NameConflict,
    ExtraServicesNotFound
}

public record CreateRoomResult
{
    public CreateRoomStatus Status { get; init; }
    public int RoomId { get; init; }
    public ICollection<int> InvalidExtraServiceIds { get; } = [];
}

public enum DeleteRoomResult
{
    Deleted,
    NotFound
}
