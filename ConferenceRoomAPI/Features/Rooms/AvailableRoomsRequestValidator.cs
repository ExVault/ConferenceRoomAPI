using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public class AvailableRoomsRequestValidator : IRequestValidator<AvailableRoomsRequest>
{
    public ValidationResult Validate(AvailableRoomsRequest request)
    {
        var result = new ValidationResult();

        if (request.StartAt.Value >= request.EndAt.Value)
        {
            result.Errors[nameof(request.EndAt)] = ["End time must be greater than start time."];
        }

        RoomRequestValidation.ValidateCapacity(request.MinCapacity, nameof(request.MinCapacity), result);

        return result;
    }
}