using Stylebazar.BLL.DTOs.Account;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Services.LoginEvents;

// ILoginEventService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface ILoginEventService
{
    /// <summary>
    /// Append a new auth-event row for the given user. Fire-and-forget from
    /// the controller's POV — failures are swallowed so a logging hiccup never
    /// blocks the login itself.
    /// </summary>
    Task TrackAsync(string userId, LoginEventType type, string? ip, string? userAgent, CancellationToken ct = default);

    /// <summary>
    /// Login history (last 30 days by default) for the current user.
    /// Newest first. Drives the Security → Login History block.
    /// </summary>
    Task<List<LoginEventDto>> GetMyHistoryAsync(int days = 30, CancellationToken ct = default);

    /// <summary>
    /// Active sessions for the current user. Currently returns a single
    /// "current device" row derived from the most-recent SignIn event;
    /// a real Sessions table can be added in a later phase if we need
    /// remote-logout granularity.
    /// </summary>
    Task<List<ActiveSessionDto>> GetMyActiveSessionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Platform-wide recent auth activity for the admin dashboard. Joins each
    /// event to its user so the view can show "kim, nə vaxt, hansı cihazdan".
    /// Newest first, capped at <paramref name="take"/> rows.
    /// </summary>
    Task<List<AdminLoginEventDto>> GetRecentForAdminAsync(int take = 10, CancellationToken ct = default);
}
