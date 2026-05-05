using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.DAL.Entities;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Authentication;

public class AuthManager : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IMapper _mapper;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;

    public AuthManager(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IMapper mapper,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mapper = mapper;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<IdentityResult> RegisterAsync(RegisterDto dto)
    {
        ValidationResult validation = await _registerValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        AppUser user = _mapper.Map<AppUser>(dto);

        IdentityResult createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
            return createResult;

        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, dto.Role.ToString());
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return roleResult;
        }

        return IdentityResult.Success;
    }

    public async Task<SignInResult> LoginAsync(LoginDto dto)
    {
        ValidationResult validation = await _loginValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        AppUser? user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return SignInResult.Failed;

        return await _signInManager.PasswordSignInAsync(
            user, dto.Password, dto.RememberMe, lockoutOnFailure: false);
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();
}
