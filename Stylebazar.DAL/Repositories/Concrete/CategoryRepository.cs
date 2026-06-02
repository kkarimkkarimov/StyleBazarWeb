using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// Category repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir;
// burada yalnız konstruktor DbContext-i bazaya ötürür.
public class CategoryRepository : EfRepositoryBase<Category, int, ApplicationDbContext>, ICategoryRepository
{
    public CategoryRepository(ApplicationDbContext context) : base(context)
    {
    }
}
