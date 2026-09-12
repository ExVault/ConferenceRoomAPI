namespace ConferenceRoomAPI.Features.Bookings;

public interface IBookingPriceCalculator
{
    decimal CalculateRoomPrice(decimal hourlyRate, TimeOnly startTime, TimeOnly endTime);
}