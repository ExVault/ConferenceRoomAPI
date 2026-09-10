using System.Text.Json.Serialization;

namespace ConferenceRoomAPI.Features.Rooms;

public record UpdateRoomRequest
{
    public string? Name { get; init; }
    public int? Capacity { get; init; }
    public decimal? HourlyRate { get; init; }

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public ICollection<int> ExtraServiceIdsToAdd { get; } = [];

    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public ICollection<int> ExtraServiceIdsToRemove { get; } = [];
}
