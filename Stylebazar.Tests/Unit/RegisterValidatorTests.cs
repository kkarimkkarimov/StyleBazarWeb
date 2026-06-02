using FluentValidation.TestHelper;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Validators;
using Stylebazar.DAL.Entities.Enums;
using Xunit;

namespace Stylebazar.Tests.Unit;

/// <summary>
/// Unit tests for the register-shape rules. Pure logic — no DB, no DI — so they
/// run in milliseconds everywhere. The validator is the single source of truth
/// for sign-up validation, so these lock its contract down.
/// </summary>
public class RegisterValidatorTests
{
    private readonly RegisterValidator _sut = new();

    private static RegisterDto Valid() => new()
    {
        FullName = "Test İstifadəçi",
        Email = "user@stylebazar.az",
        Password = "parol1",        // ≥6, has lowercase + digit
        ConfirmPassword = "parol1",
        Role = UserRole.Buyer
    };

    [Fact]
    public void Valid_dto_passes()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]      // empty
    [InlineData("   ")]   // whitespace only
    [InlineData("A")]     // < 2 chars after trim
    public void FullName_invalid_fails(string fullName)
    {
        var dto = Valid();
        dto.FullName = fullName;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Theory]
    [InlineData("")]             // empty
    [InlineData("not-an-email")] // no @
    public void Email_invalid_fails(string email)
    {
        var dto = Valid();
        dto.Email = email;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("abc")]     // too short (< 6)
    [InlineData("123456")]  // no lowercase letter
    [InlineData("parolll")] // no digit
    public void Password_not_meeting_policy_fails(string password)
    {
        var dto = Valid();
        dto.Password = password;
        dto.ConfirmPassword = password;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ConfirmPassword_mismatch_fails()
    {
        var dto = Valid();
        dto.ConfirmPassword = "different1";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void Admin_role_is_rejected()
    {
        var dto = Valid();
        dto.Role = UserRole.Admin;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Theory]
    [InlineData(UserRole.Buyer)]
    [InlineData(UserRole.Seller)]
    public void Buyer_and_seller_roles_pass(UserRole role)
    {
        var dto = Valid();
        dto.Role = role;
        _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Role);
    }
}
