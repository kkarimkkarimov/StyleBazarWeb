using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// Order repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir.
public class OrderRepository : EfRepositoryBase<Order, int, ApplicationDbContext>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }
}
