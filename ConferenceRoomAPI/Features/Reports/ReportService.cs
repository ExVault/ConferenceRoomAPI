using ConferenceRoomAPI.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomAPI.Features.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RoomUsageResponse> GetRoomUsageAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rooms = await _db.Rooms
            .AsNoTracking()
            .Select(room => new { room.Id, room.Name })
            .ToListAsync(ct);

        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(booking => booking.Date >= from && booking.Date <= to)
            .Select(booking => new
            {
                booking.RoomId,
                booking.StartTime,
                booking.EndTime,
                booking.TotalPrice
            })
            .ToListAsync(ct);

        var usageByRoom = bookings
            .GroupBy(booking => booking.RoomId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    BookingCount = group.Count(),
                    BookedHours = group.Sum(booking => GetDurationHours(booking.StartTime, booking.EndTime)),
                    Revenue = group.Sum(booking => booking.TotalPrice)
                });

        var roomUsage = rooms.Select(room =>
        {
            if (!usageByRoom.TryGetValue(room.Id, out var usage))
                return new RoomUsage(room.Id, room.Name, 0, 0, 0, 0);

            var averageBookingValue = decimal.Round(
                usage.Revenue / usage.BookingCount,
                2,
                MidpointRounding.AwayFromZero);

            return new RoomUsage(
                room.Id,
                room.Name,
                usage.BookingCount,
                usage.BookedHours,
                usage.Revenue,
                averageBookingValue);
        })
        .OrderByDescending(usage => usage.BookingCount)
        .ThenBy(usage => usage.RoomId)
        .ToArray();

        return new RoomUsageResponse(
            from,
            to,
            bookings.Count,
            roomUsage.Sum(usage => usage.BookedHours),
            roomUsage.Sum(usage => usage.Revenue),
            roomUsage);
    }

    public async Task<ExtraServiceUsageResponse> GetExtraServiceUsageAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct)
    {
        var extraServices = await _db.ExtraServices
            .AsNoTracking()
            .Select(service => new { service.Id, service.Name })
            .ToListAsync(ct);

        var selections = await _db.BookingExtraServices
            .AsNoTracking()
            .Where(selection => selection.Booking.Date >= from && selection.Booking.Date <= to)
            .Select(selection => new
            {
                selection.ExtraServiceId,
                selection.PriceSnapshot
            })
            .ToListAsync(ct);

        var usageByService = selections
            .GroupBy(selection => selection.ExtraServiceId)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    BookingCount = group.Count(),
                    Revenue = group.Sum(selection => selection.PriceSnapshot)
                });

        var extraServiceUsage = extraServices.Select(service =>
        {
            if (!usageByService.TryGetValue(service.Id, out var usage))
                return new ExtraServiceUsage(service.Id, service.Name, 0, 0);

            return new ExtraServiceUsage(service.Id, service.Name, usage.BookingCount, usage.Revenue);
        })
        .OrderByDescending(usage => usage.BookingCount)
        .ThenBy(usage => usage.ExtraServiceId)
        .ToArray();

        return new ExtraServiceUsageResponse(
            from,
            to,
            selections.Count,
            extraServiceUsage.Sum(usage => usage.Revenue),
            extraServiceUsage);
    }

    private static decimal GetDurationHours(TimeOnly startTime, TimeOnly endTime)
    {
        return (decimal)(endTime.Ticks - startTime.Ticks) / TimeSpan.TicksPerHour;
    }
}
