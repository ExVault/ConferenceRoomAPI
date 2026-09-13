namespace ConferenceRoomAPI.Features.Reports;

public record ExtraServiceUsageResponse(
    DateOnly From,
    DateOnly To,
    int TotalSelections,
    decimal TotalRevenue,
    IReadOnlyCollection<ExtraServiceUsage> ExtraServices);

public record ExtraServiceUsage(
    int ExtraServiceId,
    string ExtraServiceName,
    int BookingCount,
    decimal Revenue);