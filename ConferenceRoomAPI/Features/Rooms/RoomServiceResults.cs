namespace ConferenceRoomAPI.Features.Rooms;

public enum CreateRoomStatus
{
    Created,
    NameConflict,
    InvalidExtraServiceIds
}

public record CreateRoomResult(
    CreateRoomStatus Status,
    int RoomId,
    IReadOnlyCollection<int> InvalidExtraServiceIds);

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

public record UpdateRoomResult(UpdateRoomStatus Status, IReadOnlyCollection<int> InvalidExtraServiceIds);