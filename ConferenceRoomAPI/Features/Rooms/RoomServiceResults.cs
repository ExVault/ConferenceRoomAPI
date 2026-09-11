namespace ConferenceRoomAPI.Features.Rooms;

public enum CreateRoomStatus
{
    Created,
    NameConflict,
    InvalidExtraServiceIds
}

public class CreateRoomResult
{
    public CreateRoomResult(IReadOnlyCollection<int>? invalidExtraServiceIds = null)
    {
        InvalidExtraServiceIds = invalidExtraServiceIds ?? [];
    }

    public CreateRoomStatus Status { get; init; }
    public int RoomId { get; init; }
    public IReadOnlyCollection<int> InvalidExtraServiceIds { get; } = [];
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

public class UpdateRoomResult
{
    public UpdateRoomResult(IReadOnlyCollection<int>? invalidExtraServiceIds = null)
    {
        InvalidExtraServiceIds = invalidExtraServiceIds ?? [];
    }

    public UpdateRoomStatus Status { get; init; }
    public IReadOnlyCollection<int> InvalidExtraServiceIds { get; } = [];
}