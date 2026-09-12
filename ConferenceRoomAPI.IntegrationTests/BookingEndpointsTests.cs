using System.Net;
using System.Net.Http.Json;
using ConferenceRoomAPI.Domain;
using ConferenceRoomAPI.Domain.Entities;
using ConferenceRoomAPI.Features.Bookings;
using ConferenceRoomAPI.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomAPI.IntegrationTests;

public class BookingEndpointsTests : IClassFixture<ConferenceRoomApiFactory>, IAsyncLifetime
{
    private readonly ConferenceRoomApiFactory _factory;
    private readonly HttpClient _client;

    public BookingEndpointsTests(ConferenceRoomApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateBooking_WithValidRequest_CreatesBookingWithPriceSnapshots()
    {
        await AddRoomServicesAsync(1, 1, 2);
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(11, 0),
            new TimeOnly(13, 0),
            [1, 2]);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateBookingResponse>();

        Assert.NotNull(result);
        Assert.Equal($"/bookings/{result.Id}", response.Headers.Location?.ToString());
        Assert.Equal(1, result.RoomId);
        Assert.Equal(request.Date, result.Date);
        Assert.Equal(request.StartTime, result.StartTime);
        Assert.Equal(request.EndTime, result.EndTime);
        Assert.Equal(2000m, result.HourlyRate);
        Assert.Equal(4300m, result.RoomPrice);
        Assert.Equal(800m, result.ExtraServicesPrice);
        Assert.Equal(5100m, result.TotalPrice);
        Assert.Equal([1, 2], result.ExtraServices.Select(service => service.Id));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await db.Bookings.Include(savedBooking => savedBooking.ExtraServices)
            .SingleAsync(savedBooking => savedBooking.Id == result.Id);

        Assert.Equal(2000m, booking.HourlyRateSnapshot);
        Assert.Equal(5100m, booking.TotalPrice);
        Assert.Equal([500m, 300m], booking.ExtraServices
            .OrderBy(service => service.ExtraServiceId)
            .Select(service => service.PriceSnapshot));
    }

    [Fact]
    public async Task CreateBooking_WithServiceNotOfferedByRoom_ReturnsValidationProblem()
    {
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            [1]);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Extra services are not available for this room: 1",
            Assert.Single(problem.Errors[nameof(request.ExtraServiceIds)]));
    }

    [Fact]
    public async Task CreateBooking_WhenTimeOverlapsExistingBooking_ReturnsConflict()
    {
        await AddBookingAsync(1, new DateOnly(2026, 9, 15), new TimeOnly(10, 0), new TimeOnly(12, 0));
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(11, 0),
            new TimeOnly(13, 0),
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WhenTimeIsAdjacentToExistingBooking_CreatesBooking()
    {
        await AddBookingAsync(1, new DateOnly(2026, 9, 15), new TimeOnly(10, 0), new TimeOnly(12, 0));
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(12, 0),
            new TimeOnly(14, 0),
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WhenExistingBookingIsOnDifferentDate_CreatesBooking()
    {
        await AddBookingAsync(1, new DateOnly(2026, 9, 14), new TimeOnly(10, 0), new TimeOnly(12, 0));
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(10, 0),
            new TimeOnly(12, 0),
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithInactiveRoom_ReturnsNotFound()
    {
        await _client.DeleteAsync("/rooms/1");
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithUnknownRoom_ReturnsNotFound()
    {
        var request = new CreateBookingRequest(
            int.MaxValue,
            new DateOnly(2026, 9, 15),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithInvalidRequest_ReturnsValidationProblem()
    {
        var request = new CreateBookingRequest(
            0,
            new DateOnly(2026, 9, 15),
            new TimeOnly(5, 30),
            new TimeOnly(23, 30),
            [1, 1]);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey(nameof(request.RoomId)));
        Assert.True(problem.Errors.ContainsKey(nameof(request.StartTime)));
        Assert.True(problem.Errors.ContainsKey(nameof(request.EndTime)));
        Assert.True(problem.Errors.ContainsKey(nameof(request.ExtraServiceIds)));
    }

    [Fact]
    public async Task CreateBooking_WithSeconds_ReturnsValidationProblem()
    {
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            new TimeOnly(10, 0, 30),
            new TimeOnly(11, 0),
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Start time must use whole minutes.",
            Assert.Single(problem.Errors[nameof(request.StartTime)]));
    }

    [Fact]
    public async Task CreateBooking_WithTimesOutsideBookingTimeStep_ReturnsValidationProblem()
    {
        var rules = _factory.Services.GetRequiredService<BookingRules>();
        var startTime = rules.OpeningTime.Add(TimeSpan.FromMinutes(1));
        var endTime = startTime.Add(rules.MinimumBookingDuration);
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            startTime,
            endTime,
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal($"Start time must align with {rules.BookingTimeStep.TotalMinutes}-minute intervals.",
            Assert.Single(problem.Errors[nameof(request.StartTime)]));
        Assert.Equal($"End time must align with {rules.BookingTimeStep.TotalMinutes}-minute intervals.",
            Assert.Single(problem.Errors[nameof(request.EndTime)]));
    }

    [Fact]
    public async Task CreateBooking_ShorterThanMinimumBookingDuration_ReturnsValidationProblem()
    {
        var rules = _factory.Services.GetRequiredService<BookingRules>();
        var startTime = new TimeOnly(10, 0);
        var endTime = startTime.Add(rules.MinimumBookingDuration - TimeSpan.FromMinutes(1));
        var request = new CreateBookingRequest(
            1,
            new DateOnly(2026, 9, 15),
            startTime,
            endTime,
            []);

        var response = await _client.PostAsJsonAsync("/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains(
            $"Booking duration must be at least {rules.MinimumBookingDuration.TotalMinutes} minutes.",
            problem.Errors[nameof(request.EndTime)]);
    }

    private async Task AddRoomServicesAsync(int roomId, params int[] serviceIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.Rooms.SingleAsync(savedRoom => savedRoom.Id == roomId);

        foreach (var serviceId in serviceIds)
        {
            room.ExtraServices.Add(new RoomExtraService { Room = room, ExtraServiceId = serviceId });
        }

        await db.SaveChangesAsync();
    }

    private async Task AddBookingAsync(int roomId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Bookings.Add(new Booking
        {
            RoomId = roomId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            HourlyRateSnapshot = 2000m,
            TotalPrice = 4000m
        });
        await db.SaveChangesAsync();
    }
}
