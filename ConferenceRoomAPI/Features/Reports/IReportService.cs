namespace ConferenceRoomAPI.Features.Reports;

public interface IReportService
{
    Task<RoomUsageResponse> GetRoomUsageAsync(DateOnly from, DateOnly to, CancellationToken ct);
    Task<ExtraServiceUsageResponse> GetExtraServiceUsageAsync(DateOnly from, DateOnly to, CancellationToken ct);
}