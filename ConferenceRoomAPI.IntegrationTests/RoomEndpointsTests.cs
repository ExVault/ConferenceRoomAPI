using System.Net;
using System.Net.Http.Json;
using ConferenceRoomAPI.Domain;
using ConferenceRoomAPI.Domain.Entities;
using ConferenceRoomAPI.Features.Rooms;
using ConferenceRoomAPI.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomAPI.IntegrationTests;

public class RoomEndpointsTests : IClassFixture<ConferenceRoomApiFactory>, IAsyncLifetime
{
    private readonly ConferenceRoomApiFactory _factory;
    private readonly HttpClient _client;

    public RoomEndpointsTests(ConferenceRoomApiFactory factory)
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
    public async Task CreateRoom_WithValidRequest_CreatesRoomWithServices()
    {
        var request = new CreateRoomRequest("  Зал D  ", 40, 1800m, [1, 2]);

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateRoomResponse>();

        Assert.NotNull(result);
        Assert.Equal($"/rooms/{result.Id}", response.Headers.Location?.ToString());

        await using var scope = _factory.Services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var room = await db.Rooms.Include(savedRoom => savedRoom.ExtraServices)
            .SingleAsync(savedRoom => savedRoom.Id == result.Id);

        Assert.Equal("Зал D", room.Name);
        Assert.Equal(40, room.Capacity);
        Assert.Equal(1800m, room.HourlyRate);
        Assert.True(room.IsActive);
        Assert.Equal([1, 2], room.ExtraServices.Select(service => service.ExtraServiceId).Order());
    }

    [Fact]
    public async Task CreateRoom_WithInvalidValues_ReturnsValidationProblem()
    {
        var request = new CreateRoomRequest(" ", 0, -1m, [0]);

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Name is required.", Assert.Single(problem.Errors[nameof(request.Name)]));
        Assert.Equal("Capacity must be greater than zero.", Assert.Single(problem.Errors[nameof(request.Capacity)]));
        Assert.Equal("Hourly rate must not be negative.", Assert.Single(problem.Errors[nameof(request.HourlyRate)]));
        Assert.Equal("Extra service IDs must be greater than zero.",
            Assert.Single(problem.Errors[nameof(request.ExtraServiceIds)]));
    }

    [Fact]
    public async Task CreateRoom_WithInvalidServiceId_ReturnsValidationProblem()
    {
        var request = new CreateRoomRequest("Зал з невідомою послугою", 10, 500m, [int.MaxValue]);

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal(
            $"Invalid extra service IDs: {int.MaxValue}",
            Assert.Single(problem.Errors[nameof(request.ExtraServiceIds)]));
    }

