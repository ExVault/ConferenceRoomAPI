namespace ConferenceRoomAPI.Common.Validation;

public class ValidationResult
{
    private readonly Dictionary<string, string[]> _errors = [];

    public IReadOnlyDictionary<string, string[]> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public void AddError(string propertyName, string error)
    {
        if (_errors.TryGetValue(propertyName, out var existingErrors))
        {
            _errors[propertyName] = [.. existingErrors, error];
            return;
        }
        _errors[propertyName] = [error];
    }
}