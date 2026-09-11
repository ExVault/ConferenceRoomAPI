using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public static class RoomRequestValidation
{
    public static void ValidateName(string? name, string propertyName, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            result.Errors[propertyName] = ["Name is required."];
        }
        else if (name.Trim().Length > 100)
        {
            result.Errors[propertyName] = ["Name must not exceed 100 characters."];
        }
    }

    public static void ValidateCapacity(int capacity, string propertyName, ValidationResult result)
    {
        if (capacity <= 0)
        {
            result.Errors[propertyName] = ["Capacity must be greater than zero."];
        }
    }

    public static void ValidateHourlyRate(decimal hourlyRate, string propertyName, ValidationResult result)
    {
        if (hourlyRate < 0)
        {
            result.Errors[propertyName] = ["Hourly rate must not be negative."];
        }
    }

    public static void ValidateServiceIds(ICollection<int> serviceIds, string propertyName, ValidationResult result)
    {
        if (serviceIds.Any(id => id <= 0))
        {
            result.Errors[propertyName] = ["Extra service IDs must be greater than zero."];
        }
        else if (serviceIds.Count != serviceIds.Distinct().Count())
        {
            result.Errors[propertyName] = ["Extra service IDs must not contain duplicates."];
        }
    }
}