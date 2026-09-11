namespace ConferenceRoomAPI.Features.Rooms;

public record UpdateRoomRequest(
    string? Name = null,
    int? Capacity = null,
    decimal? HourlyRate = null,
    IReadOnlyCollection<int>? ExtraServiceIdsToAdd = null,
    IReadOnlyCollection<int>? ExtraServiceIdsToRemove = null);