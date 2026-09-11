namespace ConferenceRoomAPI.Features.Rooms;

public record CreateRoomRequest(
    string Name,
    int Capacity,
    decimal HourlyRate,
    IReadOnlyCollection<int> ExtraServiceIds);