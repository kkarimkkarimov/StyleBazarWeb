using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

public interface IMessageRepository : IAsyncRepository<Message, int>
{
    /// <summary>
    /// Flips IsReadByRecipient = true for every not-yet-read message in the
    /// conversation that was sent by someone other than the reader, in a single
    /// SQL UPDATE. Replaces the load-all-then-SaveChanges-per-message pattern.
    /// Returns the number of rows updated.
    /// </summary>
    Task<int> MarkConversationReadAsync(int conversationId, string readerUserId, CancellationToken ct = default);
}
