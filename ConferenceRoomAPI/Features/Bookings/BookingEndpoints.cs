using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Bookings;

public static class BookingEndpoints
{
    public static IEndpointRouteBuilder MapBookingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var bookings = endpoints.MapGroup("/bookings").WithTags("Bookings");

        bookings.MapPost("", CreateBookingAsync)
            .WithSummary("Book a conference room")
            .Produces<CreateBookingResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateBookingAsync(
        CreateBookingRequest request,
        IRequestValidator<CreateBookingRequest> validator,
        IBookingService bookingService,
        CancellationToken ct)
    {
        var validation = validator.Validate(request);

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.Errors);

        var result = await bookingService.CreateAsync(request, ct);

        return result.Status switch
        {
            CreateBookingStatus.Created when result.Booking != null => TypedResults.Created(
                $"/bookings/{result.Booking.Id}",
                result.Booking),

            CreateBookingStatus.RoomNotFound => TypedResults.NotFound(),

            CreateBookingStatus.RoomUnavailable => TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "The room is unavailable for the requested time"),

            CreateBookingStatus.InvalidExtraServiceIds => TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [nameof(request.ExtraServiceIds)] =
                    [
                        $"Extra services are not available for this room: " +
                        $"{string.Join(", ", result.InvalidExtraServiceIds)}"
                    ]
                }),

            _ => throw new InvalidOperationException($"Unhandled booking creation status: {result.Status}")
        };
    }
}