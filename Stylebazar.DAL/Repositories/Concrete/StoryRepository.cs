using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class StoryRepository : EfRepositoryBase<Story, int, ApplicationDbContext>, IStoryRepository
{
    public StoryRepository(ApplicationDbContext context) : base(context)
    {
    }
}
