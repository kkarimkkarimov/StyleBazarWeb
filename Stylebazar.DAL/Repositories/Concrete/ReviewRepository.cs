using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// Review repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir;
// burada yalnız konstruktor DbContext-i bazaya ötürür.
public class ReviewRepository : EfRepositoryBase<Review, int, ApplicationDbContext>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }
}
