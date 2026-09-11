using ConferenceRoomAPI.Common.Types;

namespace ConferenceRoomAPI.Features.Rooms;

public record AvailableRoomsRequest(
    StrictDateTimeOffset StartAt,
    StrictDateTimeOffset EndAt,
    int MinCapacity);