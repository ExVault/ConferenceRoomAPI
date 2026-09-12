namespace ConferenceRoomAPI.Domain;

public record BookingRules(
    TimeSpan MinimumBookingDuration,
    TimeSpan BookingTimeStep,
    IReadOnlyList<BookingPricingPeriod> PricingPeriods)
{
    public TimeOnly OpeningTime { get; } = PricingPeriods[0].StartTime;
    public TimeOnly ClosingTime { get; } = PricingPeriods[^1].EndTime;
}

public record BookingPricingPeriod(TimeOnly StartTime, TimeOnly EndTime, decimal RateMultiplier);