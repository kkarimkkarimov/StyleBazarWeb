using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

public interface IContactClickRepository : IAsyncRepository<ContactClick, int> { }
