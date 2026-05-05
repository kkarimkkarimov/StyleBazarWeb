using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class ProductSizeRepository : EfRepositoryBase<ProductSize, int, ApplicationDbContext>, IProductSizeRepository
{
    public ProductSizeRepository(ApplicationDbContext context) : base(context)
    {
    }
}
