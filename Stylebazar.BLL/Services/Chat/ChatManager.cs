using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Chat;

// ChatManager — IChatService-in V2 GERÇƏKLƏŞMƏSİ. Söhbətlər əlifba sırası ilə
// (User1Id, User2Id) saxlanır — bazadakı unikal indeks cüt qoruyucusu kimi işləyir.
// Bütün sorğular iştirakçılığı yoxlayır; blok yoxlaması göndərmə qapısındadır.
public class ChatManager : IChatService
{
    private const int PreviewMaxLength = 120;
    private const int MaxQuickReplies = 20;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IValidator<SendMessageDto> _sendValidator;

    public ChatManager(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IValidator<SendMessageDto> sendValidator)
    {
        _db = db;
        _currentUser = currentUser;
        _sendValidator = sendValidator;
    }

    // ============== GƏLƏNLƏR QUTUSU ==============

    public async Task<Paginate<ConversationDto>> GetMyConversationsAsync(
        string? filter = null, string? search = null, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        string me = RequireUserId();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Conversation> q = _db.Conversations.AsNoTracking()
            .Where(c => c.User1Id == me || c.User2Id == me);

        // Filtr: oxunmamış / sancılmış / sevimli (hər biri cari istifadəçinin ÖZ tərəfinə görə).
        switch ((filter ?? "all").ToLowerInvariant())
        {
            case "unread":
                q = q.Where(c => (c.User1Id == me && c.Unread1 > 0) || (c.User2Id == me && c.Unread2 > 0));
                break;
            case "pinned":
                q = q.Where(c => c.User1Id == me ? c.Pinned1 : c.Pinned2);
                break;
            case "fav":
                q = q.Where(c => c.User1Id == me ? c.Fav1 : c.Fav2);
                break;
        }

        // Axtarış: qarşı tərəfin adına görə (SQL-də tərcümə olunan şərti ifadə).
        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            q = q.Where(c => (c.User1Id == me ? c.User2.FullName : c.User1.FullName).Contains(s));
        }

        // Sıralama: əvvəl mənim sancdıqlarım, sonra ən son mesaj.
        q = q.OrderByDescending(c => c.User1Id == me ? c.Pinned1 : c.Pinned2)
             .ThenByDescending(c => c.LastMessageAt);

        int total = await q.CountAsync(ct);
        List<Conversation> items = await q
            .Include(c => c.User1).Include(c => c.User2)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        // Qarşı tərəflərin rol + mağaza məlumatını TƏK sorğu ilə yığ (N+1 yox).
        var peerIds = items.Select(c => c.User1Id == me ? c.User2Id : c.User1Id).Distinct().ToList();
        Dictionary<string, string> roleMap = await LoadRoleMapAsync(peerIds, ct);
        var storeMap = await _db.Stores.AsNoTracking()
            .Where(s => peerIds.Contains(s.OwnerId) && s.IsActive)
            .GroupBy(s => s.OwnerId)
            .Select(g => new { OwnerId = g.Key, g.OrderBy(s => s.Id).First().Name, g.OrderBy(s => s.Id).First().Slug })
            .ToDictionaryAsync(x => x.OwnerId, x => (x.Name, x.Slug), ct);

        var rows = items.Select(c =>
        {
            bool meIs1 = c.User1Id == me;
            AppUser? peer = meIs1 ? c.User2 : c.User1;
            string peerId = meIs1 ? c.User2Id : c.User1Id;
            storeMap.TryGetValue(peerId, out var store);
            return new ConversationDto
            {
                Id = c.Id,
                PeerUserId = peerId,
                PeerFullName = peer?.FullName ?? "(silinmiş istifadəçi)",
                PeerAvatarUrl = peer?.ProfileImageUrl,
                PeerRole = roleMap.GetValueOrDefault(peerId, "Buyer"),
                PeerLastSeenAt = peer?.LastSeenAt,
                PeerStoreName = store.Name,
                PeerStoreSlug = store.Slug,
                LastMessageAt = c.LastMessageAt,
                LastMessagePreview = c.LastMessagePreview,
                UnreadCount = meIs1 ? c.Unread1 : c.Unread2,
                IsPinned = meIs1 ? c.Pinned1 : c.Pinned2,
                IsMuted = meIs1 ? c.Muted1 : c.Muted2,
                IsFavorite = meIs1 ? c.Fav1 : c.Fav2,
                Note = meIs1 ? c.Note1 : c.Note2,
                Tag = meIs1 ? c.Tag1 : c.Tag2
            };
        }).ToList();

