using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Contact;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.Contact;

public interface IContactMessageService
{
    Task<ContactMessageDto> SubmitAsync(CreateContactMessageDto dto, string? ipAddress, CancellationToken ct = default);
    Task<Paginate<ContactMessageDto>> GetListAsync(int page, int pageSize, ContactStatus? status, CancellationToken ct = default);
    Task<ContactMessageDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ContactMessageDto> UpdateStatusAsync(int id, UpdateContactMessageStatusDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
