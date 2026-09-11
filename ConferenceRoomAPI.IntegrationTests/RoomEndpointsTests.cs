using System.Net;
using System.Net.Http.Json;
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
        var request = new CreateRoomRequest
        {
            Name = "  Зал D  ",
            Capacity = 40,
            HourlyRate = 1800m
        };

        request.ExtraServiceIds.Add(1);
        request.ExtraServiceIds.Add(2);

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
        var request = new CreateRoomRequest
        {
            Name = " ",
            Capacity = 0,
            HourlyRate = -1m
        };
        request.ExtraServiceIds.Add(0);

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
        var request = new CreateRoomRequest
        {
            Name = "Зал з невідомою послугою",
            Capacity = 10,
            HourlyRate = 500m
        };
        request.ExtraServiceIds.Add(int.MaxValue);

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
        var request = new CreateRoomRequest
        {
            Name = "Зал А",
            Capacity = 10,
            HourlyRate = 500m
        };

        var response = await _client.PostAsJsonAsync("/rooms", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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

        var request = new UpdateRoomRequest
        {
            Name = "  Оновлений зал  ",
            Capacity = 60,
            HourlyRate = 2500m
        };

        request.ExtraServiceIdsToAdd.Add(2);
        request.ExtraServiceIdsToAdd.Add(3);
        request.ExtraServiceIdsToRemove.Add(1);

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
        var request = new UpdateRoomRequest { HourlyRate = 2500m };

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
    public async Task UpdateRoom_WithInvalidValues_ReturnsValidationProblem()
    {
        var request = new UpdateRoomRequest
        {
            Name = " ",
            Capacity = 0,
            HourlyRate = -1m
        };
        request.ExtraServiceIdsToAdd.Add(-1);

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
        var request = new UpdateRoomRequest();
        request.ExtraServiceIdsToAdd.Add(1);
        request.ExtraServiceIdsToRemove.Add(1);

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
        var request = new UpdateRoomRequest { Name = "Must not be saved" };
        request.ExtraServiceIdsToAdd.Add(int.MaxValue);

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
        var request = new UpdateRoomRequest { Name = "Зал B" };

        var response = await _client.PatchAsJsonAsync("/rooms/1", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithInactiveRoom_ReturnsNotFound()
    {
        await _client.DeleteAsync("/rooms/1");

        var response = await _client.PatchAsJsonAsync(
            "/rooms/1",
            new UpdateRoomRequest { HourlyRate = 2500m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRoom_WithUnknownRoom_ReturnsNotFound()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/rooms/{int.MaxValue}",
            new UpdateRoomRequest { HourlyRate = 2500m });

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
                StartUtc = new DateTime(2026, 9, 15, 7, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc),
                HourlyRateSnapshot = 2000m,
                TotalPrice = 4000m
            });
            await arrangeDb.SaveChangesAsync();
        }

        var response = await _client.GetAsync(
            "/rooms/available?startAt=2026-09-15T10:30:00%2B03:00&endAt=2026-09-15T11:30:00%2B03:00&minCapacity=50");

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
                StartUtc = new DateTime(2026, 9, 15, 7, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc),
                HourlyRateSnapshot = 2000m,
                TotalPrice = 4000m
            });
            await arrangeDb.SaveChangesAsync();
        }

        var response = await _client.GetAsync(
            "/rooms/available?startAt=2026-09-15T12:00:00%2B03:00&endAt=2026-09-15T14:00:00%2B03:00&minCapacity=50");

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
            "/rooms/available?startAt=2026-09-15T10:00:00Z&endAt=2026-09-15T11:00:00Z&minCapacity=75");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rooms = await response.Content.ReadFromJsonAsync<AvailableRoomResponse[]>();

        Assert.NotNull(rooms);
        Assert.Empty(rooms);
    }

    [Fact]
    public async Task FindAvailableRooms_WithInvalidRequest_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync(
            "/rooms/available?startAt=2026-09-15T14:00:00Z&endAt=2026-09-15T10:00:00Z&minCapacity=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal("End time must be greater than start time.",
            Assert.Single(problem.Errors[nameof(AvailableRoomsRequest.EndAt)]));

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
    public async Task FindAvailableRooms_WithoutExplicitOffsets_ReturnsBadRequest()
    {
        var response = await _client.GetAsync(
            "/rooms/available?startAt=2026-09-15T10:00:00&endAt=2026-09-15T11:00:00&minCapacity=50");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}