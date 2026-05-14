using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class CartItemRepository : EfRepositoryBase<CartItem, int, ApplicationDbContext>, ICartItemRepository
{
    public CartItemRepository(ApplicationDbContext context) : base(context) { }
}
