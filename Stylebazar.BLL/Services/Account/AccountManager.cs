using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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
