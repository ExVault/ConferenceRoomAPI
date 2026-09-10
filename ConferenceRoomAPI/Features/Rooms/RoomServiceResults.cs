namespace ConferenceRoomAPI.Features.Rooms;

public enum CreateRoomStatus
{
    Created,
    NameConflict,
    InvalidExtraServiceIds
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

public enum UpdateRoomStatus
{
    Updated,
    NotFound,
    NameConflict,
    InvalidExtraServiceIds
}

public record UpdateRoomResult
{
    public UpdateRoomStatus Status { get; init; }
    public ICollection<int> InvalidExtraServiceIds { get; } = [];
}
