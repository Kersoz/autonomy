namespace Autonomy.Domain.Enums;

public enum DeliveryStatus
{
    Created = 1,
    Assigned = 2,
    CourierOnTheWayToPickup = 3,
    ArrivedAtMerchant = 4,
    PickedUp = 5,
    CourierOnTheWayToCustomer = 6,
    Delivered = 7,
    Cancelled = 8,
    Failed = 9
}