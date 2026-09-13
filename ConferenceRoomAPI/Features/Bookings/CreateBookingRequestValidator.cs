using ConferenceRoomAPI.Common.Validation;
using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Features.Bookings;

public class CreateBookingRequestValidator : IRequestValidator<CreateBookingRequest>
{
    private readonly BookingRules _rules;
    private readonly TimeProvider _timeProvider;

    public CreateBookingRequestValidator(BookingRules rules, TimeProvider timeProvider)
    {
        _rules = rules;
        _timeProvider = timeProvider;
    }

    public ValidationResult Validate(CreateBookingRequest request)
    {
        var result = new ValidationResult();

        if (request.RoomId <= 0)
        {
            result.AddError(nameof(request.RoomId), "Room ID must be greater than zero.");
        }

        BookingTimeValidation.Validate(
            _rules,
            request.StartTime,
            request.EndTime,
            nameof(request.StartTime),
            nameof(request.EndTime),
            result);

        BookingTimeValidation.ValidateStartTimeIsFuture(
            request.Date,
            request.StartTime,
            _timeProvider,
            nameof(request.Date),
            result);

        ExtraServiceValidation.ValidateIds(request.ExtraServiceIds, nameof(request.ExtraServiceIds), result);

        return result;
    }
}