using System.Text.Json.Serialization;

namespace ConferenceRoomAPI.Features.Rooms;

public record CreateRoomRequest
{
    public required string Name { get; init; }
    public int Capacity { get; init; }
    public decimal HourlyRate { get; init; }

    // Populate the already initialized empty collection during deserialization
    // instead of creating a new one
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public ICollection<int> ExtraServiceIds { get; } = [];
}
