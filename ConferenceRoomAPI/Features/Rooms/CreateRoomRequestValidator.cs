using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public class CreateRoomRequestValidator : IRequestValidator<CreateRoomRequest>
{
    public ValidationResult Validate(CreateRoomRequest request)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            result.Errors[nameof(request.Name)] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > 100)
        {
            result.Errors[nameof(request.Name)] = ["Name must not exceed 100 characters."];
        }

        if (request.Capacity <= 0)
        {
            result.Errors[nameof(request.Capacity)] = ["Capacity must be greater than zero."];
        }

        if (request.HourlyRate < 0)
        {
            result.Errors[nameof(request.HourlyRate)] = ["Hourly rate must not be negative."];
        }

        if (request.ExtraServiceIds.Any(id => id <= 0))
        {
            result.Errors[nameof(request.ExtraServiceIds)] = ["Extra service IDs must be greater than zero."];
        }
        else if (request.ExtraServiceIds.Count != request.ExtraServiceIds.Distinct().Count())
        {
            result.Errors[nameof(request.ExtraServiceIds)] = ["Extra service IDs must not contain duplicates."];
        }

        return result;
    }
}
