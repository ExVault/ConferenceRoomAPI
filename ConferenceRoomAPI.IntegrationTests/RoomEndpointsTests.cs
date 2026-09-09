using System.Net;
using System.Net.Http.Json;
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
        
        var result = await response.Content.ReadFromJsonAsync<RoomCreatedResponse>();
        
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
    public async Task CreateRoom_WithUnknownService_ReturnsValidationProblem()
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
            $"Unknown extra service IDs: {int.MaxValue}",
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
}
