using ConferenceRoomAPI.Domain;
using ConferenceRoomAPI.Features.Bookings;

namespace ConferenceRoomAPI.UnitTests.Features.Bookings;

public class BookingPriceCalculatorTests
{
    private static readonly BookingRules Rules = new(
        TimeSpan.FromMinutes(30),
        TimeSpan.FromMinutes(10),
        [
            new(new TimeOnly(6, 0), new TimeOnly(9, 0), 0.90m),
            new(new TimeOnly(9, 0), new TimeOnly(12, 0), 1.00m),
            new(new TimeOnly(12, 0), new TimeOnly(14, 0), 1.15m),
            new(new TimeOnly(14, 0), new TimeOnly(18, 0), 1.00m),
            new(new TimeOnly(18, 0), new TimeOnly(23, 0), 0.80m)
        ]);

    private readonly BookingPriceCalculator _calculator = new(Rules);

    [Theory]
    [InlineData(6, 0, 9, 0, 5400)]
    [InlineData(9, 0, 12, 0, 6000)]
    [InlineData(12, 0, 14, 0, 4600)]
    [InlineData(14, 0, 18, 0, 8000)]
    [InlineData(18, 0, 23, 0, 8000)]
    public void CalculateRoomPrice_ForSinglePricingPeriod_ReturnsExpectedPrice(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute,
        int expectedPrice)
    {
        var price = _calculator.CalculateRoomPrice(
            2000m,
            new TimeOnly(startHour, startMinute),
            new TimeOnly(endHour, endMinute));

        Assert.Equal(expectedPrice, price);
    }

    [Fact]
    public void CalculateRoomPrice_WhenBookingCrossesPeriodBoundary_PricesEachSegment()
    {
        var price = _calculator.CalculateRoomPrice(2000m, new TimeOnly(11, 30), new TimeOnly(12, 30));

        Assert.Equal(2150m, price);
    }

    [Fact]
    public void CalculateRoomPrice_WhenBookingCrossesSeveralPeriods_PricesEachSegment()
    {
        var price = _calculator.CalculateRoomPrice(2000m, new TimeOnly(8, 0), new TimeOnly(19, 0));

        Assert.Equal(22000m, price);
    }

    [Fact]
    public void CalculateRoomPrice_WhenResultHasHalfCent_RoundsAwayFromZero()
    {
        var price = _calculator.CalculateRoomPrice(0.30m, new TimeOnly(9, 0), new TimeOnly(9, 1));

        Assert.Equal(0.01m, price);
    }
}