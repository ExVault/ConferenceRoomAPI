using ConferenceRoomAPI.Common.Validation;
using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Features.Rooms;

public class AvailableRoomsRequestValidator : IRequestValidator<AvailableRoomsRequest>
{
    private readonly BookingRules _rules;

    public AvailableRoomsRequestValidator(BookingRules rules)
    {
        _rules = rules;
    }

    public ValidationResult Validate(AvailableRoomsRequest request)
    {
        var result = new ValidationResult();

        BookingTimeValidation.Validate(
            _rules,
            request.StartTime,
            request.EndTime,
            nameof(request.StartTime),
            nameof(request.EndTime),
            result);

        RoomRequestValidation.ValidateCapacity(request.MinCapacity, nameof(request.MinCapacity), result);

        return result;
    }
}