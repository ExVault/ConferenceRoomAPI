using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public class UpdateRoomRequestValidator : IRequestValidator<UpdateRoomRequest>
{
    public ValidationResult Validate(UpdateRoomRequest request)
    {
        var result = new ValidationResult();

        var serviceIdsToAdd = request.ExtraServiceIdsToAdd ?? [];
        var serviceIdsToRemove = request.ExtraServiceIdsToRemove ?? [];

        if (request.Name != null)
        {
            RoomRequestValidation.ValidateName(request.Name, nameof(request.Name), result);
        }
        if (request.Capacity != null)
        {
            RoomRequestValidation.ValidateCapacity(request.Capacity.Value, nameof(request.Capacity), result);
        }
        if (request.HourlyRate != null)
        {
            RoomRequestValidation.ValidateHourlyRate(request.HourlyRate.Value, nameof(request.HourlyRate), result);
        }

        RoomRequestValidation.ValidateServiceIds(
            serviceIdsToAdd,
            nameof(request.ExtraServiceIdsToAdd),
            result);

        RoomRequestValidation.ValidateServiceIds(
            serviceIdsToRemove,
            nameof(request.ExtraServiceIdsToRemove),
            result);

        if (serviceIdsToAdd.Intersect(serviceIdsToRemove).Any())
        {
            result.Errors[nameof(request.ExtraServiceIdsToRemove)] =
                ["An extra service ID cannot be both added and removed."];
        }

        if (request.Name == null && request.Capacity == null && request.HourlyRate == null &&
            serviceIdsToAdd.Count == 0 && serviceIdsToRemove.Count == 0)
        {
            result.Errors[nameof(UpdateRoomRequest)] = ["At least one room update must be provided."];
        }

        return result;
    }
}