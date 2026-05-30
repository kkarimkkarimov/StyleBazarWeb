using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Chat;

/// <summary>
/// Reference implementation of <see cref="IChatService"/>. Stores conversations
/// with alphabetically-sorted (User1Id, User2Id) so the unique index in the
/// DB acts as a pair guard. All inbox / message queries enforce participant
/// membership; admin-only methods are explicitly suffixed.
/// </summary>
public class ChatManager : IChatService
{
    private const int PreviewMaxLength = 120;

    private readonly IConversationRepository _conversations;
    private readonly IMessageRepository _messages;
    private readonly IStoryRepository _stories;
    private readonly IProductRepository _products;
    private readonly ICurrentUserService _currentUser;
    private readonly UserManager<AppUser> _users;
    private readonly IValidator<SendMessageDto> _sendValidator;

    public ChatManager(
        IConversationRepository conversations,
        IMessageRepository messages,
        IStoryRepository stories,
        IProductRepository products,
        ICurrentUserService currentUser,
        UserManager<AppUser> users,
        IValidator<SendMessageDto> sendValidator)
    {
        _conversations = conversations;
        _messages = messages;
        _stories = stories;
        _products = products;
        _currentUser = currentUser;
        _users = users;
        _sendValidator = sendValidator;
    }

    // ============== INBOX ==============

    public async Task<Paginate<ConversationDto>> GetMyConversationsAsync(int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        string me = RequireUserId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        Paginate<Conversation> result = await _conversations.GetListAsync(
            predicate: c => c.User1Id == me || c.User2Id == me,
            orderBy: q => q.OrderByDescending(c => c.LastMessageAt),
            include: q => q.Include(c => c.User1).Include(c => c.User2),
            index: page - 1,
            size: pageSize,
            enableTracking: false,
            cancellationToken: ct);

        var rows = new List<ConversationDto>(result.Items.Count);
        foreach (Conversation c in result.Items)
        {
            bool meIs1 = c.User1Id == me;
            AppUser peer = meIs1 ? c.User2 : c.User1;
            IList<string> peerRoles = peer is null ? Array.Empty<string>() : await _users.GetRolesAsync(peer);
            rows.Add(new ConversationDto
            {
                Id = c.Id,
                PeerUserId = peer?.Id ?? (meIs1 ? c.User2Id : c.User1Id),
                PeerFullName = peer?.FullName ?? "(silinmiş istifadəçi)",
                PeerAvatarUrl = peer?.ProfileImageUrl,
                PeerRole = peerRoles.FirstOrDefault() ?? "Buyer",
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = c.LastMessagePreview,
                UnreadCount = meIs1 ? c.Unread1 : c.Unread2
            });
        }

        return new Paginate<ConversationDto>
        {
            Index = page - 1, Size = pageSize, Count = result.Count,
            Pages = (int)Math.Ceiling(result.Count / (double)pageSize),
            Items = rows
        };
    }

    public async Task<Paginate<MessageDto>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        string me = RequireUserId();
        Conversation conv = await RequireParticipantAsync(conversationId, me, ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        Paginate<Message> result = await _messages.GetListAsync(
            predicate: m => m.ConversationId == conversationId && !m.IsDeletedForAll,
            orderBy: q => q.OrderByDescending(m => m.CreatedDate),
            include: q => q.Include(m => m.Sender)
                           .Include(m => m.Story).ThenInclude(s => s!.Store)
                           .Include(m => m.Product).ThenInclude(p => p!.Images),
            index: page - 1,
            size: pageSize,
            enableTracking: false,
            cancellationToken: ct);

        var rows = result.Items.Select(ToDto).ToList();
        return new Paginate<MessageDto>
        {
            Index = page - 1, Size = pageSize, Count = result.Count,
            Pages = (int)Math.Ceiling(result.Count / (double)pageSize),
            Items = rows
        };
    }

    // ============== SEND ==============

