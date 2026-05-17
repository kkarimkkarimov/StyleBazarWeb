using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class StoreFollowerRepository : EfRepositoryBase<StoreFollower, int, ApplicationDbContext>, IStoreFollowerRepository
{
    public StoreFollowerRepository(ApplicationDbContext context) : base(context)
    {
    }
}
