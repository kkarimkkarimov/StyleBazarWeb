using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class AddressRepository : EfRepositoryBase<Address, int, ApplicationDbContext>, IAddressRepository
{
    public AddressRepository(ApplicationDbContext context) : base(context) { }
}
