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
            .Produces<RoomCreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        rooms.MapDelete("/{id:int}", DeleteRoomAsync)
            .WithSummary("Delete a conference room")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
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
                new RoomCreatedResponse(result.RoomId)),
            
            CreateRoomStatus.NameConflict => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A room with this name already exists"),
            
            CreateRoomStatus.ExtraServicesNotFound => TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.ExtraServiceIds)] =
                    [$"Unknown extra service IDs: {string.Join(", ", result.InvalidExtraServiceIds)}"]
                }),
            _ => throw new InvalidOperationException($"Unknown room creation status: {result.Status}")
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
}
