namespace ConferenceRoomAPI.Features.Rooms;

public record AvailableRoomsRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MinCapacity);