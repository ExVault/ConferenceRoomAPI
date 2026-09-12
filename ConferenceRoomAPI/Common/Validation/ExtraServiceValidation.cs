namespace ConferenceRoomAPI.Common.Validation;

public static class ExtraServiceValidation
{
    public static void ValidateIds(
        IReadOnlyCollection<int> serviceIds,
        string propertyName,
        ValidationResult result)
    {
        if (serviceIds.Any(id => id <= 0))
        {
            result.AddError(propertyName, "Extra service IDs must be greater than zero.");
        }
        else if (serviceIds.Count != serviceIds.Distinct().Count())
        {
            result.AddError(propertyName, "Extra service IDs must not contain duplicates.");
        }
    }
}
