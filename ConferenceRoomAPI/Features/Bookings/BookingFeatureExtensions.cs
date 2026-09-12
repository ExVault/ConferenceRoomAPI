using ConferenceRoomAPI.Common.Validation;

namespace ConferenceRoomAPI.Features.Bookings;

public static class BookingFeatureExtensions
{
    public static IServiceCollection AddBookingFeatures(this IServiceCollection services)
    {
        services.AddSingleton<IRequestValidator<CreateBookingRequest>, CreateBookingRequestValidator>();
        services.AddSingleton<IBookingPriceCalculator, BookingPriceCalculator>();
        services.AddScoped<IBookingService, BookingService>();
        return services;
    }
}