using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Features.Bookings;

public class BookingPriceCalculator : IBookingPriceCalculator
{
    private readonly BookingRules _rules;

    public BookingPriceCalculator(BookingRules rules)
    {
        _rules = rules;
    }

    public decimal CalculateRoomPrice(decimal hourlyRate, TimeOnly startTime, TimeOnly endTime)
    {
        var price = 0m;

        foreach (var period in _rules.PricingPeriods)
        {
            var segmentStart = startTime > period.StartTime ? startTime : period.StartTime;
            var segmentEnd = endTime < period.EndTime ? endTime : period.EndTime;

            if (segmentStart >= segmentEnd)
                continue;

            var hours = (decimal)(segmentEnd - segmentStart).TotalMinutes / 60m;
            price += hourlyRate * period.RateMultiplier * hours;
        }

        return decimal.Round(price, 2, MidpointRounding.AwayFromZero);
    }
}