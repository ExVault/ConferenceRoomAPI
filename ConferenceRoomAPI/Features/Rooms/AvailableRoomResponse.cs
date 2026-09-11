namespace ConferenceRoomAPI.Features.Rooms;

public class AvailableRoomResponse
{
    public AvailableRoomResponse(IReadOnlyCollection<AvailableRoomExtraServiceResponse> extraServices)
    {
        ExtraServices = extraServices;
    }

    public int Id { get; init; }
    public required string Name { get; init; }
    public int Capacity { get; init; }
    public decimal HourlyRate { get; init; }
    public IReadOnlyCollection<AvailableRoomExtraServiceResponse> ExtraServices { get; } = [];
}

public record AvailableRoomExtraServiceResponse(int Id, string Name, decimal Price);