    public async Task<ConversationDto> GetOrCreateConversationAsync(string peerUserId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        if (string.IsNullOrEmpty(peerUserId) || peerUserId == me)
            throw new BusinessException("Söhbət açmaq üçün başqa istifadəçi seç.");

        (string u1, string u2) = SortPair(me, peerUserId);

        AppUser? peer = await _users.FindByIdAsync(peerUserId);
        if (peer is null)
            throw new NotFoundException("İstifadəçi tapılmadı.");

        Conversation? conv = await _conversations.GetAsync(
            predicate: c => c.User1Id == u1 && c.User2Id == u2,
            include: q => q.Include(c => c.User1).Include(c => c.User2),
            enableTracking: false,
            cancellationToken: ct);

        if (conv is null)
        {
            conv = new Conversation
            {
                User1Id = u1,
                User2Id = u2,
                LastMessageAt = DateTime.UtcNow,
                LastMessagePreview = null,
                Unread1 = 0,
                Unread2 = 0
            };
            conv = await _conversations.AddAsync(conv, ct);
            // re-load with includes for the projection below
            conv = await _conversations.GetAsync(
                predicate: c => c.Id == conv.Id,
                include: q => q.Include(c => c.User1).Include(c => c.User2),
                enableTracking: false,
                cancellationToken: ct) ?? conv;
        }

        bool meIs1 = conv.User1Id == me;
        AppUser? peerUser = meIs1 ? conv.User2 : conv.User1;
        IList<string> peerRoles = peerUser is null ? Array.Empty<string>() : await _users.GetRolesAsync(peerUser);

        return new ConversationDto
        {
            Id = conv.Id,
            PeerUserId = peer.Id,
            PeerFullName = peer.FullName,
            PeerAvatarUrl = peer.ProfileImageUrl,
            PeerRole = peerRoles.FirstOrDefault() ?? "Buyer",
            LastMessageAt = conv.LastMessageAt,
            LastMessagePreview = conv.LastMessagePreview,
            UnreadCount = meIs1 ? conv.Unread1 : conv.Unread2
        };
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default)
    {
        string me = RequireUserId();

        ValidationResult validation = await _sendValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        if (dto.RecipientUserId == me)
            throw new BusinessException("Özünə mesaj göndərə bilməzsən.");

        // Either-or resolve: caller can target a specific user; we sort the
        // pair and find-or-create the conversation atomically.
        (string u1, string u2) = SortPair(me, dto.RecipientUserId);
        Conversation? conv = await _conversations.GetAsync(
            predicate: c => c.User1Id == u1 && c.User2Id == u2,
            enableTracking: true,
            cancellationToken: ct);

        if (conv is null)
        {
            AppUser? peer = await _users.FindByIdAsync(dto.RecipientUserId);
            if (peer is null) throw new NotFoundException("Alıcı istifadəçi tapılmadı.");

            conv = new Conversation
            {
                User1Id = u1,
                User2Id = u2,
                LastMessageAt = DateTime.UtcNow
            };
            conv = await _conversations.AddAsync(conv, ct);
        }

        Message msg = new()
        {
            ConversationId = conv.Id,
            SenderId = me,
            MessageType = dto.MessageType,
            Body = dto.Body?.Trim(),
            AttachmentUrl = dto.AttachmentUrl,
            StoryId = dto.MessageType == MessageType.StoryReply ? dto.StoryId : null,
            ProductId = dto.MessageType == MessageType.ProductShare ? dto.ProductId : null,
            IsReadByRecipient = false
        };
        msg = await _messages.AddAsync(msg, ct);

        // Update denormalised fields + flip the OTHER user's unread counter.
        Conversation? convTracked = await _conversations.GetAsync(c => c.Id == conv.Id, cancellationToken: ct);
        if (convTracked is not null)
        {
            convTracked.LastMessageAt = DateTime.UtcNow;
            convTracked.LastMessagePreview = BuildPreview(msg);
            if (convTracked.User1Id == me) convTracked.Unread2 += 1;
            else convTracked.Unread1 += 1;
            await _conversations.UpdateAsync(convTracked, ct);
        }

        // Re-load the saved row with the relations the DTO needs.
        Message? hydrated = await _messages.GetAsync(
            predicate: m => m.Id == msg.Id,
            include: q => q.Include(m => m.Sender)
                           .Include(m => m.Story).ThenInclude(s => s!.Store)
                           .Include(m => m.Product).ThenInclude(p => p!.Images),
            enableTracking: false,
            cancellationToken: ct);
        return ToDto(hydrated ?? msg);
    }

