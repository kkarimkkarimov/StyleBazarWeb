using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.DAL.Repositories.Concrete;

// Message repository-sinin GERÇƏKLƏŞMƏSİ. CRUD EfRepositoryBase-dən gəlir; əlavə olaraq
// "söhbəti oxundu işarələ" metodunu effektiv (çoxluq-əsaslı) şəkildə həyata keçirir.
public class MessageRepository : EfRepositoryBase<Message, int, ApplicationDbContext>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context) { }

    public async Task<int> MarkConversationReadAsync(int conversationId, string readerUserId, CancellationToken ct = default)
    {
        // Tək, çoxluq-əsaslı (set-based) UPDATE — heç bir obyekt yaddaşa (change tracker-ə)
        // yüklənmir, sətir-sətir SaveChanges yoxdur. ExecuteUpdateAsync soft-delete filtrinə
        // hörmət edir, ona görə silinmiş sətirlərə toxunulmur.
        // Şərt: bu söhbətdə + göndərən oxucu DEYİL + hələ oxunmayıb → IsReadByRecipient = true.
        return await Context.Set<Message>()
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != readerUserId
                        && !m.IsReadByRecipient)
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.IsReadByRecipient, true),
                ct);
    }
}
