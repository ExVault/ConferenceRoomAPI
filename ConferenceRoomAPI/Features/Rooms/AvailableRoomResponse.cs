namespace ConferenceRoomAPI.Features.Rooms;

public record AvailableRoomResponse(
    int Id,
    string Name,
    int Capacity,
    decimal HourlyRate,
    IReadOnlyCollection<AvailableRoomExtraServiceResponse> ExtraServices);

public record AvailableRoomExtraServiceResponse(
    int Id,
    string Name,
    decimal Price);