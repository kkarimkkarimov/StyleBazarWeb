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
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Account;

public class AccountManager : IAccountService
{
    private const string AvatarSubfolder = "avatars";

    private readonly UserManager<AppUser> _users;
    private readonly IStoreRepository _stores;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _files;
    private readonly IValidator<UpdateProfileDto> _profileValidator;
    private readonly IValidator<ChangePasswordDto> _passwordValidator;

    public AccountManager(
        UserManager<AppUser> users,
        IStoreRepository stores,
        ICurrentUserService currentUser,
        IFileStorageService files,
        IValidator<UpdateProfileDto> profileValidator,
        IValidator<ChangePasswordDto> passwordValidator)
    {
        _users = users;
        _stores = stores;
        _currentUser = currentUser;
        _files = files;
        _profileValidator = profileValidator;
        _passwordValidator = passwordValidator;
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

            int storeCount = await _stores.GetListAsync(
                predicate: s => s.OwnerId == u.Id,
                orderBy: x => x.OrderBy(s => s.Id),
                index: 0, size: 1, enableTracking: false, cancellationToken: ct).ContinueWith(t => t.Result.Count, ct);

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

        IdentityResult res = await _users.DeleteAsync(u);
        if (!res.Succeeded)
            throw new BusinessException(string.Join(" • ", res.Errors.Select(e => e.Description)));
    }

    // -------- helpers --------

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
