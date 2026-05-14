using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class ProductColorRepository : EfRepositoryBase<ProductColor, int, ApplicationDbContext>, IProductColorRepository
{
    public ProductColorRepository(ApplicationDbContext context) : base(context) { }
}
