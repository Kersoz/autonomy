using Autonomy.Domain.Common;

namespace Autonomy.Domain.Entities;

public class CourierLocation : BaseEntity
{
    public Guid FleetId { get; set; }

    public Guid CourierId { get; set; }

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public decimal? AccuracyMeters { get; set; }

    public DateTime RecordedAt { get; set; }
}