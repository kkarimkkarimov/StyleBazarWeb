using Core.Persistence.Paging;
using Microsoft.AspNetCore.Http;
using Stylebazar.BLL.DTOs.Account;

namespace Stylebazar.BLL.Services.Account;

public interface IAccountService
{
    /// <summary>
    /// Read the current user's full account profile (name, email, phone, bio,
    /// avatar, customer + vendor short codes, roles). Used by the Profile page
    /// and the sidebar avatar block.
    /// </summary>
    Task<AccountProfileDto> GetMineAsync(CancellationToken ct = default);

    /// <summary>Update name / phone / bio. Returns the refreshed DTO.</summary>
    Task<AccountProfileDto> UpdateProfileAsync(UpdateProfileDto dto, CancellationToken ct = default);

    /// <summary>Change the current user's password. Validates current first.</summary>
    Task ChangePasswordAsync(ChangePasswordDto dto, CancellationToken ct = default);

    /// <summary>Upload a new avatar. Replaces the previous URL; old file is best-effort deleted.</summary>
    Task<string> UploadAvatarAsync(IFormFile file, CancellationToken ct = default);

    /// <summary>Removes the avatar (sets ProfileImageUrl = null + deletes file).</summary>
    Task RemoveAvatarAsync(CancellationToken ct = default);

    // ---- Admin-scoped operations ----

    /// <summary>Paginated user list with role/status filter + search. Admin only.</summary>
    Task<Paginate<UserAdminDto>> GetUsersAsync(UserAdminListFilter filter, CancellationToken ct = default);

    /// <summary>Loads a single user by id with admin-level detail.</summary>
    Task<UserAdminDto?> GetUserByIdAsync(string userId, CancellationToken ct = default);

    /// <summary>Replaces the user's role with the new one (drops all previous roles).</summary>
    Task SetRoleAsync(string userId, string newRole, CancellationToken ct = default);

    /// <summary>
    /// Toggle the user's lockout. Suspended → LockoutEnd set to year 9999;
    /// active → LockoutEnd set to null. Returns the new state.
    /// </summary>
    Task<bool> ToggleSuspensionAsync(string userId, CancellationToken ct = default);

    /// <summary>Hard delete a user. Admin-only, cannot delete themselves.</summary>
    Task DeleteUserAsync(string userId, CancellationToken ct = default);
}
