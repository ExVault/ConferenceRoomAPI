namespace ConferenceRoomAPI.Features.Bookings;

public interface IBookingService
{
    Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken ct);
}