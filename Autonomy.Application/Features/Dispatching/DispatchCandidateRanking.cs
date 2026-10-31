using Autonomy.Domain.Entities;

namespace Autonomy.Application.Features.Dispatching;

internal static class DispatchCandidateRanking
{
    private static readonly TimeSpan LocationFreshness = TimeSpan.FromMinutes(2);
    private const double EstimatedSpeedKilometersPerHour = 25;

    public static bool IsFreshAndValidLocation(CourierLocation? location, DateTime now)
    {
        return location is not null
            && location.RecordedAt <= now
            && now - location.RecordedAt <= LocationFreshness
            && location.Latitude is >= -90 and <= 90
            && location.Longitude is >= -180 and <= 180;
    }

    public static (decimal DistanceKm, int EstimatedArrivalMinutes) Calculate(
        decimal courierLatitude,
        decimal courierLongitude,
        decimal pickupLatitude,
        decimal pickupLongitude)
    {
        const double earthRadiusKilometers = 6371.0088;
        var latitudeDelta = DegreesToRadians((double)(pickupLatitude - courierLatitude));
        var longitudeDelta = DegreesToRadians((double)(pickupLongitude - courierLongitude));
        var courierLatitudeRadians = DegreesToRadians((double)courierLatitude);
        var pickupLatitudeRadians = DegreesToRadians((double)pickupLatitude);
        var haversine = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(courierLatitudeRadians)
                * Math.Cos(pickupLatitudeRadians)
                * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        var distance = earthRadiusKilometers
            * 2
            * Math.Asin(Math.Sqrt(Math.Clamp(haversine, 0, 1)));
        var etaMinutes = (int)Math.Ceiling(
            distance / EstimatedSpeedKilometersPerHour * 60);

        return (
            decimal.Round((decimal)distance, 2),
            etaMinutes);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