    // ============== READ / UNREAD ==============

    public async Task<string?> MarkConversationReadAsync(int conversationId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        Conversation? conv = await _conversations.GetAsync(
            predicate: c => c.Id == conversationId,
            enableTracking: true,
            cancellationToken: ct);
        if (conv is null) return null;
        if (conv.User1Id != me && conv.User2Id != me) return null;

        bool meIs1 = conv.User1Id == me;
        if (meIs1 && conv.Unread1 == 0) return null;
        if (!meIs1 && conv.Unread2 == 0) return null;

        string peerId = meIs1 ? conv.User2Id : conv.User1Id;

        if (meIs1) conv.Unread1 = 0; else conv.Unread2 = 0;
        await _conversations.UpdateAsync(conv, ct);

        // Flip IsReadByRecipient on the OTHER user's messages — drives the
        // "Görüldü" indicator the sender sees in their view.
        Paginate<Message> unread = await _messages.GetListAsync(
            predicate: m => m.ConversationId == conversationId && m.SenderId != me && !m.IsReadByRecipient,
            orderBy: q => q.OrderBy(m => m.Id),
            index: 0, size: 500,
            enableTracking: true,
            cancellationToken: ct);
        foreach (Message m in unread.Items)
        {
            m.IsReadByRecipient = true;
            await _messages.UpdateAsync(m, ct);
        }

        return peerId;
    }

    public async Task<int> GetMyUnreadTotalAsync(CancellationToken ct = default)
    {
        string me = RequireUserId();
        Paginate<Conversation> all = await _conversations.GetListAsync(
            predicate: c => (c.User1Id == me && c.Unread1 > 0) || (c.User2Id == me && c.Unread2 > 0),
            orderBy: q => q.OrderBy(c => c.Id),
            index: 0, size: 1000,
            enableTracking: false,
            cancellationToken: ct);
        return all.Items.Sum(c => c.User1Id == me ? c.Unread1 : c.Unread2);
    }

    // ============== ADMIN MODERATION ==============

    public async Task<Paginate<AdminConversationRowDto>> GetAllConversationsForAdminAsync(int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        Paginate<Conversation> result = await _conversations.GetListAsync(
            orderBy: q => q.OrderByDescending(c => c.LastMessageAt),
            include: q => q.Include(c => c.User1).Include(c => c.User2),
            index: page - 1,
            size: pageSize,
            enableTracking: false,
            cancellationToken: ct);

        var rows = new List<AdminConversationRowDto>(result.Items.Count);
        foreach (Conversation c in result.Items)
        {
            IList<string> r1 = c.User1 is null ? Array.Empty<string>() : await _users.GetRolesAsync(c.User1);
            IList<string> r2 = c.User2 is null ? Array.Empty<string>() : await _users.GetRolesAsync(c.User2);
            int msgCount = (await _messages.GetListAsync(
                predicate: m => m.ConversationId == c.Id,
                orderBy: q => q.OrderBy(m => m.Id),
                index: 0, size: 1,
                enableTracking: false,
                cancellationToken: ct)).Count;
            rows.Add(new AdminConversationRowDto
            {
                Id = c.Id,
                User1Id = c.User1Id,
                User1FullName = c.User1?.FullName ?? "(silinmiş)",
                User1Role = r1.FirstOrDefault() ?? "Buyer",
                User2Id = c.User2Id,
                User2FullName = c.User2?.FullName ?? "(silinmiş)",
                User2Role = r2.FirstOrDefault() ?? "Buyer",
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = c.LastMessagePreview,
                MessageCount = msgCount
            });
        }

        return new Paginate<AdminConversationRowDto>
        {
            Index = page - 1, Size = pageSize, Count = result.Count,
            Pages = (int)Math.Ceiling(result.Count / (double)pageSize),
            Items = rows
        };
    }

