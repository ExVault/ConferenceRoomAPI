namespace ConferenceRoomAPI.Common.Validation;

public class ValidationResult
{
    public Dictionary<string, string[]> Errors { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
