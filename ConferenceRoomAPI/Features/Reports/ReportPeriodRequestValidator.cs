using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Reports;

public class ReportPeriodRequestValidator : IRequestValidator<ReportPeriodRequest>
{
    public ValidationResult Validate(ReportPeriodRequest request)
    {
        var result = new ValidationResult();

        if (request.From > request.To)
        {
            result.AddError(nameof(request.To), "End date must not be earlier than start date.");
        }

        return result;
    }
}