        return new Paginate<ConversationDto>
        {
            Index = page - 1, Size = pageSize, Count = total,
            Pages = (int)Math.Ceiling(total / (double)pageSize),
            Items = rows
        };
    }

    public async Task<Paginate<MessageDto>> GetMessagesAsync(int conversationId, int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        string me = RequireUserId();
        await RequireParticipantAsync(conversationId, me, ct);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Message> q = _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeletedForAll);

        int total = await q.CountAsync(ct);
        List<Message> items = await q
            .OrderByDescending(m => m.CreatedDate)
            .Include(m => m.Sender)
            .Include(m => m.Story)
            .Include(m => m.Product).ThenInclude(p => p!.Images)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .Include(m => m.Reactions)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new Paginate<MessageDto>
        {
            Index = page - 1, Size = pageSize, Count = total,
            Pages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items.Select(m => ToDto(m, me)).ToList()
        };
    }

    public async Task<ConversationDto> GetOrCreateConversationAsync(string peerUserId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        if (string.IsNullOrEmpty(peerUserId) || peerUserId == me)
            throw new BusinessException("Söhbət açmaq üçün başqa istifadəçi seç.");

        AppUser? peer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == peerUserId, ct);
        if (peer is null) throw new NotFoundException("İstifadəçi tapılmadı.");

        (string u1, string u2) = SortPair(me, peerUserId);
        Conversation? conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2, ct);

        if (conv is null)
        {
            conv = new Conversation { User1Id = u1, User2Id = u2, LastMessageAt = DateTime.UtcNow };
            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync(ct);
        }

        bool meIs1 = conv.User1Id == me;
        Dictionary<string, string> roleMap = await LoadRoleMapAsync(new List<string> { peerUserId }, ct);
        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.OwnerId == peerUserId && s.IsActive)
            .Select(s => new { s.Name, s.Slug })
            .FirstOrDefaultAsync(ct);

        return new ConversationDto
        {
            Id = conv.Id,
            PeerUserId = peer.Id,
            PeerFullName = peer.FullName,
            PeerAvatarUrl = peer.ProfileImageUrl,
            PeerRole = roleMap.GetValueOrDefault(peerUserId, "Buyer"),
            PeerLastSeenAt = peer.LastSeenAt,
            PeerStoreName = store?.Name,
            PeerStoreSlug = store?.Slug,
            LastMessageAt = conv.LastMessageAt,
            LastMessagePreview = conv.LastMessagePreview,
            UnreadCount = meIs1 ? conv.Unread1 : conv.Unread2,
            IsPinned = meIs1 ? conv.Pinned1 : conv.Pinned2,
            IsMuted = meIs1 ? conv.Muted1 : conv.Muted2,
            IsFavorite = meIs1 ? conv.Fav1 : conv.Fav2,
            Note = meIs1 ? conv.Note1 : conv.Note2,
            Tag = meIs1 ? conv.Tag1 : conv.Tag2
        };
    }

    public async Task<ConversationDetailDto> GetConversationDetailAsync(int conversationId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        Conversation conv = await _db.Conversations.AsNoTracking()
            .Include(c => c.User1).Include(c => c.User2)
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Söhbət tapılmadı.");
        if (conv.User1Id != me && conv.User2Id != me)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");

        bool meIs1 = conv.User1Id == me;
        AppUser? peer = meIs1 ? conv.User2 : conv.User1;
        string peerId = meIs1 ? conv.User2Id : conv.User1Id;

        Dictionary<string, string> roleMap = await LoadRoleMapAsync(new List<string> { peerId }, ct);
        var store = await _db.Stores.AsNoTracking()
            .Where(s => s.OwnerId == peerId && s.IsActive)
            .Select(s => new { s.Id, s.Name, s.Slug, s.ProfileImageUrl })
            .FirstOrDefaultAsync(ct);

        // Paylaşılmış şəkillər (son 12) və fayllar (son 10) — silinməmişlər.
        List<string> sharedImages = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.MessageType == MessageType.Image
                        && !m.IsDeletedForAll && !m.IsDeletedBySender && m.AttachmentUrl != null)
            .OrderByDescending(m => m.CreatedDate)
            .Select(m => m.AttachmentUrl!)
            .Take(12).ToListAsync(ct);

        List<SharedFileDto> sharedFiles = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.MessageType == MessageType.File
                        && !m.IsDeletedForAll && !m.IsDeletedBySender && m.AttachmentUrl != null)
            .OrderByDescending(m => m.CreatedDate)
            .Select(m => new SharedFileDto
            {
                Url = m.AttachmentUrl!,
                Name = m.AttachmentName ?? "fayl",
                Size = m.AttachmentSize,
                SentAt = m.CreatedDate
            })
            .Take(10).ToListAsync(ct);

        // Son paylaşılan məhsul (sağ paneldəki "Müzakirə olunan məhsul" kartı).
        Message? lastProductMsg = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.MessageType == MessageType.ProductShare
                        && !m.IsDeletedForAll && !m.IsDeletedBySender && m.ProductId != null)
            .OrderByDescending(m => m.CreatedDate)
            .Include(m => m.Product).ThenInclude(p => p!.Images)
            .FirstOrDefaultAsync(ct);

        bool blockedByMe = await _db.UserBlocks.AsNoTracking()
            .AnyAsync(b => b.BlockerId == me && b.BlockedId == peerId, ct);

        return new ConversationDetailDto
        {
            ConversationId = conv.Id,
            PeerUserId = peerId,
            PeerFullName = peer?.FullName ?? "(silinmiş istifadəçi)",
            PeerAvatarUrl = peer?.ProfileImageUrl,
            PeerRole = roleMap.GetValueOrDefault(peerId, "Buyer"),
            PeerLastSeenAt = peer?.LastSeenAt,
            PeerMemberSince = peer?.CreatedDate ?? DateTime.MinValue,
            StoreId = store?.Id,
            StoreName = store?.Name,
            StoreSlug = store?.Slug,
            StoreLogoUrl = store?.ProfileImageUrl,
            SharedImages = sharedImages,
            SharedFiles = sharedFiles,
            LastProduct = lastProductMsg?.Product is null ? null : ToProductPreview(lastProductMsg.Product),
            IsPinned = meIs1 ? conv.Pinned1 : conv.Pinned2,
            IsMuted = meIs1 ? conv.Muted1 : conv.Muted2,
            IsFavorite = meIs1 ? conv.Fav1 : conv.Fav2,
            Note = meIs1 ? conv.Note1 : conv.Note2,
            Tag = meIs1 ? conv.Tag1 : conv.Tag2,
            IsBlockedByMe = blockedByMe
        };
    }

    public async Task<List<MessageDto>> SearchMessagesAsync(int conversationId, string query, int take = 20, CancellationToken ct = default)
    {
        string me = RequireUserId();
        await RequireParticipantAsync(conversationId, me, ct);
        if (string.IsNullOrWhiteSpace(query)) return new List<MessageDto>();
        string s = query.Trim();
        take = Math.Clamp(take, 1, 50);

        List<Message> items = await _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeletedForAll && !m.IsDeletedBySender
                        && m.Body != null && m.Body.Contains(s))
            .OrderByDescending(m => m.CreatedDate)
            .Include(m => m.Sender)
            .Take(take).ToListAsync(ct);

        return items.Select(m => ToDto(m, me)).ToList();
    }

    public async Task<ProductPreviewDto?> GetProductPreviewAsync(int productId, CancellationToken ct = default)
    {
        RequireUserId();
        Product? p = await _db.Products.AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive, ct);
        return p is null ? null : ToProductPreview(p);
    }

    // ============== GÖNDƏRMƏ / REDAKTƏ / SİLMƏ ==============

    public async Task<MessageDto> SendMessageAsync(SendMessageDto dto, CancellationToken ct = default)
    {
        string me = RequireUserId();

        ValidationResult validation = await _sendValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid) throw new ValidationException(validation.Errors);
        if (dto.RecipientUserId == me)
            throw new BusinessException("Özünə mesaj göndərə bilməzsən.");

        // BLOK QAPISI: hər iki istiqamətdə blok varsa göndərmə dayanır.
        UserBlock? block = await _db.UserBlocks.AsNoTracking().FirstOrDefaultAsync(b =>
            (b.BlockerId == me && b.BlockedId == dto.RecipientUserId) ||
            (b.BlockerId == dto.RecipientUserId && b.BlockedId == me), ct);
        if (block is not null)
        {
            throw new BusinessException(block.BlockerId == me
                ? "Blokladığın istifadəçiyə mesaj göndərə bilməzsən. Əvvəl bloku götür."
                : "Bu istifadəçi ilə hazırda yazışmaq mümkün deyil.");
        }

        (string u1, string u2) = SortPair(me, dto.RecipientUserId);
        Conversation? conv = await _db.Conversations
            .FirstOrDefaultAsync(c => c.User1Id == u1 && c.User2Id == u2, ct);
        if (conv is null)
        {
            bool peerExists = await _db.Users.AnyAsync(u => u.Id == dto.RecipientUserId, ct);
            if (!peerExists) throw new NotFoundException("Alıcı istifadəçi tapılmadı.");
            conv = new Conversation { User1Id = u1, User2Id = u2, LastMessageAt = DateTime.UtcNow };
            _db.Conversations.Add(conv);
            await _db.SaveChangesAsync(ct);
        }

        // Reply-to yalnız EYNİ söhbətin mesajına ola bilər (yad istinadlar atılır).
        int? replyTo = null;
        if (dto.ReplyToMessageId is int rid && rid > 0)
        {
            bool ok = await _db.Messages.AnyAsync(m => m.Id == rid && m.ConversationId == conv.Id, ct);
            if (ok) replyTo = rid;
        }

        var msg = new Message
        {
            ConversationId = conv.Id,
            SenderId = me,
            MessageType = dto.MessageType,
            Body = dto.Body?.Trim(),
            AttachmentUrl = dto.AttachmentUrl,
            AttachmentName = dto.AttachmentName,
            AttachmentSize = dto.AttachmentSize,
            DurationSeconds = dto.MessageType == MessageType.Voice ? dto.DurationSeconds : null,
            StoryId = dto.MessageType == MessageType.StoryReply ? dto.StoryId : null,
            ProductId = dto.MessageType == MessageType.ProductShare ? dto.ProductId : null,
            ReplyToMessageId = replyTo,
            IsReadByRecipient = false
        };
        _db.Messages.Add(msg);

        // Denormalizə sahələri + qarşı tərəfin oxunmamış sayğacı.
        conv.LastMessageAt = DateTime.UtcNow;
        conv.LastMessagePreview = BuildPreview(msg);
        if (conv.User1Id == me) conv.Unread2 += 1; else conv.Unread1 += 1;
        await _db.SaveChangesAsync(ct);

        // DTO üçün əlaqələrlə yenidən yüklə.
        Message hydrated = await _db.Messages.AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.Story)
            .Include(m => m.Product).ThenInclude(p => p!.Images)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .Include(m => m.Reactions)
            .FirstAsync(m => m.Id == msg.Id, ct);
        return ToDto(hydrated, me);
    }

    public async Task<MessageDto> EditMessageAsync(int messageId, string newBody, CancellationToken ct = default)
    {
        string me = RequireUserId();
        newBody = (newBody ?? string.Empty).Trim();
        if (newBody.Length == 0) throw new BusinessException("Mesaj boş ola bilməz.");
        if (newBody.Length > 2000) throw new BusinessException("Mesaj 2000 simvoldan uzun ola bilməz.");

        Message msg = await _db.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new NotFoundException("Mesaj tapılmadı.");
        if (msg.SenderId != me) throw new BusinessException("Yalnız öz mesajını redaktə edə bilərsən.");
        if (msg.IsDeletedBySender || msg.IsDeletedForAll) throw new BusinessException("Silinmiş mesaj redaktə oluna bilməz.");
        if (msg.MessageType != MessageType.Text && string.IsNullOrEmpty(msg.Body) )
            throw new BusinessException("Bu mesaj növü redaktə oluna bilməz.");

        msg.Body = newBody;
        msg.EditedAt = DateTime.UtcNow;

        // Söhbətin SON mesajı redaktə olunubsa — siyahıdakı önizləməni də yenilə.
        bool isLast = !await _db.Messages.AnyAsync(m =>
            m.ConversationId == msg.ConversationId && m.Id > msg.Id && !m.IsDeletedForAll, ct);
        if (isLast) msg.Conversation.LastMessagePreview = BuildPreview(msg);

        await _db.SaveChangesAsync(ct);

        Message hydrated = await _db.Messages.AsNoTracking()
            .Include(m => m.Sender)
            .Include(m => m.ReplyToMessage).ThenInclude(r => r!.Sender)
            .Include(m => m.Reactions)
            .FirstAsync(m => m.Id == msg.Id, ct);
        return ToDto(hydrated, me);
    }

    public async Task<(int ConversationId, string PeerId)> DeleteMessageAsync(int messageId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        Message msg = await _db.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new NotFoundException("Mesaj tapılmadı.");
        if (msg.SenderId != me) throw new BusinessException("Yalnız öz mesajını silə bilərsən.");

        msg.IsDeletedBySender = true;

        // Söhbətin son mesajı silinibsə önizləməni yenilə.
        bool isLast = !await _db.Messages.AnyAsync(m =>
            m.ConversationId == msg.ConversationId && m.Id > msg.Id && !m.IsDeletedForAll, ct);
        if (isLast) msg.Conversation.LastMessagePreview = "🚫 Mesaj silindi";

        await _db.SaveChangesAsync(ct);

        string peerId = msg.Conversation.User1Id == me ? msg.Conversation.User2Id : msg.Conversation.User1Id;
        return (msg.ConversationId, peerId);
    }

    public async Task<(List<MessageReactionDto> Reactions, int ConversationId, string PeerId)> ReactAsync(
        int messageId, string emoji, CancellationToken ct = default)
    {
        string me = RequireUserId();
        emoji = (emoji ?? string.Empty).Trim();
        if (emoji.Length is 0 or > 16) throw new BusinessException("Yanlış reaksiya.");

        Message msg = await _db.Messages
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId, ct)
            ?? throw new NotFoundException("Mesaj tapılmadı.");
        Conversation conv = msg.Conversation;
        if (conv.User1Id != me && conv.User2Id != me)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");

        MessageReaction? existing = await _db.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == me, ct);

        if (existing is null)
        {
            _db.MessageReactions.Add(new MessageReaction { MessageId = messageId, UserId = me, Emoji = emoji });
        }
        else if (existing.Emoji == emoji)
        {
            _db.MessageReactions.Remove(existing);   // eyni emoji ikinci dəfə → reaksiya götürülür
        }
        else
        {
            existing.Emoji = emoji;                   // fərqli emoji → əvəzlənir
        }
        await _db.SaveChangesAsync(ct);

        List<MessageReactionDto> groups = await _db.MessageReactions.AsNoTracking()
            .Where(r => r.MessageId == messageId)
            .GroupBy(r => r.Emoji)
            .Select(g => new MessageReactionDto
            {
                Emoji = g.Key,
                Count = g.Count(),
                Mine = g.Any(r => r.UserId == me)
            })
            .ToListAsync(ct);

        string peerId = conv.User1Id == me ? conv.User2Id : conv.User1Id;
        return (groups, msg.ConversationId, peerId);
    }

    // ============== OXUNDU / SAYĞAC ==============

    public async Task<string?> MarkConversationReadAsync(int conversationId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        Conversation? conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conv is null) return null;
        if (conv.User1Id != me && conv.User2Id != me) return null;

        bool meIs1 = conv.User1Id == me;
        if (meIs1 && conv.Unread1 == 0) return null;
        if (!meIs1 && conv.Unread2 == 0) return null;

        string peerId = meIs1 ? conv.User2Id : conv.User1Id;
        if (meIs1) conv.Unread1 = 0; else conv.Unread2 = 0;
        await _db.SaveChangesAsync(ct);

        // Qarşı tərəfin mesajlarını TƏK çoxluq-əsaslı UPDATE ilə "oxundu" et.
        await _db.Messages
            .Where(m => m.ConversationId == conversationId && m.SenderId != me && !m.IsReadByRecipient)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsReadByRecipient, true), ct);

        return peerId;
    }

    public async Task<int> GetMyUnreadTotalAsync(CancellationToken ct = default)
    {
        string me = RequireUserId();
        return await _db.Conversations.AsNoTracking()
            .Where(c => c.User1Id == me || c.User2Id == me)
            .SumAsync(c => c.User1Id == me ? c.Unread1 : c.Unread2, ct);
    }

    // ============== SÖHBƏT PARAMETRLƏRİ ==============

    public Task<bool> TogglePinAsync(int conversationId, CancellationToken ct = default)
        => ToggleSideFlagAsync(conversationId, "pin", ct);
    public Task<bool> ToggleMuteAsync(int conversationId, CancellationToken ct = default)
        => ToggleSideFlagAsync(conversationId, "mute", ct);
    public Task<bool> ToggleFavoriteAsync(int conversationId, CancellationToken ct = default)
        => ToggleSideFlagAsync(conversationId, "fav", ct);

    // Ortaq köməkçi: cari istifadəçinin ÖZ tərəfindəki bayrağı çevir, yeni dəyəri qaytar.
    private async Task<bool> ToggleSideFlagAsync(int conversationId, string kind, CancellationToken ct)
    {
        string me = RequireUserId();
        Conversation conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Söhbət tapılmadı.");
        if (conv.User1Id != me && conv.User2Id != me)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");

        bool meIs1 = conv.User1Id == me;
        bool newValue;
        switch (kind)
        {
            case "pin":
                newValue = meIs1 ? (conv.Pinned1 = !conv.Pinned1) : (conv.Pinned2 = !conv.Pinned2);
                break;
            case "mute":
                newValue = meIs1 ? (conv.Muted1 = !conv.Muted1) : (conv.Muted2 = !conv.Muted2);
                break;
            default:
                newValue = meIs1 ? (conv.Fav1 = !conv.Fav1) : (conv.Fav2 = !conv.Fav2);
                break;
        }
        await _db.SaveChangesAsync(ct);
        return newValue;
    }

    public async Task SetNoteAsync(int conversationId, string? note, CancellationToken ct = default)
    {
        string me = RequireUserId();
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note is { Length: > 500 }) throw new BusinessException("Qeyd 500 simvoldan uzun ola bilməz.");

        Conversation conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Söhbət tapılmadı.");
        if (conv.User1Id != me && conv.User2Id != me)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");

        if (conv.User1Id == me) conv.Note1 = note; else conv.Note2 = note;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetTagAsync(int conversationId, string? tag, CancellationToken ct = default)
    {
        string me = RequireUserId();
        tag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
        if (tag is { Length: > 40 }) throw new BusinessException("Etiket 40 simvoldan uzun ola bilməz.");

        Conversation conv = await _db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct)
            ?? throw new NotFoundException("Söhbət tapılmadı.");
        if (conv.User1Id != me && conv.User2Id != me)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");

        if (conv.User1Id == me) conv.Tag1 = tag; else conv.Tag2 = tag;
        await _db.SaveChangesAsync(ct);
    }

    // ============== BLOK / ŞİKAYƏT ==============

    public async Task<bool> ToggleBlockAsync(string peerUserId, CancellationToken ct = default)
    {
        string me = RequireUserId();
        if (string.IsNullOrEmpty(peerUserId) || peerUserId == me)
            throw new BusinessException("Yanlış istifadəçi.");

        UserBlock? existing = await _db.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == me && b.BlockedId == peerUserId, ct);
        if (existing is not null)
        {
            _db.UserBlocks.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return false;   // blok götürüldü
        }

        bool peerExists = await _db.Users.AnyAsync(u => u.Id == peerUserId, ct);
        if (!peerExists) throw new NotFoundException("İstifadəçi tapılmadı.");

        _db.UserBlocks.Add(new UserBlock { BlockerId = me, BlockedId = peerUserId });
        await _db.SaveChangesAsync(ct);
        return true;        // bloklandı
    }

    public async Task ReportUserAsync(string peerUserId, int? conversationId, string reason, CancellationToken ct = default)
    {
        string me = RequireUserId();
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length < 5) throw new BusinessException("Şikayət səbəbini qısaca izah et (ən az 5 simvol).");
        if (reason.Length > 1000) throw new BusinessException("Şikayət 1000 simvoldan uzun ola bilməz.");
        if (string.IsNullOrEmpty(peerUserId) || peerUserId == me)
            throw new BusinessException("Yanlış istifadəçi.");

        // Gündə eyni istifadəçidən YALNIZ BİR şikayət (spam-qoruma).
        DateTime dayAgo = DateTime.UtcNow.AddDays(-1);
        bool dup = await _db.UserReports.AnyAsync(r =>
            r.ReporterId == me && r.ReportedUserId == peerUserId && r.CreatedDate >= dayAgo, ct);
        if (dup) throw new BusinessException("Bu istifadəçidən artıq şikayət göndərmisən. Komandamız baxır.");

        _db.UserReports.Add(new UserReport
        {
            ReporterId = me,
            ReportedUserId = peerUserId,
            ConversationId = conversationId,
            Reason = reason
        });
        await _db.SaveChangesAsync(ct);
    }

    // ============== SÜRƏTLİ CAVABLAR ==============

    public async Task<List<QuickReplyDto>> GetMyQuickRepliesAsync(CancellationToken ct = default)
    {
        string me = RequireUserId();
        return await _db.QuickReplies.AsNoTracking()
            .Where(q => q.UserId == me)
            .OrderBy(q => q.Title)
            .Select(q => new QuickReplyDto { Id = q.Id, Title = q.Title, Body = q.Body })
            .ToListAsync(ct);
    }

    public async Task<QuickReplyDto> AddQuickReplyAsync(string title, string body, CancellationToken ct = default)
    {
        string me = RequireUserId();
        title = (title ?? string.Empty).Trim();
        body = (body ?? string.Empty).Trim();
        if (title.Length is 0 or > 60) throw new BusinessException("Başlıq 1–60 simvol olmalıdır.");
        if (body.Length is 0 or > 1000) throw new BusinessException("Mətn 1–1000 simvol olmalıdır.");

        int count = await _db.QuickReplies.CountAsync(q => q.UserId == me, ct);
        if (count >= MaxQuickReplies) throw new BusinessException($"Ən çoxu {MaxQuickReplies} şablon saxlaya bilərsən.");

        var entity = new QuickReply { UserId = me, Title = title, Body = body };
        _db.QuickReplies.Add(entity);
        await _db.SaveChangesAsync(ct);
        return new QuickReplyDto { Id = entity.Id, Title = entity.Title, Body = entity.Body };
    }

    public async Task DeleteQuickReplyAsync(int id, CancellationToken ct = default)
    {
        string me = RequireUserId();
        QuickReply? q = await _db.QuickReplies.FirstOrDefaultAsync(x => x.Id == id && x.UserId == me, ct);
        if (q is null) return;
        _db.QuickReplies.Remove(q);
        await _db.SaveChangesAsync(ct);
    }

    // ============== ADMIN MODERASİYASI ==============

    public async Task<Paginate<AdminConversationRowDto>> GetAllConversationsForAdminAsync(int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Conversation> q = _db.Conversations.AsNoTracking();
        int total = await q.CountAsync(ct);
        List<Conversation> items = await q
            .OrderByDescending(c => c.LastMessageAt)
            .Include(c => c.User1).Include(c => c.User2)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        var userIds = items.SelectMany(c => new[] { c.User1Id, c.User2Id }).Distinct().ToList();
        Dictionary<string, string> roleMap = await LoadRoleMapAsync(userIds, ct);

        var convIds = items.Select(c => c.Id).ToList();
        Dictionary<int, int> countMap = await _db.Messages.AsNoTracking()
            .Where(m => convIds.Contains(m.ConversationId))
            .GroupBy(m => m.ConversationId)
            .Select(g => new { g.Key, N = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.N, ct);

        var rows = items.Select(c => new AdminConversationRowDto
        {
            Id = c.Id,
            User1Id = c.User1Id,
            User1FullName = c.User1?.FullName ?? "(silinmiş)",
            User1Role = roleMap.GetValueOrDefault(c.User1Id, "Buyer"),
            User2Id = c.User2Id,
            User2FullName = c.User2?.FullName ?? "(silinmiş)",
            User2Role = roleMap.GetValueOrDefault(c.User2Id, "Buyer"),
            LastMessageAt = c.LastMessageAt,
            LastMessagePreview = c.LastMessagePreview,
            MessageCount = countMap.GetValueOrDefault(c.Id)
        }).ToList();

        return new Paginate<AdminConversationRowDto>
        {
            Index = page - 1, Size = pageSize, Count = total,
            Pages = (int)Math.Ceiling(total / (double)pageSize),
            Items = rows
        };
    }

    public async Task<Paginate<MessageDto>> GetMessagesForAdminAsync(int conversationId, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<Message> q = _db.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId);   // admin silinmişləri də görür

        int total = await q.CountAsync(ct);
        List<Message> items = await q
            .OrderByDescending(m => m.CreatedDate)
            .Include(m => m.Sender)
            .Include(m => m.Story)
            .Include(m => m.Product).ThenInclude(p => p!.Images)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new Paginate<MessageDto>
        {
            Index = page - 1, Size = pageSize, Count = total,
            Pages = (int)Math.Ceiling(total / (double)pageSize),
            Items = items.Select(m => ToDto(m, _currentUser.UserId ?? string.Empty)).ToList()
        };
    }

    public async Task DeleteMessageForAdminAsync(int messageId, CancellationToken ct = default)
    {
        Message? m = await _db.Messages.FirstOrDefaultAsync(x => x.Id == messageId, ct);
        if (m is null) throw new NotFoundException("Mesaj tapılmadı.");
        m.IsDeletedForAll = true;
        await _db.SaveChangesAsync(ct);
    }

    // ============== KÖMƏKÇİLƏR ==============

    private string RequireUserId()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Account.NotAuthenticated);
        return _currentUser.UserId;
    }

    private async Task RequireParticipantAsync(int conversationId, string userId, CancellationToken ct)
    {
        Conversation? conv = await _db.Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (conv is null) throw new NotFoundException("Söhbət tapılmadı.");
        if (conv.User1Id != userId && conv.User2Id != userId)
            throw new BusinessException("Bu söhbətin iştirakçısı deyilsən.");
    }

    // Verilən istifadəçilərin İLK rolunu tək sorğu ilə yığır (N+1 əvəzinə).
    private async Task<Dictionary<string, string>> LoadRoleMapAsync(List<string> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return new Dictionary<string, string>();
        var pairs = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           where userIds.Contains(ur.UserId)
                           select new { ur.UserId, r.Name }).ToListAsync(ct);
        return pairs.GroupBy(p => p.UserId)
                    .ToDictionary(g => g.Key, g => g.First().Name ?? "Buyer");
    }

    // Əlifba sırası ilə cüt — (User1Id, User2Id) unikal indeksini dürüst saxlayır.
    private static (string lo, string hi) SortPair(string a, string b)
        => string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);

    // Gələnlər qutusu önizləmə mətni (mesaj növünə görə).
    private static string BuildPreview(Message m)
    {
        string raw = m.MessageType switch
        {
            MessageType.Image => "📷 Şəkil" + (string.IsNullOrEmpty(m.Body) ? "" : " · " + m.Body),
            MessageType.StoryReply => "↩ Hekayəyə cavab: " + (m.Body ?? ""),
            MessageType.ProductShare => "🛍 Məhsul" + (string.IsNullOrEmpty(m.Body) ? "" : " · " + m.Body),
            MessageType.Voice => "🎤 Səsli mesaj",
            MessageType.File => "📎 " + (m.AttachmentName ?? "Fayl"),
            _ => m.Body ?? ""
        };
        return raw.Length <= PreviewMaxLength ? raw : raw.Substring(0, PreviewMaxLength - 1) + "…";
    }

    // Cavab önizləməsi üçün qısa mətn.
    private static string ReplySnippet(Message m)
    {
        if (m.IsDeletedBySender || m.IsDeletedForAll) return "🚫 Silinmiş mesaj";
        string raw = m.MessageType switch
        {
            MessageType.Image => "📷 Şəkil",
            MessageType.Voice => "🎤 Səsli mesaj",
            MessageType.File => "📎 " + (m.AttachmentName ?? "Fayl"),
            MessageType.ProductShare => "🛍 Məhsul",
            MessageType.StoryReply => "↩ Hekayəyə cavab",
            _ => m.Body ?? ""
        };
        return raw.Length <= 80 ? raw : raw.Substring(0, 79) + "…";
    }

    private static ProductPreviewDto ToProductPreview(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Slug = p.Slug ?? string.Empty,
        ImageUrl = p.Images?.OrderByDescending(i => i.IsMain).ThenBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl,
        Price = p.Price,
        DiscountPrice = p.DiscountPrice
    };

    // Entity → DTO proyeksiyası. viewerId reaksiyalardakı "Mine" bayrağı üçün lazımdır.
    // Silinmiş mesajların məzmunu DTO-da boşaldılır (UI "Bu mesaj silindi" göstərir).
    private static MessageDto ToDto(Message m, string viewerId)
    {
        bool deleted = m.IsDeletedBySender;
        var dto = new MessageDto
        {
            Id = m.Id,
            ConversationId = m.ConversationId,
            SenderId = m.SenderId,
            SenderFullName = m.Sender?.FullName ?? "(silinmiş)",
            SenderAvatarUrl = m.Sender?.ProfileImageUrl,
            MessageType = m.MessageType,
            Body = deleted ? null : m.Body,
            AttachmentUrl = deleted ? null : m.AttachmentUrl,
            AttachmentName = deleted ? null : m.AttachmentName,
            AttachmentSize = deleted ? null : m.AttachmentSize,
            DurationSeconds = deleted ? null : m.DurationSeconds,
            ReplyToMessageId = m.ReplyToMessageId,
            ReplyToSenderName = m.ReplyToMessage?.Sender?.FullName,
            ReplyToSnippet = m.ReplyToMessage is null ? null : ReplySnippet(m.ReplyToMessage),
            EditedAt = m.EditedAt,
            IsDeletedBySender = deleted,
            IsReadByRecipient = m.IsReadByRecipient,
            CreatedDate = m.CreatedDate
        };

        if (m.Reactions is { Count: > 0 } && !deleted)
        {
            dto.Reactions = m.Reactions
                .GroupBy(r => r.Emoji)
                .Select(g => new MessageReactionDto
                {
                    Emoji = g.Key,
                    Count = g.Count(),
                    Mine = g.Any(r => r.UserId == viewerId)
                })
                .ToList();
        }

        if (m.Story is not null && !deleted)
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
        if (m.Product is not null && !deleted)
        {
            dto.ProductPreview = ToProductPreview(m.Product);
        }
        return dto;
    }
}
