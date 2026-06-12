using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.Services.Chat;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;

namespace Stylebazar.MVC.Hubs;

// CHAT HUB — REAL-VAXT (anlıq) mesajlaşma üçün SignalR hub-ı.
// SignalR brauzerlə server arasında daimi bağlantı (WebSocket) saxlayır, beləliklə mesaj
// səhifəni yeniləmədən dərhal gəlir. Hər giriş etmiş istifadəçi "user-{id}" qrupuna qoşulur;
// kiməsə mesaj göndərmək = onun qrupuna SendAsync etmək.
// Onlayn/oflayn vəziyyəti yaddaşda (in-memory) saxlanır (tək server üçün kifayətdir).
// [Authorize] → yalnız giriş etmiş istifadəçilər qoşula bilər.
[Authorize]
public class ChatHub : Hub
{
    // Bütün hub nümunələri üçün ortaq: açar = userId, dəyər = onun aktiv bağlantı id-ləri.
    private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineConnections = new();
    private static readonly object _onlineLock = new();

    private readonly IChatService _chat;              // mesajları yadda saxlayan biznes xidməti
    private readonly UserManager<AppUser> _users;     // cari istifadəçini tapmaq üçün
    private readonly ApplicationDbContext _db;        // "son görülmə" yazısı üçün

    public ChatHub(IChatService chat, UserManager<AppUser> users, ApplicationDbContext db)
    {
        _chat = chat;
        _users = users;
        _db = db;
    }

    // İstifadəçi qoşulanda işə düşür: onu öz qrupuna əlavə et və "onlayn oldu" siqnalı göndər.
    public override async Task OnConnectedAsync()
    {
        string? userId = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
            bool wasOffline = AddConnection(userId, Context.ConnectionId);
            if (wasOffline)
            {
                // Bu istifadəçini açıq söhbətində saxlayanlara bildir: bu nəfər indi onlayn oldu.
                await Clients.Others.SendAsync("UserPresence", new { userId, online = true });
            }
        }
        await base.OnConnectedAsync();
    }

    // İstifadəçi ayrılanda işə düşür: bağlantını çıxar; başqa bağlantısı qalmadısa "oflayn" bildir.
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(userId))
        {
            bool nowOffline = RemoveConnection(userId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId));
            if (nowOffline)
            {
                // "Son görülmə"ni yaz (tək set-based UPDATE — tracking yükü yoxdur).
                DateTime now = DateTime.UtcNow;
                await _db.Users.Where(u => u.Id == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSeenAt, now));
                await Clients.Others.SendAsync("UserPresence", new { userId, online = false, lastSeenAt = now });
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    // MESAJ GÖNDƏR — əvvəlcə BLL ilə bazaya yaz, sonra həm göndərənin digər tab-larına,
    // həm də qəbul edənə anlıq çatdır. Yadda saxlanmış mesajı geri qaytarır.
    public async Task<MessageDto> SendMessage(SendMessageDto dto)
    {
        // BLL yoxlama/biznes problemində xəta atır; JS bunu tutub toast göstərir.
        MessageDto saved = await _chat.SendMessageAsync(dto, Context.ConnectionAborted);

        string? me = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(me))
        {
            // Göndərənin digər açıq tab-larına da mesajı göndər (onlar da yenilənsin).
            await Clients.Group(GroupName(me)).SendAsync("MessageReceived", saved);
        }
        if (!string.Equals(dto.RecipientUserId, me, StringComparison.OrdinalIgnoreCase))
        {
            await Clients.Group(GroupName(dto.RecipientUserId)).SendAsync("MessageReceived", saved);
        }
        return saved;
    }

    // OXUNDU İŞARƏLƏ — söhbətdəki mesajları cari istifadəçi adından oxundu kimi qeyd et.
    public async Task MarkRead(int conversationId)
    {
        string? peerId = await _chat.MarkConversationReadAsync(conversationId, Context.ConnectionAborted);
        string? me = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(peerId) && !string.IsNullOrEmpty(me))
        {
            // YALNIZ qarşı tərəfə (mesajı göndərənə) bildir ki, mesajları görüldü →
            // onun tərəfində "Görüldü" nişanını işə salır.
            await Clients.Group(GroupName(peerId)).SendAsync("MessagesRead", new { conversationId, readerUserId = me });
        }
    }

    // YAZIR... göstəricisi — saxlanılmır, sadəcə qarşı tərəfə ötəri siqnal göndərilir.
    public async Task Typing(string peerUserId, bool isTyping)
    {
        string? me = _users.GetUserId(Context.User!);
        if (string.IsNullOrEmpty(me) || string.IsNullOrEmpty(peerUserId)) return;
        await Clients.Group(GroupName(peerUserId)).SendAsync("PeerTyping", new { userId = me, isTyping });
    }

    // Bir qrup istifadəçinin onlayn/oflayn vəziyyətini qaytarır (gələnlər qutusundakı nöqtə üçün).
    public Task<Dictionary<string, bool>> GetPresence(string[] userIds)
    {
        var snapshot = new Dictionary<string, bool>(userIds.Length);
        lock (_onlineLock)
        {
            foreach (string uid in userIds)
            {
                snapshot[uid] = _onlineConnections.TryGetValue(uid, out var conns) && conns.Count > 0;
            }
        }
        return Task.FromResult(snapshot);
    }

    // -------- köməkçi metodlar --------

    // İstifadəçinin SignalR qrup adı.
    private static string GroupName(string userId) => "user-" + userId;

    // Bağlantı əlavə et. İstifadəçi əvvəl oflayn idisə true qaytarır (ilk bağlantı).
    private static bool AddConnection(string userId, string connectionId)
    {
        lock (_onlineLock)
        {
            if (!_onlineConnections.TryGetValue(userId, out var conns))
            {
                conns = new HashSet<string>();
                _onlineConnections[userId] = conns;
                conns.Add(connectionId);
                return true;
            }
            conns.Add(connectionId);
            return false;
        }
    }

    // Bağlantını çıxar. İstifadəçinin heç bağlantısı qalmadısa true qaytarır (tam oflayn).
    private static bool RemoveConnection(string userId, string connectionId)
    {
        lock (_onlineLock)
        {
            if (!_onlineConnections.TryGetValue(userId, out var conns)) return false;
            conns.Remove(connectionId);
            if (conns.Count == 0)
            {
                _onlineConnections.TryRemove(userId, out _);
                return true;
            }
            return false;
        }
    }
}
