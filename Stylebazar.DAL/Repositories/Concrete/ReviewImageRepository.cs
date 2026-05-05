using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class ReviewImageRepository : EfRepositoryBase<ReviewImage, int, ApplicationDbContext>, IReviewImageRepository
{
    public ReviewImageRepository(ApplicationDbContext context) : base(context)
    {
    }
}
