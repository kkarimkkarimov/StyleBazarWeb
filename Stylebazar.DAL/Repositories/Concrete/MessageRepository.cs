using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

public class MessageRepository : EfRepositoryBase<Message, int, ApplicationDbContext>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context) { }

    public async Task<int> MarkConversationReadAsync(int conversationId, string readerUserId, CancellationToken ct = default)
    {
        // Single set-based UPDATE — no entities loaded into the change tracker,
        // no per-row SaveChanges. ExecuteUpdateAsync respects the soft-delete
        // global query filter, so deleted rows are left untouched.
        return await Context.Set<Message>()
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != readerUserId
                        && !m.IsReadByRecipient)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsReadByRecipient, true),
                ct);
    }
}
