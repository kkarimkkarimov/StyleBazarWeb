using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// HeroBanner repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir;
// burada yalnız konstruktor DbContext-i bazaya ötürür.
public class HeroBannerRepository : EfRepositoryBase<HeroBanner, int, ApplicationDbContext>, IHeroBannerRepository
{
    public HeroBannerRepository(ApplicationDbContext context) : base(context)
    {
    }
}
