using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.Services.Chat;
using Stylebazar.DAL.Entities;

namespace Stylebazar.MVC.Hubs;

/// <summary>
/// Real-time chat hub. Every signed-in user is auto-joined to a SignalR group
/// named "user-{UserId}" so peer-to-peer delivery is just
/// <c>Clients.Group("user-{recipient}").SendAsync(...)</c>.
///
/// Presence is tracked in a lightweight in-memory dict (Set&lt;ConnectionId&gt; per
/// user); for a single-instance deployment that's enough. When we scale out we
/// move this to Redis via the SignalR backplane.
///
/// Auth is enforced at the class level — no anonymous connections allowed.
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    /// <summary>Shared by all hub instances. Key = userId, value = set of active connection ids.</summary>
    private static readonly ConcurrentDictionary<string, HashSet<string>> _onlineConnections = new();
    private static readonly object _onlineLock = new();

    private readonly IChatService _chat;
    private readonly UserManager<AppUser> _users;

    public ChatHub(IChatService chat, UserManager<AppUser> users)
    {
        _chat = chat;
        _users = users;
    }

    public override async Task OnConnectedAsync()
    {
        string? userId = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
            bool wasOffline = AddConnection(userId, Context.ConnectionId);
            if (wasOffline)
            {
                // Tell anyone who has this user in their open conversations: this peer just came online.
                await Clients.Others.SendAsync("UserPresence", new { userId, online = true });
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(userId))
        {
            bool nowOffline = RemoveConnection(userId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId));
            if (nowOffline)
            {
                await Clients.Others.SendAsync("UserPresence", new { userId, online = false });
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a message via SignalR. Persists via BLL (same path as REST) then
    /// pushes the saved DTO to BOTH the sender's other tabs and the recipient.
    /// Returns the saved DTO so the calling tab can replace its optimistic row.
    /// </summary>
    public async Task<MessageDto> SendMessage(SendMessageDto dto)
    {
        // BLL throws on validation/business issues; the JS catches via the
        // hub method's promise rejection and surfaces a toast.
        MessageDto saved = await _chat.SendMessageAsync(dto, Context.ConnectionAborted);

        string? me = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(me))
        {
            // Sender's other tabs get the message echoed (so they update too).
            await Clients.Group(GroupName(me)).SendAsync("MessageReceived", saved);
        }
        if (!string.Equals(dto.RecipientUserId, me, StringComparison.OrdinalIgnoreCase))
        {
            await Clients.Group(GroupName(dto.RecipientUserId)).SendAsync("MessageReceived", saved);
        }
        return saved;
    }

    /// <summary>Marks all messages in the conversation as read on behalf of the caller.</summary>
    public async Task MarkRead(int conversationId)
    {
        string? peerId = await _chat.MarkConversationReadAsync(conversationId, Context.ConnectionAborted);
        string? me = _users.GetUserId(Context.User!);
        if (!string.IsNullOrEmpty(peerId) && !string.IsNullOrEmpty(me))
        {
            // Notify ONLY the peer (the original sender) that their messages were
            // seen — drives the "Görüldü" indicator on their side. Targeting the
            // peer's group avoids leaking read receipts to unrelated clients.
            await Clients.Group(GroupName(peerId)).SendAsync("MessagesRead", new { conversationId, readerUserId = me });
        }
    }

    /// <summary>
    /// Typing indicator — fire-and-forget. We don't persist it; the recipient
    /// receives a transient "Typing" event that decays client-side after a few seconds.
    /// </summary>
    public async Task Typing(string peerUserId, bool isTyping)
    {
        string? me = _users.GetUserId(Context.User!);
        if (string.IsNullOrEmpty(me) || string.IsNullOrEmpty(peerUserId)) return;
        await Clients.Group(GroupName(peerUserId)).SendAsync("PeerTyping", new { userId = me, isTyping });
    }

    /// <summary>Returns presence status for a batch of users (used by the inbox dot).</summary>
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

    // -------- helpers --------

    private static string GroupName(string userId) => "user-" + userId;

    /// <summary>True if this user wasn't online before (i.e. first connection).</summary>
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

    /// <summary>True if removing this connection leaves the user with zero connections.</summary>
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
