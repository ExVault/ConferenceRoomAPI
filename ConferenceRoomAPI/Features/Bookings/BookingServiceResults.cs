namespace ConferenceRoomAPI.Features.Bookings;

public enum CreateBookingStatus
{
    Created,
    RoomNotFound,
    RoomUnavailable,
    InvalidExtraServiceIds
}

public record CreateBookingResult(
    CreateBookingStatus Status,
    CreateBookingResponse? Booking,
    IReadOnlyCollection<int> InvalidExtraServiceIds);