using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class CartRepository : EfRepositoryBase<Cart, int, ApplicationDbContext>, ICartRepository
{
    public CartRepository(ApplicationDbContext context) : base(context) { }
}
