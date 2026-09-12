using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public class CreateRoomRequestValidator : IRequestValidator<CreateRoomRequest>
{
    public ValidationResult Validate(CreateRoomRequest request)
    {
        var result = new ValidationResult();

        RoomRequestValidation.ValidateName(request.Name, nameof(request.Name), result);
        RoomRequestValidation.ValidateCapacity(request.Capacity, nameof(request.Capacity), result);
        RoomRequestValidation.ValidateHourlyRate(request.HourlyRate, nameof(request.HourlyRate), result);
        ExtraServiceValidation.ValidateIds(request.ExtraServiceIds, nameof(request.ExtraServiceIds), result);

        return result;
    }
}