using Autonomy.Application.Abstractions.Persistence;
using Autonomy.Domain.Entities;
using Autonomy.Persistence.Context;

namespace Autonomy.Persistence.Repositories;

public class OrderRepository : GenericRepository<Delivery>, IOrderRepository
{
    public OrderRepository(AutonomyDbContext context)
        : base(context)
    {
    }
}