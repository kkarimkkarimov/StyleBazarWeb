using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

/// <summary>
/// An audit row for every authentication-related event. Rendered on the
/// user's Profile page → Security → Login History.
/// </summary>
public class LoginEvent : Entity<int>
{
    public required string UserId { get; set; }
    public LoginEventType EventType { get; set; }

    /// <summary>Source IP (proxy-aware via X-Forwarded-For when present).</summary>
    public string? IpAddress { get; set; }

    /// <summary>Raw User-Agent header — parsed at render time into device hints.</summary>
    public string? UserAgent { get; set; }

    public virtual AppUser User { get; set; } = null!;
}