    [Fact]
    public async Task CreateRoom_WithExistingName_ReturnsConflict()
    {
        var request = new CreateRoomRequest("Зал А", 10, 500m, []);

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WithoutExtraServiceIds_ReturnsBadRequest()
    {
        var request = new
        {
            name = "Зал без списку послуг",
            capacity = 10,
            hourlyRate = 500m
        };

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WithNullExtraServiceIds_ReturnsBadRequest()
    {
        var request = new
        {
            name = "Зал з null замість списку",
            capacity = 10,
            hourlyRate = 500m,
            extraServiceIds = (int[]?)null
        };

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRoom_WithExistingRoom_DeactivatesRoom()
    {
        var response = await _client.DeleteAsync("/rooms/1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var room = await db.Rooms.SingleAsync(savedRoom => savedRoom.Id == 1);

        Assert.False(room.IsActive);
    }

    [Fact]
    public async Task DeleteRoom_WithInactiveRoom_ReturnsNoContent()
    {
        await _client.DeleteAsync("/rooms/2");

        var response = await _client.DeleteAsync("/rooms/2");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRoom_WithUnknownRoom_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/rooms/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithValidRequest_UpdatesPropertiesAndServices()
    {
        await using (var arrangeScope = _factory.Services.CreateAsyncScope())
        {
            var arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var room = await arrangeDb.Rooms.Include(savedRoom => savedRoom.ExtraServices)
                .SingleAsync(savedRoom => savedRoom.Id == 1);

            room.ExtraServices.Add(new RoomExtraService { Room = room, ExtraServiceId = 1 });
            room.ExtraServices.Add(new RoomExtraService { Room = room, ExtraServiceId = 2 });
            await arrangeDb.SaveChangesAsync();
        }

        var request = new UpdateRoomRequest(
            Name: "  Оновлений зал  ",
            Capacity: 60,
            HourlyRate: 2500m,
            ExtraServiceIdsToAdd: [2, 3],
            ExtraServiceIdsToRemove: [1]);

        var firstResponse = await _client.PatchAsJsonAsync("/rooms/1", request);
        var repeatedResponse = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeatedResponse.StatusCode);

        await using var assertScope = _factory.Services.CreateAsyncScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var updatedRoom = await assertDb.Rooms.Include(room => room.ExtraServices)
            .SingleAsync(room => room.Id == 1);

        Assert.Equal("Оновлений зал", updatedRoom.Name);
        Assert.Equal(60, updatedRoom.Capacity);
        Assert.Equal(2500m, updatedRoom.HourlyRate);
        Assert.Equal([2, 3], updatedRoom.ExtraServices.Select(service => service.ExtraServiceId).Order());
    }

    [Fact]
    public async Task UpdateRoom_WithOnlyHourlyRate_PreservesOtherProperties()
    {
        var request = new { hourlyRate = 2500m };

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.Rooms.SingleAsync(savedRoom => savedRoom.Id == 1);

        Assert.Equal("Зал А", room.Name);
        Assert.Equal(50, room.Capacity);
        Assert.Equal(2500m, room.HourlyRate);
    }

    [Fact]
    public async Task UpdateRoom_WithNullServiceCollections_TreatsThemAsNoChanges()
    {
        var request = new UpdateRoomRequest(HourlyRate: 2600m);

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var room = await db.Rooms.SingleAsync(savedRoom => savedRoom.Id == 1);

        Assert.Equal(2600m, room.HourlyRate);
    }

    [Fact]
    public async Task UpdateRoom_WithInvalidValues_ReturnsValidationProblem()
    {
        var request = new UpdateRoomRequest(
            Name: " ",
            Capacity: 0,
            HourlyRate: -1m,
            ExtraServiceIdsToAdd: [-1]);

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Name is required.", Assert.Single(problem.Errors[nameof(request.Name)]));
        Assert.Equal("Capacity must be greater than zero.", Assert.Single(problem.Errors[nameof(request.Capacity)]));
        Assert.Equal("Hourly rate must not be negative.", Assert.Single(problem.Errors[nameof(request.HourlyRate)]));

        Assert.Equal("Extra service IDs must be greater than zero.",
            Assert.Single(problem.Errors[nameof(request.ExtraServiceIdsToAdd)]));
    }

    [Fact]
    public async Task UpdateRoom_WithNoUpdates_ReturnsValidationProblem()
    {
        var response = await _client.PatchAsJsonAsync("/rooms/1", new UpdateRoomRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal("At least one room update must be provided.",
            Assert.Single(problem.Errors[nameof(UpdateRoomRequest)]));
    }

    [Fact]
    public async Task UpdateRoom_WithServiceInBothCollections_ReturnsValidationProblem()
    {
        var request = new UpdateRoomRequest(ExtraServiceIdsToAdd: [1], ExtraServiceIdsToRemove: [1]);

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal("An extra service ID cannot be both added and removed.",
            Assert.Single(problem.Errors[nameof(request.ExtraServiceIdsToRemove)]));
    }

    [Fact]
    public async Task UpdateRoom_WithInvalidServiceId_ReturnsValidationProblemWithoutUpdatingRoom()
    {
        var request = new UpdateRoomRequest(Name: "Must not be saved", ExtraServiceIdsToAdd: [int.MaxValue]);

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal($"Invalid extra service IDs to add: {int.MaxValue}",
            Assert.Single(problem.Errors[nameof(request.ExtraServiceIdsToAdd)]));

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var room = await db.Rooms.SingleAsync(savedRoom => savedRoom.Id == 1);

        Assert.Equal("Зал А", room.Name);
    }

    [Fact]
    public async Task UpdateRoom_WithExistingName_ReturnsConflict()
    {
        var request = new UpdateRoomRequest(Name: "Зал B");

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithInactiveRoom_ReturnsNotFound()
    {
        await _client.DeleteAsync("/rooms/1");

        var response = await _client.PatchAsJsonAsync(
            "/rooms/1",
            new UpdateRoomRequest(HourlyRate: 2500m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithUnknownRoom_ReturnsNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/rooms/{int.MaxValue}",
            new UpdateRoomRequest(HourlyRate: 2500m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FindAvailableRooms_FiltersByCapacityAndOverlappingBookings()
    {
        await using (var arrangeScope = _factory.Services.CreateAsyncScope())
        {
            var arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roomToConfigure = await arrangeDb.Rooms.SingleAsync(savedRoom => savedRoom.Id == 2);

            roomToConfigure.ExtraServices.Add(new RoomExtraService { Room = roomToConfigure, ExtraServiceId = 3 });
            arrangeDb.Bookings.Add(new Booking
            {
                RoomId = 1,
                Date = new DateOnly(2026, 9, 15),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                HourlyRateSnapshot = 2000m,
                TotalPrice = 4000m
            });
            await arrangeDb.SaveChangesAsync();
        }

        var response = await _client.GetAsync(
            "/rooms/available?date=2026-09-15&startTime=10:30&endTime=11:30&minCapacity=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rooms = await response.Content.ReadFromJsonAsync<AvailableRoomResponse[]>();

        Assert.NotNull(rooms);
        var room = Assert.Single(rooms);
        Assert.Equal(2, room.Id);
        Assert.Equal("Зал B", room.Name);
        Assert.Equal(100, room.Capacity);
        Assert.Equal(3500m, room.HourlyRate);

        var extraService = Assert.Single(room.ExtraServices);
        Assert.Equal(3, extraService.Id);
        Assert.Equal("Звук", extraService.Name);
        Assert.Equal(700m, extraService.Price);
    }

    [Fact]
    public async Task FindAvailableRooms_WithAdjacentBooking_ReturnsRoom()
    {
        await using (var arrangeScope = _factory.Services.CreateAsyncScope())
        {
            var arrangeDb = arrangeScope.ServiceProvider.GetRequiredService<AppDbContext>();
            arrangeDb.Bookings.Add(new Booking
            {
                RoomId = 1,
                Date = new DateOnly(2026, 9, 15),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(12, 0),
                HourlyRateSnapshot = 2000m,
                TotalPrice = 4000m
            });
            await arrangeDb.SaveChangesAsync();
        }

        var response = await _client.GetAsync(
            "/rooms/available?date=2026-09-15&startTime=12:00&endTime=14:00&minCapacity=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rooms = await response.Content.ReadFromJsonAsync<AvailableRoomResponse[]>();

        Assert.NotNull(rooms);
        Assert.Equal([1, 2], rooms.Select(room => room.Id));
    }

    [Fact]
    public async Task FindAvailableRooms_DoesNotReturnInactiveRooms()
    {
        await _client.DeleteAsync("/rooms/2");

        var response = await _client.GetAsync(
            "/rooms/available?date=2026-09-15&startTime=10:00&endTime=11:00&minCapacity=75");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rooms = await response.Content.ReadFromJsonAsync<AvailableRoomResponse[]>();

        Assert.NotNull(rooms);
        Assert.Empty(rooms);
    }

    [Fact]
    public async Task FindAvailableRooms_WithInvalidRequest_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync(
            "/rooms/available?date=2026-09-15&startTime=14:00&endTime=10:00&minCapacity=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal("End time must be greater than start time.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.EndTime)]));

        Assert.Equal("Capacity must be greater than zero.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.MinCapacity)]));
    }

    [Fact]
    public async Task FindAvailableRooms_WithoutRequiredParameters_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/rooms/available");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FindAvailableRooms_OutsideOpeningHours_ReturnsValidationProblem()
    {
        var rules = _factory.Services.GetRequiredService<BookingRules>();
        var response = await _client.GetAsync(
            "/rooms/available?date=2026-09-15&startTime=05:30&endTime=23:30&minCapacity=50");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal($"Start time must not be earlier than {rules.OpeningTime:HH:mm}.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.StartTime)]));

        Assert.Equal($"End time must not be later than {rules.ClosingTime:HH:mm}.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.EndTime)]));
    }

    [Fact]
    public async Task FindAvailableRooms_WithRangeShorterThanMinimumBookingDuration_ReturnsValidationProblem()
    {
        var rules = _factory.Services.GetRequiredService<BookingRules>();

        var startTime = new TimeOnly(10, 0);
        var endTime = startTime.Add(rules.MinimumBookingDuration - TimeSpan.FromMinutes(1));

        var response = await _client.GetAsync(
            $"/rooms/available?date=2026-09-15&startTime={startTime:HH:mm}&endTime={endTime:HH:mm}&minCapacity=50");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Contains(
            $"Booking duration must be at least {rules.MinimumBookingDuration.TotalMinutes} minutes.",
            problem.Errors[nameof(AvailableRoomsRequest.EndTime)]);
    }

    [Fact]
    public async Task FindAvailableRooms_WithTimesOutsideBookingTimeStep_ReturnsValidationProblem()
    {
        var rules = _factory.Services.GetRequiredService<BookingRules>();

        var startTime = rules.OpeningTime.Add(TimeSpan.FromMinutes(1));
        var endTime = startTime.Add(rules.MinimumBookingDuration);

        var response = await _client.GetAsync(
            $"/rooms/available?date=2026-09-15&startTime={startTime:HH:mm}&endTime={endTime:HH:mm}&minCapacity=50");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal($"Start time must align with {rules.BookingTimeStep.TotalMinutes}-minute intervals.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.StartTime)]));

        Assert.Equal($"End time must align with {rules.BookingTimeStep.TotalMinutes}-minute intervals.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.EndTime)]));
    }
}
