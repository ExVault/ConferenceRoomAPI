using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public class AvailableRoomsRequestValidator : IRequestValidator<AvailableRoomsRequest>
{
    private static readonly TimeOnly OpeningTime = new(6, 0);
    private static readonly TimeOnly ClosingTime = new(23, 0);

    public ValidationResult Validate(AvailableRoomsRequest request)
    {
        var result = new ValidationResult();

        if (request.StartTime >= request.EndTime)
        {
            result.Errors[nameof(request.EndTime)] = ["End time must be greater than start time."];
        }

        if (request.StartTime < OpeningTime)
        {
            result.Errors[nameof(request.StartTime)] = ["Start time must not be earlier than 06:00."];
        }

        if (request.EndTime > ClosingTime)
        {
            result.Errors[nameof(request.EndTime)] = ["End time must not be later than 23:00."];
        }

        RoomRequestValidation.ValidateCapacity(request.MinCapacity, nameof(request.MinCapacity), result);

        return result;
    }
}