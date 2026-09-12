namespace ConferenceRoomAPI.Features.Bookings;

public record CreateBookingRequest(
    int RoomId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    IReadOnlyCollection<int> ExtraServiceIds);