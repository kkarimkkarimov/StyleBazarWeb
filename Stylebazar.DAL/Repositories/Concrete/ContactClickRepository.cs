using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class ContactClickRepository : EfRepositoryBase<ContactClick, int, ApplicationDbContext>, IContactClickRepository
{
    public ContactClickRepository(ApplicationDbContext context) : base(context) { }
}
