using System.Data;
using ConferenceRoomAPI.Domain.Entities;
using ConferenceRoomAPI.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomAPI.Features.Bookings;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly IBookingPriceCalculator _priceCalculator;

    public BookingService(AppDbContext db, IBookingPriceCalculator priceCalculator)
    {
        _db = db;
        _priceCalculator = priceCalculator;
    }

    public async Task<CreateBookingResult> CreateAsync(CreateBookingRequest request, CancellationToken ct)
    {
        // Do everything in a single transaction so two requests cannot book the same slot
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var room = await _db.Rooms
            .Include(room => room.ExtraServices)
            .ThenInclude(roomService => roomService.ExtraService)
            .SingleOrDefaultAsync(room => room.Id == request.RoomId && room.IsActive, ct);

        if (room == null)
            return new CreateBookingResult(CreateBookingStatus.RoomNotFound, null, []);

        var requestedServiceIds = request.ExtraServiceIds.ToHashSet();

        var selectedServices = room.ExtraServices
            .Where(roomService => requestedServiceIds.Contains(roomService.ExtraServiceId))
            .Select(roomService => roomService.ExtraService)
            .OrderBy(service => service.Id)
            .ToArray();

        var invalidServiceIds = requestedServiceIds
            .Except(selectedServices.Select(service => service.Id))
            .Order()
            .ToArray();

        if (invalidServiceIds.Length > 0)
            return new CreateBookingResult(CreateBookingStatus.InvalidExtraServiceIds, null, invalidServiceIds);

        var overlapsExistingBooking = await _db.Bookings.AnyAsync(
            b => b.RoomId == request.RoomId &&
                 b.Date == request.Date &&
                 b.StartTime < request.EndTime &&
                 b.EndTime > request.StartTime,
            ct);

        if (overlapsExistingBooking)
            return new CreateBookingResult(CreateBookingStatus.RoomUnavailable, null, []);

        var roomPrice = _priceCalculator.CalculateRoomPrice(room.HourlyRate, request.StartTime, request.EndTime);

        var extraServicesPrice = selectedServices.Sum(service => service.Price);

        var totalPrice = decimal.Round(roomPrice + extraServicesPrice, 2, MidpointRounding.AwayFromZero);

        var booking = new Booking
        {
            RoomId = room.Id,
            Room = room,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            HourlyRateSnapshot = room.HourlyRate,
            TotalPrice = totalPrice
        };

        foreach (var service in selectedServices)
        {
            booking.ExtraServices.Add(new BookingExtraService
            {
                Booking = booking,
                ExtraServiceId = service.Id,
                ExtraService = service,
                PriceSnapshot = service.Price
            });
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var bookedServices = selectedServices
            .Select(service => new BookedExtraServiceResponse(service.Id, service.Name, service.Price))
            .ToArray();

        var response = new CreateBookingResponse(
            booking.Id,
            room.Id,
            booking.Date,
            booking.StartTime,
            booking.EndTime,
            booking.HourlyRateSnapshot,
            roomPrice,
            bookedServices,
            extraServicesPrice,
            booking.TotalPrice);

        return new CreateBookingResult(CreateBookingStatus.Created, response, []);
    }
}