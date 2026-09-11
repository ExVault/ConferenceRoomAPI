using ConferenceRoomAPI.Common.Validation;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ConferenceRoomAPI.Features.Rooms;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rooms = endpoints.MapGroup("/rooms").WithTags("Rooms");

        rooms.MapPost("", CreateRoomAsync)
            .WithSummary("Add a conference room")
            .Produces<CreateRoomResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        rooms.MapGet("/available", FindAvailableRoomsAsync)
            .WithSummary("Find available conference rooms")
            .Produces<AvailableRoomResponse[]>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        rooms.MapPatch("/{id:int}", UpdateRoomAsync)
            .WithSummary("Update a conference room")
            .WithDescription(
                "Updates supplied room properties. " +
                "Extra service IDs use set semantics through the add and remove collections.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        rooms.MapDelete("/{id:int}", DeleteRoomAsync)
            .WithSummary("Delete a conference room")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> FindAvailableRoomsAsync(
        [AsParameters] AvailableRoomsRequest request,
        IRequestValidator<AvailableRoomsRequest> validator,
        IRoomService roomService,
        CancellationToken ct)
    {
        var validation = validator.Validate(request);

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        // Normalize start/end provided by client
        var startUtc = request.StartAt.Value.UtcDateTime;
        var endUtc = request.EndAt.Value.UtcDateTime;

        var rooms = await roomService.FindAvailableAsync(
            startUtc,
            endUtc,
            request.MinCapacity,
            ct);

        return TypedResults.Ok(rooms);
    }

    private static async Task<IResult> CreateRoomAsync(
        CreateRoomRequest request,
        IRequestValidator<CreateRoomRequest> validator,
        IRoomService roomService,
        CancellationToken ct)
    {
        var validation = validator.Validate(request);

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        var result = await roomService.CreateAsync(request, ct);

        return result.Status switch
        {
            CreateRoomStatus.Created => TypedResults.Created(
                $"/rooms/{result.RoomId}",
                new CreateRoomResponse(result.RoomId)),

            CreateRoomStatus.NameConflict => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A room with this name already exists"),

            CreateRoomStatus.InvalidExtraServiceIds => TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.ExtraServiceIds)] =
                        [$"Invalid extra service IDs: {string.Join(", ", result.InvalidExtraServiceIds)}"]
                }),

            _ => throw new InvalidOperationException($"Unhandled room creation status: {result.Status}")
        };
    }

    private static async Task<Results<NoContent, NotFound>> DeleteRoomAsync(
        int id,
        IRoomService roomService,
        CancellationToken ct)
    {
        var result = await roomService.DeleteAsync(id, ct);

        return result == DeleteRoomResult.Deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<IResult> UpdateRoomAsync(
        int id,
        UpdateRoomRequest request,
        IRequestValidator<UpdateRoomRequest> validator,
        IRoomService roomService,
        CancellationToken ct)
    {
        var validation = validator.Validate(request);

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        var result = await roomService.UpdateAsync(id, request, ct);

        return result.Status switch
        {
            UpdateRoomStatus.Updated => TypedResults.NoContent(),

            UpdateRoomStatus.NotFound => TypedResults.NotFound(),

            UpdateRoomStatus.NameConflict => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A room with this name already exists"),

            UpdateRoomStatus.InvalidExtraServiceIds => CreateInvalidExtraServicesProblem(request, result),

            _ => throw new InvalidOperationException($"Unhandled room update status: {result.Status}")
        };
    }

    private static IResult CreateInvalidExtraServicesProblem(UpdateRoomRequest request, UpdateRoomResult result)
    {
        var invalidServiceIds = result.InvalidExtraServiceIds.ToHashSet();
        var invalidIdsToAdd = request.ExtraServiceIdsToAdd.Where(invalidServiceIds.Contains).Order().ToArray();
        var invalidIdsToRemove = request.ExtraServiceIdsToRemove.Where(invalidServiceIds.Contains).Order().ToArray();

        var errors = new Dictionary<string, string[]>();

        if (invalidIdsToAdd.Length > 0)
        {
            errors[nameof(request.ExtraServiceIdsToAdd)] =
                [$"Invalid extra service IDs to add: {string.Join(", ", invalidIdsToAdd)}"];
        }

        if (invalidIdsToRemove.Length > 0)
        {
            errors[nameof(request.ExtraServiceIdsToRemove)] =
                [$"Invalid extra service IDs to remove: {string.Join(", ", invalidIdsToRemove)}"];
        }

        return TypedResults.ValidationProblem(errors);
    }
}