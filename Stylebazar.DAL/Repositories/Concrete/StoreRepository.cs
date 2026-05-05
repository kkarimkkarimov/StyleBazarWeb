using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class StoreRepository : EfRepositoryBase<Store, int, ApplicationDbContext>, IStoreRepository
{
    public StoreRepository(ApplicationDbContext context) : base(context)
    {
    }
}
