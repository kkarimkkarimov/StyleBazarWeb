using Core.Persistence.Paging;
using Microsoft.AspNetCore.Identity;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Services.LoginEvents;

// LoginEventManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class LoginEventManager : ILoginEventService
{
    private readonly ILoginEventRepository _events;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<AppUser> _users;

    public LoginEventManager(ILoginEventRepository events, ICurrentUserService currentUser, UserManager<AppUser> users)
    {
        _events = events;
        _currentUser = currentUser;
        _users = users;
    }

    public async Task TrackAsync(string userId, LoginEventType type, string? ip, string? userAgent, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return;

        try
        {
            LoginEvent entity = new()
            {
                UserId = userId,
                EventType = type,
                IpAddress = Truncate(ip, 64),
                UserAgent = Truncate(userAgent, 500)
            };
            await _events.AddAsync(entity, ct);
        }
        catch
        {
            // Auth must not fail because of an audit-log hiccup. Swallow.
        }
    }

    public async Task<List<LoginEventDto>> GetMyHistoryAsync(int days = 30, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return new List<LoginEventDto>();

        DateTime since = DateTime.UtcNow.AddDays(-Math.Max(1, days));
        string userId = _currentUser.UserId;

        Paginate<LoginEvent> page = await _events.GetListAsync(
            predicate: e => e.UserId == userId && e.CreatedDate >= since,
            orderBy: q => q.OrderByDescending(e => e.CreatedDate),
            index: 0,
            size: 50,
            enableTracking: false,
            cancellationToken: ct);

        return page.Items.Select(ToDto).ToList();
    }

    public async Task<List<ActiveSessionDto>> GetMyActiveSessionsAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            return new List<ActiveSessionDto>();

        string userId = _currentUser.UserId;

        // Find latest SignIn / SignUp — represents the "current device"
        Paginate<LoginEvent> page = await _events.GetListAsync(
            predicate: e => e.UserId == userId &&
                            (e.EventType == LoginEventType.SignIn ||
                             e.EventType == LoginEventType.SignUp),
            orderBy: q => q.OrderByDescending(e => e.CreatedDate),
            index: 0,
            size: 1,
            enableTracking: false,
            cancellationToken: ct);

        LoginEvent? latest = page.Items.FirstOrDefault();
        if (latest is null) return new List<ActiveSessionDto>();

        return new List<ActiveSessionDto>
        {
            new()
            {
                DeviceLabel = DeriveDeviceLabel(latest.UserAgent),
                IpAddress = latest.IpAddress,
                LastSeen = latest.CreatedDate,
                IsCurrent = true
            }
        };
    }

    public async Task<List<AdminLoginEventDto>> GetRecentForAdminAsync(int take = 10, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);

        // Fetch the most-recent N events, then resolve each user with a per-row
        // lookup. N is small (default 10) so the extra roundtrips are
        // immaterial and we don't need to expose the Identity DbSet here.
        Paginate<LoginEvent> page = await _events.GetListAsync(
            orderBy: q => q.OrderByDescending(e => e.CreatedDate),
            index: 0,
            size: take,
            enableTracking: false,
            cancellationToken: ct);

        var list = new List<AdminLoginEventDto>(page.Items.Count);
        foreach (LoginEvent e in page.Items)
        {
            AppUser? u = await _users.FindByIdAsync(e.UserId);
            list.Add(new AdminLoginEventDto
            {
                Id = e.Id,
                EventType = e.EventType,
                EventLabel = ToDto(e).EventLabel,
                IpAddress = e.IpAddress,
                DeviceLabel = DeriveDeviceLabel(e.UserAgent),
                CreatedDate = e.CreatedDate,
                UserId = e.UserId,
                UserFullName = u?.FullName ?? "(silinmiş istifadəçi)",
                UserEmail = u?.Email ?? string.Empty
            });
        }
        return list;
    }

    // -------- helpers --------

    private static LoginEventDto ToDto(LoginEvent e) => new()
    {
        Id = e.Id,
        EventType = e.EventType,
        EventLabel = e.EventType switch
        {
            LoginEventType.SignUp => "Sign Up",
            LoginEventType.SignIn => "Login",
            LoginEventType.SignOut => "Logout",
            LoginEventType.FailedSignIn => "Failed Login",
            LoginEventType.PasswordChanged => "Password Changed",
            LoginEventType.PasswordReset => "Password Reset",
            LoginEventType.EmailVerified => "Email Verified",
            LoginEventType.TwoFactorChallenge => "2FA Prompt",
            LoginEventType.Locked => "Account Locked",
            _ => e.EventType.ToString()
        },
        IpAddress = e.IpAddress,
        UserAgent = e.UserAgent,
        DeviceLabel = DeriveDeviceLabel(e.UserAgent),
        CreatedDate = e.CreatedDate
    };

    private static string DeriveDeviceLabel(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Bilinməyən cihaz";
        string ua = userAgent.ToLowerInvariant();
        if (ua.Contains("iphone")) return "iPhone";
        if (ua.Contains("ipad")) return "iPad";
        if (ua.Contains("android")) return "Android";
        if (ua.Contains("mac os")) return "Mac";
        if (ua.Contains("windows")) return "Windows PC";
        if (ua.Contains("linux")) return "Linux";
        return "Bilinməyən cihaz";
    }

    private static string? Truncate(string? s, int max)
    {
        if (s is null) return null;
        return s.Length <= max ? s : s.Substring(0, max);
    }
}
