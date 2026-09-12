using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Common.Validation;

public static class BookingTimeValidation
{
    public static void Validate(
        BookingRules rules,
        TimeOnly startTime,
        TimeOnly endTime,
        string startPropertyName,
        string endPropertyName,
        ValidationResult result)
    {
        if (startTime >= endTime)
        {
            result.AddError(endPropertyName, "End time must be greater than start time.");
        }
        else if (endTime - startTime < rules.MinimumBookingDuration)
        {
            result.AddError(
                endPropertyName,
                $"Booking duration must be at least {rules.MinimumBookingDuration.TotalMinutes} minutes.");
        }

        if (startTime < rules.OpeningTime)
        {
            result.AddError(
                startPropertyName,
                $"Start time must not be earlier than {rules.OpeningTime:HH:mm}.");
        }
        if (endTime > rules.ClosingTime)
        {
            result.AddError(
                endPropertyName,
                $"End time must not be later than {rules.ClosingTime:HH:mm}.");
        }

        if (startTime.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            result.AddError(startPropertyName, "Start time must use whole minutes.");
        }
        else if (startTime.Ticks % rules.BookingTimeStep.Ticks != 0)
        {
            result.AddError(
                startPropertyName,
                $"Start time must align with {rules.BookingTimeStep.TotalMinutes}-minute intervals.");
        }

        if (endTime.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            result.AddError(endPropertyName, "End time must use whole minutes.");
        }
        else if (endTime.Ticks % rules.BookingTimeStep.Ticks != 0)
        {
            result.AddError(
                endPropertyName,
                $"End time must align with {rules.BookingTimeStep.TotalMinutes}-minute intervals.");
        }
    }
}
