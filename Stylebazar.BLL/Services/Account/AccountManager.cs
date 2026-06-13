using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.Constants;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.CurrentUser;
using Stylebazar.BLL.Services.FileStorage;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Account;

// AccountManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class AccountManager : IAccountService
{
    private const string AvatarSubfolder = "avatars";

    private readonly UserManager<AppUser> _users;
    private readonly IStoreRepository _stores;
    private readonly ISubscriptionRepository _subs;
    private readonly IWishlistRepository _wishlist;
    private readonly IReviewRepository _reviews;
    private readonly IProductRepository _products;
    private readonly IStoryRepository _stories;
    private readonly IContactClickRepository _contactClicks;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly IValidator<UpdateProfileDto> _profileValidator;
    private readonly IValidator<ChangePasswordDto> _passwordValidator;
    // Messaging V2 + sifariş entity-lərinin Restrict FK-lərini set-based təmizləmək üçün
    // birbaşa DbContext (bu entity-lərin ayrıca repository-si yoxdur).
    private readonly ApplicationDbContext _db;

    public AccountManager(
        UserManager<AppUser> users,
        IStoreRepository stores,
        ISubscriptionRepository subs,
        IWishlistRepository wishlist,
        IReviewRepository reviews,
        IProductRepository products,
        IStoryRepository stories,
        IContactClickRepository contactClicks,
        ICurrentUserService currentUser,
        IFileStorageService files,
        IValidator<UpdateProfileDto> profileValidator,
        IValidator<ChangePasswordDto> passwordValidator,
        ApplicationDbContext db)
    {
        _users = users;
        _stores = stores;
        _subs = subs;
        _wishlist = wishlist;
        _reviews = reviews;
        _products = products;
        _stories = stories;
        _contactClicks = contactClicks;
        _currentUser = currentUser;
        _files = files;
        _profileValidator = profileValidator;
        _passwordValidator = passwordValidator;
        _db = db;
    }

    public async Task<AccountProfileDto> GetMineAsync(CancellationToken ct = default)
    {
        AppUser user = await RequireUserAsync();
        return await ToDtoAsync(user, ct);
    }

    public async Task<AccountProfileDto> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _profileValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        AppUser user = await RequireUserAsync();
        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.Bio = string.IsNullOrWhiteSpace(dto.Bio) ? null : dto.Bio.Trim();

        IdentityResult result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessException(string.Join(" • ", result.Errors.Select(e => e.Description)));

        return await ToDtoAsync(user, ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct = default)
    {
        ValidationResult v = await _passwordValidator.ValidateAsync(dto, ct);
        if (!v.IsValid) throw new ValidationException(v.Errors);

        AppUser user = await RequireUserAsync();
        IdentityResult result = await _users.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new BusinessException(string.Join(" • ", result.Errors.Select(e => e.Description)));
    }

    public async Task<string> UploadAvatarAsync(IFormFile file, CancellationToken ct = default)
    {
        AppUser user = await RequireUserAsync();

        string? previous = user.ProfileImageUrl;
        string url = await _files.SaveAsync(file, AvatarSubfolder, ct);

        user.ProfileImageUrl = url;
        IdentityResult result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BusinessException(string.Join(" • ", result.Errors.Select(e => e.Description)));

        // Best-effort delete of the prior avatar — never blocks the upload.
        if (!string.IsNullOrEmpty(previous))
            await _files.DeleteAsync(previous, ct);

        return url;
    }

    public async Task RemoveAvatarAsync(CancellationToken ct = default)
    {
        AppUser user = await RequireUserAsync();
        string? previous = user.ProfileImageUrl;
        if (string.IsNullOrEmpty(previous)) return;

        user.ProfileImageUrl = null;
        await _users.UpdateAsync(user);
        await _files.DeleteAsync(previous, ct);
    }

    // -------- admin --------

    public async Task<Paginate<UserAdminDto>> GetUsersAsync(UserAdminListFilter filter, CancellationToken ct = default)
    {
        IQueryable<AppUser> q = _users.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            string s = filter.Search.Trim().ToLowerInvariant();
            q = q.Where(u =>
                (u.FullName ?? "").ToLower().Contains(s) ||
                (u.Email ?? "").ToLower().Contains(s));
        }

        if (filter.Status == "active")
            q = q.Where(u => u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow);
        else if (filter.Status == "suspended")
            q = q.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow);

        int total = await q.CountAsync(ct);

        // Page first, then enrich with role lookup (per-user — UserManager has no batch API).
        List<AppUser> pageItems = await q
            .OrderByDescending(u => u.CreatedDate)
            .Skip((Math.Max(filter.Page, 1) - 1) * Math.Max(filter.PageSize, 1))
            .Take(filter.PageSize)
            .ToListAsync(ct);

        var items = new List<UserAdminDto>(pageItems.Count);
        foreach (AppUser u in pageItems)
        {
            IList<string> roles = await _users.GetRolesAsync(u);
            string role = roles.FirstOrDefault() ?? "Buyer";

            // Role filter is applied AFTER projection because Identity-roles is
            // an out-of-band join — done here keeps the query simple at the cost
            // of an over-fetch that's irrelevant at our scale (<10k users).
            if (!string.IsNullOrWhiteSpace(filter.Role) && !string.Equals(role, filter.Role, StringComparison.OrdinalIgnoreCase))
                continue;

            Paginate<Store> storePage = await _stores.GetListAsync(
                predicate: s => s.OwnerId == u.Id,
                orderBy: x => x.OrderBy(s => s.Id),
                index: 0, size: 1, enableTracking: false, cancellationToken: ct);
            int storeCount = storePage.Count;

            bool suspended = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow;
            items.Add(new UserAdminDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? string.Empty,
                PhoneNumber = u.PhoneNumber,
                ProfileImageUrl = u.ProfileImageUrl,
                CreatedDate = u.CreatedDate,
                EmailConfirmed = u.EmailConfirmed,
                Role = role,
                IsSuspended = suspended,
                LockoutEnd = u.LockoutEnd?.UtcDateTime,
                StoreCount = storeCount
            });
        }

        return new Paginate<UserAdminDto>
        {
            Index = Math.Max(filter.Page, 1) - 1,
            Size = filter.PageSize,
            Count = total,
            Pages = filter.PageSize > 0 ? (int)Math.Ceiling(total / (double)filter.PageSize) : 0,
            Items = items
        };
    }

    public async Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken ct = default)
    {
        AppUser? u = await _users.FindByIdAsync(userId);
        if (u is null) return null;

        IList<string> roles = await _users.GetRolesAsync(u);
        Paginate<Store> stores = await _stores.GetListAsync(
            predicate: s => s.OwnerId == u.Id,
            orderBy: x => x.OrderBy(s => s.Id),
            index: 0, size: 1, enableTracking: false, cancellationToken: ct);

        return new UserAdminDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email ?? string.Empty,
            PhoneNumber = u.PhoneNumber,
            ProfileImageUrl = u.ProfileImageUrl,
            CreatedDate = u.CreatedDate,
            EmailConfirmed = u.EmailConfirmed,
            Role = roles.FirstOrDefault() ?? "Buyer",
            IsSuspended = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow,
            LockoutEnd = u.LockoutEnd?.UtcDateTime,
            StoreCount = stores.Count
        };
    }

    public async Task SetRoleAsync(string userId, string newRole, CancellationToken ct = default)
    {
        AppUser? u = await _users.FindByIdAsync(userId);
        if (u is null) throw new NotFoundException(Messages.Account.NotFound);

        IList<string> roles = await _users.GetRolesAsync(u);
        if (roles.Any())
        {
            IdentityResult rem = await _users.RemoveFromRolesAsync(u, roles);
            if (!rem.Succeeded)
                throw new BusinessException(string.Join(" • ", rem.Errors.Select(e => e.Description)));
        }
        IdentityResult add = await _users.AddToRoleAsync(u, newRole);
        if (!add.Succeeded)
            throw new BusinessException(string.Join(" • ", add.Errors.Select(e => e.Description)));
    }

    public async Task<bool> ToggleSuspensionAsync(string userId, CancellationToken ct = default)
    {
        AppUser? u = await _users.FindByIdAsync(userId);
        if (u is null) throw new NotFoundException(Messages.Account.NotFound);

        bool currentlySuspended = u.LockoutEnd != null && u.LockoutEnd > DateTime.UtcNow;

        if (!await _users.GetLockoutEnabledAsync(u))
            await _users.SetLockoutEnabledAsync(u, true);

        if (currentlySuspended)
        {
            await _users.SetLockoutEndDateAsync(u, null);
            return false;
        }
        else
        {
            await _users.SetLockoutEndDateAsync(u, DateTimeOffset.MaxValue);
            return true;
        }
    }

    public async Task DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        // Guard: admin cannot delete themselves.
        if (userId == _currentUser.UserId)
            throw new BusinessException("Öz hesabınızı buradan silə bilməzsiniz.");

        AppUser? u = await _users.FindByIdAsync(userId);
        if (u is null) throw new NotFoundException(Messages.Account.NotFound);

        // ---------- BLOCKER: stores ----------
        // Store.OwnerId FK is Restrict. We have to consider BOTH live and
        // soft-deleted (IsDeleted=true) stores because the FK row sits in SQL
        // regardless of the app-level soft-delete flag.
        //
        // Strategy:
        //   - LIVE stores (IsDeleted=false): block delete and tell admin to
        //     remove them first (so they get the "store has products" guard
        //     from StoreService and can react properly).
        //   - SOFT-DELETED stores (IsDeleted=true): hard-purge them here.
        //     The admin already clicked "Sil" on them — they don't need to
        //     hunt down phantom rows the user can't see.
        Paginate<Store> allStores = await _stores.GetListAsync(
            predicate: s => s.OwnerId == userId,
            orderBy: q => q.OrderBy(s => s.Id),
            index: 0, size: 1000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);

        int liveStoreCount = allStores.Items.Count(s => s.DeletedDate == null);
        if (liveStoreCount > 0)
        {
            throw new BusinessException(
                $"Bu istifadəçinin {liveStoreCount} mağazası var. Əvvəlcə mağazaları silin (mağazalar boş olmalıdır).");
        }

        foreach (Store s in allStores.Items.Where(s => s.DeletedDate != null))
        {
            await HardPurgeStoreAsync(s, ct);
        }

        // ---------- CLEANUP: subscriptions ----------
        // Subscription.UserId FK is Restrict (we kept history intentionally).
        // Drop the user's history; null out admin-approval refs so OTHER users'
        // history stays intact.
        Paginate<Subscription> ownedSubs = await _subs.GetListAsync(
            predicate: s => s.UserId == userId,
            orderBy: q => q.OrderBy(s => s.Id),
            index: 0, size: 1000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);
        foreach (Subscription s in ownedSubs.Items)
        {
            // permanent: true → bypass soft-delete filter so FK actually releases.
            await _subs.DeleteAsync(s, permanent: true, cancellationToken: ct);
        }

        Paginate<Subscription> approvedSubs = await _subs.GetListAsync(
            predicate: s => s.ApprovedByAdminId == userId,
            orderBy: q => q.OrderBy(s => s.Id),
            index: 0, size: 1000,
            withDeleted: true,
            enableTracking: true,
            cancellationToken: ct);
        foreach (Subscription s in approvedSubs.Items)
        {
            s.ApprovedByAdminId = null;
            await _subs.UpdateAsync(s, ct);
        }

        // ---------- CLEANUP: wishlist ----------
        // Wishlist.UserId FK is Restrict. Wishlist items are personal — no audit
        // value to preserve, so cascade-delete them with the user. Fully-qualified
        // because `Stylebazar.BLL.Services.Wishlist` is a sibling namespace that
        // would otherwise shadow the entity type name.
        Paginate<Stylebazar.DAL.Entities.Wishlist> wishItems = await _wishlist.GetListAsync(
            predicate: w => w.UserId == userId,
            orderBy: q => q.OrderBy(w => w.Id),
            index: 0, size: 5000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);
        foreach (var w in wishItems.Items)
            await _wishlist.DeleteAsync(w, permanent: true, cancellationToken: ct);

        // ---------- CLEANUP: reviews ----------
        // Review.UserId FK is Restrict. Deleting reviews loses some store-rating
        // history but keeps the schema sound; reasonable trade-off because the
        // alternative is forbidding user-delete entirely for anyone who ever
        // reviewed something.
        Paginate<Stylebazar.DAL.Entities.Review> userReviews = await _reviews.GetListAsync(
            predicate: r => r.UserId == userId,
            orderBy: q => q.OrderBy(r => r.Id),
            index: 0, size: 5000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);
        foreach (var r in userReviews.Items)
            await _reviews.DeleteAsync(r, permanent: true, cancellationToken: ct);

        // ---------- CLEANUP: messaging (V2) ----------
        // Conversation.User1/User2, Message.Sender, MessageReaction.User, UserBlock.*,
        // UserReport.* — hamısı Restrict FK-dir. İstifadəçi silinməzdən əvvəl bunları
        // çoxluq-əsaslı (set-based) silirik. ExecuteDelete dərhal SQL-ə yazır (SaveChanges
        // gözləmir), ona görə son addımda Identity DELETE-i artıq sərbəstdir.
        List<int> convIds = await _db.Conversations.IgnoreQueryFilters()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (convIds.Count > 0)
        {
            // Reply-to özünə-istinad NO ACTION-dur — kaskad silmədə "same table reference"
            // konfliktini önləmək üçün əvvəlcə bu istinadları null edirik.
            await _db.Messages.IgnoreQueryFilters()
                .Where(m => convIds.Contains(m.ConversationId))
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReplyToMessageId, (int?)null), ct);
        }

        // İstifadəçinin qoyduğu reaksiyalar (Restrict) — söhbətlər silinəndə mesaj reaksiyaları
        // onsuz da kaskadla gedir, amma istifadəçiyə bağlı olanları açıq silirik.
        await _db.MessageReactions.IgnoreQueryFilters()
            .Where(r => r.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // Söhbətləri sil → DB mesajları və onların reaksiyalarını kaskadla təmizləyir.
        await _db.Conversations.IgnoreQueryFilters()
            .Where(c => c.User1Id == userId || c.User2Id == userId)
            .ExecuteDeleteAsync(ct);

        // Təhlükəsizlik üçün: qalan istənilən mesaj (göndərən bu istifadəçidir) silinsin.
        await _db.Messages.IgnoreQueryFilters()
            .Where(m => m.SenderId == userId)
            .ExecuteDeleteAsync(ct);

        // Bloklar + şikayətlər (hər iki istiqamətdə Restrict).
        await _db.UserBlocks.IgnoreQueryFilters()
            .Where(b => b.BlockerId == userId || b.BlockedId == userId)
            .ExecuteDeleteAsync(ct);
        await _db.UserReports.IgnoreQueryFilters()
            .Where(r => r.ReporterId == userId || r.ReportedUserId == userId)
            .ExecuteDeleteAsync(ct);

        // Sürətli cavab şablonları (User FK Cascade-dir, amma açıq silmək də zərərsizdir).
        await _db.QuickReplies.IgnoreQueryFilters()
            .Where(q => q.UserId == userId)
            .ExecuteDeleteAsync(ct);

        // ---------- CLEANUP: orders (alıcı) ----------
        // Order.Buyer FK Restrict; OrderItems Order-dən DB kaskadı ilə silinir.
        await _db.Orders.IgnoreQueryFilters()
            .Where(o => o.BuyerId == userId)
            .ExecuteDeleteAsync(ct);

        // ---------- FINAL: delete the user ----------
        // Identity will cascade Addresses, LoginEvents, StoreFollowers, AspNetUserRoles,
        // AspNetUserClaims, AspNetUserLogins, AspNetUserTokens. ContactClick.UserId
        // is SetNull so admin-side aggregate stats survive.
        IdentityResult res = await _users.DeleteAsync(u);
        if (!res.Succeeded)
            throw new BusinessException(string.Join(" • ", res.Errors.Select(e => e.Description)));
    }

    // -------- helpers --------

    /// <summary>
    /// Hard-purges a Store + every row that FK to it. Order matters because
    /// SQL won't release a parent row while any child still references it:
    ///
    ///   1. ContactClicks — both StoreId AND ProductId are Restrict, so we
    ///      delete clicks for the store AND for each of the store's products
    ///      FIRST (before we touch products).
    ///   2. Products — Product.StoreId is Restrict. Cascade kills child
    ///      ProductImages / ProductColors / ProductSizes automatically.
    ///   3. Stories — StoreId is Cascade per config but we hit them
    ///      explicitly to defuse the soft-delete-phantom trap too.
    ///   4. Finally, the Store itself.
    ///
    /// withDeleted: true everywhere because the query filter hides soft-deleted
    /// rows from app code, but SQL still sees their FK to the parent.
    /// </summary>
    private async Task HardPurgeStoreAsync(Store store, CancellationToken ct)
    {
        // Find all products owned by this store first — we need their IDs
        // both to delete the products and to drop any ContactClicks that
        // FK to them via ProductId.
        Paginate<Product> products = await _products.GetListAsync(
            predicate: p => p.StoreId == store.Id,
            orderBy: q => q.OrderBy(p => p.Id),
            index: 0, size: 5000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);
        var productIds = products.Items.Select(p => (int?)p.Id).ToList();

        // ---- ContactClicks ----
        // ProductId can be null (clicks on the store itself), so we use the
        // explicit-list predicate. EF Core supports Contains over a List<int?>.
        Paginate<Stylebazar.DAL.Entities.ContactClick> clicks = await _contactClicks.GetListAsync(
            predicate: c => c.StoreId == store.Id
                            || (c.ProductId.HasValue && productIds.Contains(c.ProductId)),
            orderBy: q => q.OrderBy(c => c.Id),
            index: 0, size: 50000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);
        foreach (var c in clicks.Items)
            await _contactClicks.DeleteAsync(c, permanent: true, cancellationToken: ct);

        // ---- OrderItems ----
        // OrderItem.StoreId VƏ OrderItem.ProductId hər ikisi Restrict FK-dir. Bu mağazanın
        // məhsulları hansısa sifarişdə varsa, məhsulu silmək olmaz. Ona görə əvvəlcə bu
        // mağazaya aid sifariş sətirlərini set-based silirik (alıcının sifariş başlığı qalır;
        // çox-satıcılı sifarişdə yalnız bu satıcının sətri çıxır).
        await _db.OrderItems.IgnoreQueryFilters()
            .Where(oi => oi.StoreId == store.Id)
            .ExecuteDeleteAsync(ct);

        // ---- Products ----
        foreach (Product p in products.Items)
            await _products.DeleteAsync(p, permanent: true, cancellationToken: ct);

        // ---- Stories ----
        Paginate<Stylebazar.DAL.Entities.Story> stories = await _stories.GetListAsync(
            predicate: x => x.StoreId == store.Id,
            orderBy: q => q.OrderBy(x => x.Id),
            index: 0, size: 5000,
            withDeleted: true,
            enableTracking: false,
            cancellationToken: ct);
        foreach (var s in stories.Items)
            await _stories.DeleteAsync(s, permanent: true, cancellationToken: ct);

        // ---- Finally, the Store row itself ----
        await _stores.DeleteAsync(store, permanent: true, cancellationToken: ct);
    }

    private async Task<AppUser> RequireUserAsync()
    {
        if (string.IsNullOrEmpty(_currentUser.UserId))
            throw new UnauthorizedException(Messages.Account.NotAuthenticated);

        AppUser? user = await _users.FindByIdAsync(_currentUser.UserId);
        if (user is null) throw new NotFoundException(Messages.Account.NotFound);
        return user;
    }

    private async Task<AccountProfileDto> ToDtoAsync(AppUser user, CancellationToken ct)
    {
        // Stable short ids: take first 8 chars of GUID, uppercase
        string customerCode = "usr_" + user.Id.Replace("-", "").Substring(0, 8).ToUpperInvariant();

        // Vendor code only when the user owns at least one store.
        string? vendorCode = null;
        Paginate<Store> stores = await _stores.GetListAsync(
            predicate: s => s.OwnerId == user.Id,
            orderBy: q => q.OrderBy(s => s.CreatedDate),
            index: 0, size: 1,
            enableTracking: false, cancellationToken: ct);
        Store? primary = stores.Items.FirstOrDefault();
        if (primary is not null)
        {
            // 8-char base36-ish from the store id + user hash for stability
            string seed = (primary.Id + primary.OwnerId.GetHashCode()).ToString("X").PadLeft(8, '0');
            vendorCode = "vnd_" + seed.Substring(0, Math.Min(8, seed.Length)).ToUpperInvariant();
        }

        IList<string> roles = await _users.GetRolesAsync(user);

        return new AccountProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedDate = user.CreatedDate,
            CustomerCode = customerCode,
            VendorCode = vendorCode,
            Roles = roles.ToList(),
            IsEmailConfirmed = user.EmailConfirmed
        };
    }
}
