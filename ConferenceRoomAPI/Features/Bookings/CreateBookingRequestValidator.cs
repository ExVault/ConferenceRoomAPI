using ConferenceRoomAPI.Common.Validation;
using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Features.Bookings;

public class CreateBookingRequestValidator : IRequestValidator<CreateBookingRequest>
{
    private readonly BookingRules _rules;

    public CreateBookingRequestValidator(BookingRules rules)
    {
        _rules = rules;
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

        ExtraServiceValidation.ValidateIds(request.ExtraServiceIds, nameof(request.ExtraServiceIds), result);

        return result;
    }
}
