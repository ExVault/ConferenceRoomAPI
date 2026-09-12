using System.Net;
using System.Net.Http.Json;
using ConferenceRoomAPI.Domain.Entities;
using ConferenceRoomAPI.Features.Reports;
using ConferenceRoomAPI.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomAPI.IntegrationTests;

public class ReportEndpointsTests : IClassFixture<ConferenceRoomApiFactory>, IAsyncLifetime
{
    private readonly ConferenceRoomApiFactory _factory;
    private readonly HttpClient _client;

    public ReportEndpointsTests(ConferenceRoomApiFactory factory)
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
    public async Task GetRoomUsage_ReturnsUsageWithinInclusiveDateRange()
    {
        await AddBookingAsync(1, new DateOnly(2026, 9, 1), new TimeOnly(10, 0), new TimeOnly(12, 0), 4500m);
        await AddBookingAsync(1, new DateOnly(2026, 9, 2), new TimeOnly(10, 0), new TimeOnly(11, 30), 3500m);
        await AddBookingAsync(2, new DateOnly(2026, 9, 2), new TimeOnly(14, 0), new TimeOnly(15, 0), 4000m);
        await AddBookingAsync(3, new DateOnly(2026, 8, 31), new TimeOnly(10, 0), new TimeOnly(11, 0), 1500m);

        var response = await _client.GetAsync("/reports/room-usage?from=2026-09-01&to=2026-09-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<RoomUsageResponse>();

        Assert.NotNull(report);
        
        Assert.Equal(new DateOnly(2026, 9, 1), report.From);
        Assert.Equal(new DateOnly(2026, 9, 2), report.To);
        Assert.Equal(3, report.TotalBookings);
        Assert.Equal(4.5m, report.TotalBookedHours);
        Assert.Equal(12000m, report.TotalRevenue);

        Assert.Collection(
            report.Rooms,
            room =>
            {
                Assert.Equal(1, room.RoomId);
                Assert.Equal(2, room.BookingCount);
                Assert.Equal(3.5m, room.BookedHours);
                Assert.Equal(8000m, room.Revenue);
                Assert.Equal(4000m, room.AverageBookingValue);
            },
            room =>
            {
                Assert.Equal(2, room.RoomId);
                Assert.Equal(1, room.BookingCount);
                Assert.Equal(1m, room.BookedHours);
                Assert.Equal(4000m, room.Revenue);
                Assert.Equal(4000m, room.AverageBookingValue);
            },
            room =>
            {
                Assert.Equal(3, room.RoomId);
                Assert.Equal(0, room.BookingCount);
                Assert.Equal(0m, room.BookedHours);
                Assert.Equal(0m, room.Revenue);
                Assert.Equal(0m, room.AverageBookingValue);
            });
    }

    [Fact]
    public async Task GetExtraServiceUsage_ReturnsSnapshotRevenueAndUnusedServices()
    {
        await AddBookingAsync(
            1,
            new DateOnly(2026, 9, 1),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            2800m,
            (1, 500m),
            (2, 300m));
        
        await AddBookingAsync(
            2,
            new DateOnly(2026, 9, 2),
            new TimeOnly(12, 0),
            new TimeOnly(13, 0),
            4625m,
            (1, 600m));
        
        await AddBookingAsync(
            3,
            new DateOnly(2026, 9, 3),
            new TimeOnly(10, 0),
            new TimeOnly(11, 0),
            2200m,
            (3, 700m));

        var response = await _client.GetAsync(
            "/reports/extra-service-usage?from=2026-09-01&to=2026-09-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync<ExtraServiceUsageResponse>();

        Assert.NotNull(report);
        Assert.Equal(3, report.TotalSelections);
        Assert.Equal(1400m, report.TotalRevenue);

        Assert.Collection(
            report.ExtraServices,
            service =>
            {
                Assert.Equal(1, service.ExtraServiceId);
                Assert.Equal(2, service.BookingCount);
                Assert.Equal(1100m, service.Revenue);
            },
            service =>
            {
                Assert.Equal(2, service.ExtraServiceId);
                Assert.Equal(1, service.BookingCount);
                Assert.Equal(300m, service.Revenue);
            },
            service =>
            {
                Assert.Equal(3, service.ExtraServiceId);
                Assert.Equal(0, service.BookingCount);
                Assert.Equal(0m, service.Revenue);
            });
    }

    [Theory]
    [InlineData("/reports/room-usage")]
    [InlineData("/reports/extra-service-usage")]
    public async Task GetReport_WithEndDateBeforeStartDate_ReturnsValidationProblem(string path)
    {
        var response = await _client.GetAsync($"{path}?from=2026-09-02&to=2026-09-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        
        Assert.Equal("End date must not be earlier than start date.",
            Assert.Single(problem.Errors[nameof(ReportPeriodRequest.To)]));
    }

    private async Task AddBookingAsync(
        int roomId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal totalPrice,
        params (int Id, decimal Price)[] extraServices)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var booking = new Booking
        {
            RoomId = roomId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            HourlyRateSnapshot = 2000m,
            TotalPrice = totalPrice
        };

        foreach (var extraService in extraServices)
        {
            booking.ExtraServices.Add(new BookingExtraService
            {
                Booking = booking,
                ExtraServiceId = extraService.Id,
                PriceSnapshot = extraService.Price
            });
        }

        db.Bookings.Add(booking);
        
        await db.SaveChangesAsync();
    }
}
