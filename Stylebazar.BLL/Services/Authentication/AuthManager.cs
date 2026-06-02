using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Authentication;

// AuthManager — xidmətin GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı). Yoxlama → repository/baza → nəticə axını burada idarə olunur.
public class AuthManager : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthManager(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        IMapper mapper,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _mapper = mapper;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<RegisterResultDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        // ---- 1. DTO shape validation ----------------------------------
        // Az-message FluentValidation rules — same language as the UI.
        ValidationResult validation = await _registerValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return new RegisterResultDto
            {
                Succeeded = false,
                FieldErrors = validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            };
        }

        // ---- 2. Identity create ---------------------------------------
        AppUser user = _mapper.Map<AppUser>(dto);
        IdentityResult createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return BuildErrorResult(createResult);

        // ---- 3. Assign role -------------------------------------------
        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, dto.Role.ToString());
        if (!roleResult.Succeeded)
        {
            // Compensating delete — never leave a user without a role.
            await _userManager.DeleteAsync(user);
            return BuildErrorResult(roleResult);
        }

        // ---- 4. Success -----------------------------------------------
        string firstName = string.IsNullOrWhiteSpace(dto.FullName)
            ? "stilist"
            : dto.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "stilist";

        return new RegisterResultDto
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email,
            FirstName = firstName
        };
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto dto)
    {
        ValidationResult validation = await _loginValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        AppUser? user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            throw new UnauthorizedException("Email və ya parol səhvdir.");

        bool passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
            throw new UnauthorizedException("Email və ya parol səhvdir.");

        IList<string> roles = await _userManager.GetRolesAsync(user);
        (string token, DateTime expiresAt) = _tokenService.CreateToken(user, roles);

        return new LoginResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();

    // ---------- helpers ----------

    /// <summary>
    /// Translates a failed IdentityResult into our DTO shape. Errors are
    /// routed to the most relevant field where the code identifies one —
    /// password rule violations go to "Password", duplicate-email to "Email",
    /// everything else lands as a form-level error.
    /// </summary>
    private static RegisterResultDto BuildErrorResult(IdentityResult result)
    {
        Dictionary<string, List<string>> byField = new();
        List<string> general = new();

        foreach (IdentityError err in result.Errors)
        {
            string az = IdentityErrorTranslator.Translate(err);
            string? field = err.Code switch
            {
                "DuplicateUserName" or "DuplicateEmail" or "InvalidEmail" => "Email",
                "PasswordTooShort" or "PasswordRequiresUniqueChars"
                    or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresDigit"
                    or "PasswordRequiresLower" or "PasswordRequiresUpper" => "Password",
                _ => null
            };

            if (field is null)
                general.Add(az);
            else
            {
                if (!byField.TryGetValue(field, out var list))
                    byField[field] = list = new List<string>();
                if (!list.Contains(az)) list.Add(az);
            }
        }

        return new RegisterResultDto
        {
            Succeeded = false,
            FieldErrors = byField.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()),
            GeneralError = general.Count == 0 ? null : string.Join(" • ", general.Distinct())
        };
    }
}
