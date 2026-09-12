using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Configuration;

public static class BookingRulesConfiguration
{
    private const string SectionName = "BookingRules";

    public static BookingRules LoadBookingRules(this IConfiguration config)
    {
        var rules = config.GetRequiredSection(SectionName).Get<BookingRules>()
                    ?? throw new InvalidOperationException($"Configuration section '{SectionName}' is invalid.");

        Validate(rules);
        return rules;
    }

    private static void Validate(BookingRules rules)
    {
        if (rules.MinimumBookingDuration <= TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException("BookingRules:MinimumBookingDuration must be at least 1 minute.");
        }

        if (rules.MinimumBookingDuration.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new InvalidOperationException("BookingRules:MinimumBookingDuration must use whole minutes.");
        }

        if (rules.BookingTimeStep < TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException("BookingRules:BookingTimeStep must be at least 1 minute.");
        }

        if (rules.BookingTimeStep.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            throw new InvalidOperationException("BookingRules:BookingTimeStep must use whole minutes.");
        }

        if (rules.MinimumBookingDuration.Ticks % rules.BookingTimeStep.Ticks != 0)
        {
            throw new InvalidOperationException(
                "BookingRules:MinimumBookingDuration must align with BookingTimeStep.");
        }

        if (rules.PricingPeriods.Count == 0)
        {
            throw new InvalidOperationException("BookingRules:PricingPeriods must contain at least one period.");
        }

        for (var i = 0; i < rules.PricingPeriods.Count; i++)
        {
            var period = rules.PricingPeriods[i];

            if (period.StartTime >= period.EndTime)
            {
                throw new InvalidOperationException($"BookingRules:PricingPeriods:{i} must end after it starts.");
            }

            if (period.StartTime.Ticks % TimeSpan.TicksPerMinute != 0 ||
                period.EndTime.Ticks % TimeSpan.TicksPerMinute != 0)
            {
                throw new InvalidOperationException(
                    $"BookingRules:PricingPeriods:{i} boundaries must use whole minutes.");
            }

            if (period.RateMultiplier < 0)
            {
                throw new InvalidOperationException(
                    $"BookingRules:PricingPeriods:{i}:RateMultiplier must not be negative.");
            }

            if (i == 0)
                continue;

            var previousPeriod = rules.PricingPeriods[i - 1];

            // Disallow gaps and overlaps like in original tech spec
            if (period.StartTime < previousPeriod.EndTime)
            {
                throw new InvalidOperationException($"BookingRules:PricingPeriods:{i} overlaps the previous period.");
            }
            if (period.StartTime > previousPeriod.EndTime)
            {
                throw new InvalidOperationException($"BookingRules:PricingPeriods:{i} leaves a gap after the previous period.");
            }
        }

        if (rules.MinimumBookingDuration > rules.ClosingTime - rules.OpeningTime)
        {
            throw new InvalidOperationException(
                "BookingRules:MinimumBookingDuration must not exceed the configured operating window.");
        }

        if (rules.OpeningTime.Ticks % rules.BookingTimeStep.Ticks != 0 ||
            rules.ClosingTime.Ticks % rules.BookingTimeStep.Ticks != 0)
        {
            throw new InvalidOperationException(
                "BookingRules opening and closing times must align with BookingTimeStep.");
        }
    }
}