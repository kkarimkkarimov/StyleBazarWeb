using Core.Persistence.Repositories;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// Conversation repository-sinin GERÇƏKLƏŞMƏSİ. EfRepositoryBase bütün CRUD məntiqini verir;
// burada yalnız konstruktor DbContext-i bazaya ötürür.
public class ConversationRepository : EfRepositoryBase<Conversation, int, ApplicationDbContext>, IConversationRepository
{
    public ConversationRepository(ApplicationDbContext context) : base(context) { }
}
