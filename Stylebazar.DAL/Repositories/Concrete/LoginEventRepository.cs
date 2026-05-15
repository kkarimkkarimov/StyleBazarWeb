using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class LoginEventRepository : EfRepositoryBase<LoginEvent, int, ApplicationDbContext>, ILoginEventRepository
{
    public LoginEventRepository(ApplicationDbContext context) : base(context) { }
}