    public async Task<Paginate<MessageDto>> GetMessagesForAdminAsync(int conversationId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        Paginate<Message> result = await _messages.GetListAsync(
            predicate: m => m.ConversationId == conversationId,  // admin sees deleted-for-all too? Keep visible.
            orderBy: q => q.OrderByDescending(m => m.CreatedDate),
            include: q => q.Include(m => m.Sender)
                           .Include(m => m.Story).ThenInclude(s => s!.Store)
                           .Include(m => m.Product).ThenInclude(p => p!.Images),
            index: page - 1,
            size: pageSize,
            enableTracking: false,
            cancellationToken: ct);

        return new Paginate<MessageDto>
        {
            Index = page - 1, Size = pageSize, Count = result.Count,
            Pages = (int)Math.Ceiling(result.Count / (double)pageSize),
            Items = result.Items.Select(ToDto).ToList()
        };
    }

    public async Task DeleteMessageForAdminAsync(int messageId, CancellationToken ct = default)
    {
        Message? m = await _messages.GetAsync(
            predicate: x => x.Id == messageId,
            enableTracking: true,
            cancellationToken: ct);
        if (m is null) throw new NotFoundException("Mesaj tapılmadı.");
        m.IsDeletedForAll = true;
        await _messages.UpdateAsync(m, ct);
    }

    // ============== HELPERS ==============

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Account.NotAuthenticated);
        return _currentUser.UserId;
    }

    private async Task<Conversation> RequireParticipantAsync(int conversationId, string userId, CancellationToken ct)
    {
        Conversation? conv = await _conversations.GetAsync(
            predicate: c => c.Id == conversationId,
            enableTracking: false,
            cancellationToken: ct);
        if (conv is null) throw new NotFoundException("Söhbət tapılmadı.");
        if (conv.User1Id != userId && conv.User2Id != userId)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");
        return conv;
    }

    /// <summary>Alphabetical pair sort — keeps the (User1Id, User2Id) unique index honest.</summary>
    private static (string lo, string hi) SortPair(string a, string b)
    {
        return string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    }

    private static string BuildPreview(Message m)
    {
        string raw = m.MessageType switch
        {
            MessageType.Image => "📷 Şəkil" + (string.IsNullOrEmpty(m.Body) ? "" : " · " + m.Body),
            MessageType.StoryReply => "↩ Hekayəyə cavab: " + (m.Body ?? ""),
            MessageType.ProductShare => "🛍 Məhsul paylaşımı" + (string.IsNullOrEmpty(m.Body) ? "" : " · " + m.Body),
            _ => m.Body ?? ""
        };
        return raw.Length <= PreviewMaxLength ? raw : raw.Substring(0, PreviewMaxLength - 1) + "…";
    }

    private static MessageDto ToDto(Message m)
    {
        var dto = new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderFullName = m.Sender?.FullName ?? "(silinmiş)",
            SenderAvatarUrl = m.Sender?.ProfileImageUrl,
            MessageType = m.MessageType,
            Body = m.Body,
            AttachmentUrl = m.AttachmentUrl,
            IsReadByRecipient = m.IsReadByRecipient,
            CreatedDate = m.CreatedDate
        };
        if (m.Story is not null)
        {
            dto.StoryPreview = new StoryPreviewDto
            {
                Id = m.Story.Id,
                MediaUrl = m.Story.MediaUrl,
                ThumbnailUrl = m.Story.ThumbnailUrl,
                MediaType = m.Story.MediaType,
                Caption = m.Story.Caption,
                IsExpired = m.Story.ExpiresAt <= DateTime.UtcNow
            };
        }
        if (m.Product is not null)
        {
            string? imgUrl = m.Product.Images?.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl;
            dto.ProductPreview = new ProductPreviewDto
            {
                Id = m.Product.Id,
                Name = m.Product.Name,
                Slug = m.Product.Slug ?? string.Empty,
                ImageUrl = imgUrl,
                Price = m.Product.Price,
                DiscountPrice = m.Product.DiscountPrice
            };
        }
        return dto;
    }
}
