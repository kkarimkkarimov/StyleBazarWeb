using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class MessageRepository : EfRepositoryBase<Message, int, ApplicationDbContext>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context) { }
}
