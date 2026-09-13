using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reports = endpoints.MapGroup("/reports").WithTags("Reports");

        reports.MapGet("/room-usage", GetRoomUsageAsync)
            .WithSummary("Get room usage for a date range")
            .Produces<RoomUsageResponse>()
            .ProducesValidationProblem();

        reports.MapGet("/extra-service-usage", GetExtraServiceUsageAsync)
            .WithSummary("Get extra service usage for a date range")
            .Produces<ExtraServiceUsageResponse>()
            .ProducesValidationProblem();

        return endpoints;
    }

    private static async Task<IResult> GetRoomUsageAsync(
        [AsParameters] ReportPeriodRequest request,
        IRequestValidator<ReportPeriodRequest> validator,
        IReportService reportService,
        CancellationToken ct)
    {
        var validation = validator.Validate(request);

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        var report = await reportService.GetRoomUsageAsync(request.From, request.To, ct);

        return TypedResults.Ok(report);
    }

    private static async Task<IResult> GetExtraServiceUsageAsync(
        [AsParameters] ReportPeriodRequest request,
        IRequestValidator<ReportPeriodRequest> validator,
        IReportService reportService,
        CancellationToken ct)
    {
        var validation = validator.Validate(request);

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        var report = await reportService.GetExtraServiceUsageAsync(request.From, request.To, ct);

        return TypedResults.Ok(report);
    }
}