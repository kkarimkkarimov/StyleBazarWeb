using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// StoreFollower repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir;
// burada yalnız konstruktor DbContext-i bazaya ötürür.
public class StoreFollowerRepository : EfRepositoryBase<StoreFollower, int, ApplicationDbContext>, IStoreFollowerRepository
{
    public StoreFollowerRepository(ApplicationDbContext context) : base(context)
    {
    }
}
