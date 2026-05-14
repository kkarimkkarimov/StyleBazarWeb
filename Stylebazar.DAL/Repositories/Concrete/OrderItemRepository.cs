using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class OrderItemRepository : EfRepositoryBase<OrderItem, int, ApplicationDbContext>, IOrderItemRepository
{
    public OrderItemRepository(ApplicationDbContext context) : base(context) { }
}
