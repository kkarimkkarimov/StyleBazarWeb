using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

public class ContactMessage : Entity<int>
{
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? Subject { get; set; }
    public required string Message { get; set; }
    public ContactStatus Status { get; set; } = ContactStatus.New;
    public string? AdminNote { get; set; }
    public string? UserIpAddress { get; set; }
}
