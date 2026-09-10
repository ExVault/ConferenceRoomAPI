using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Rooms;

public static class RoomFeatureExtensions
{
    public static IServiceCollection AddRoomFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IRequestValidator<CreateRoomRequest>, CreateRoomRequestValidator>();
        services.AddSingleton<IRequestValidator<UpdateRoomRequest>, UpdateRoomRequestValidator>();
        services.AddScoped<IRoomService, RoomService>();
        return services;
    }
}
