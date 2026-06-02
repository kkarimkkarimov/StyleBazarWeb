using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities;

namespace Stylebazar.DAL.Repositories.Abstract;

// Message üçün repository MÜQAVİLƏSİ. Standart CRUD-a ƏLAVƏ olaraq bir xüsusi metodu var.
public interface IMessageRepository : IAsyncRepository<Message, int>
{
    // Söhbətdəki — oxucudan BAŞQASI tərəfindən göndərilmiş və hələ oxunmamış — bütün mesajları
    // BİR SQL UPDATE ilə "oxundu" işarələyir. Hər mesajı yükləyib bir-bir yadda saxlamaqdan
    // qat-qat sürətlidir. Yenilənən sətirlərin sayını qaytarır.
    Task<int> MarkConversationReadAsync(int conversationId, string readerUserId, CancellationToken ct = default);
}
