using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class SubscriptionRepository : EfRepositoryBase<Subscription, int, ApplicationDbContext>, ISubscriptionRepository
{
    public SubscriptionRepository(ApplicationDbContext context) : base(context) { }
}
