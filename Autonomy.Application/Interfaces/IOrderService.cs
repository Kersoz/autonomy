namespace Autonomy.Application.Interfaces;

public interface IOrderService
{
    //Task<Guid> CreateAsync(CreateOrderDto dto);

    Task AssignCourierAsync(Guid orderId);

    Task DeliverAsync(Guid orderId);
}