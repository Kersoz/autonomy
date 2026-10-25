using Autonomy.Domain.Common;
using Autonomy.Domain.Enums;

namespace Autonomy.Domain.Entities;

public class Fleet : BaseEntity
{
    public string Name { get; set; } = null!;

    public AccountStatus AccountStatus { get; set; }

    public string DefaultCurrency { get; set; } = "TRY";

    public string BrandName { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? PrimaryColor { get; set; }

    public string? SecondaryColor { get; set; }

    public string? SupportPhone { get; set; }

    public string? SupportEmail { get; set; }

    public ICollection<Merchant> Merchants { get; set; }
        = new List<Merchant>();

    public ICollection<Courier> Couriers { get; set; }
        = new List<Courier>();

    public ICollection<Delivery> Deliveries { get; set; }
        = new List<Delivery>();
}
