using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class HeroBannerRepository : EfRepositoryBase<HeroBanner, int, ApplicationDbContext>, IHeroBannerRepository
{
    public HeroBannerRepository(ApplicationDbContext context) : base(context)
    {
    }
}
