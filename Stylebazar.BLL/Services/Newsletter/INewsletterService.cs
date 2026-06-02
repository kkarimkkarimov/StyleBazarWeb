using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Newsletter;

namespace Stylebazar.BLL.Services.Newsletter;

// INewsletterService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface INewsletterService
{
    Task<NewsletterSubscriberDto> SubscribeAsync(SubscribeDto dto, CancellationToken ct = default);
    Task UnsubscribeAsync(string token, CancellationToken ct = default);
    Task<Paginate<NewsletterSubscriberDto>> GetListAsync(int page, int pageSize, bool? isActive, CancellationToken ct = default);
}
