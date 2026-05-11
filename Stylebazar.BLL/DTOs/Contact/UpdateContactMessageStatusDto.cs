using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Contact;

public class UpdateContactMessageStatusDto
{
    public ContactStatus Status { get; set; }
    public string? AdminNote { get; set; }
}
