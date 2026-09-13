using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Reports;

public static class ReportFeatureExtensions
{
    public static IServiceCollection AddReportFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IRequestValidator<ReportPeriodRequest>, ReportPeriodRequestValidator>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}