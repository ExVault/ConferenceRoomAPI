using ConferenceRoomAPI.Common.Validation;
using ConferenceRoomAPI.Domain;

namespace ConferenceRoomAPI.Features.Rooms;

public class AvailableRoomsRequestValidator : IRequestValidator<AvailableRoomsRequest>
{
    private readonly BookingRules _rules;
    private readonly TimeProvider _timeProvider;

    public AvailableRoomsRequestValidator(BookingRules rules, TimeProvider timeProvider)
    {
        _rules = rules;
        _timeProvider = timeProvider;
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

        BookingTimeValidation.ValidateStartTimeIsFuture(
            request.Date,
            request.StartTime,
            _timeProvider,
            nameof(request.Date),
            result);

        RoomRequestValidation.ValidateCapacity(request.MinCapacity, nameof(request.MinCapacity), result);

        return result;
    }
}