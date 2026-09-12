using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public static class RoomRequestValidation
{
    public static void ValidateName(string? name, string propertyName, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            result.AddError(propertyName, "Name is required.");
        }
        else if (name.Trim().Length > 100)
        {
            result.AddError(propertyName, "Name must not exceed 100 characters.");
        }
    }

    public static void ValidateCapacity(int capacity, string propertyName, ValidationResult result)
    {
        if (capacity <= 0)
        {
            result.AddError(propertyName, "Capacity must be greater than zero.");
        }
    }

    public static void ValidateHourlyRate(decimal hourlyRate, string propertyName, ValidationResult result)
    {
        if (hourlyRate < 0)
        {
            result.AddError(propertyName, "Hourly rate must not be negative.");
        }
    }
}
