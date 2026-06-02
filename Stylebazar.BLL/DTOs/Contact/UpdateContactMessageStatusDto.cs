using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Contact;

// UpdateContactMessageStatusDto — mövcud qeydi YENİLƏMƏK üçün sahələri daşıyan DTO.
public class UpdateContactMessageStatusDto
{
    public ContactStatus Status { get; set; }
    public string? AdminNote { get; set; }
}
