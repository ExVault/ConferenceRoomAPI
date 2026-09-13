namespace ConferenceRoomAPI.Features.Reports;

public record RoomUsageResponse(
    DateOnly From,
    DateOnly To,
    int TotalBookings,
    decimal TotalBookedHours,
    decimal TotalRevenue,
    IReadOnlyCollection<RoomUsage> Rooms);

public record RoomUsage(
    int RoomId,
    string RoomName,
    int BookingCount,
    decimal BookedHours,
    decimal Revenue,
    decimal AverageBookingValue);