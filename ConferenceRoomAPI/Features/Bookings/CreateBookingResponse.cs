namespace ConferenceRoomAPI.Features.Bookings;

public record CreateBookingResponse(
    int Id,
    int RoomId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal HourlyRate,
    decimal RoomPrice,
    IReadOnlyCollection<BookedExtraServiceResponse> ExtraServices,
    decimal ExtraServicesPrice,
    decimal TotalPrice);

public record BookedExtraServiceResponse(int Id, string Name, decimal Price);