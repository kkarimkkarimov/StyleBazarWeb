using FluentValidation.TestHelper;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.Validators;
using Xunit;

namespace Stylebazar.Tests.Unit;

public class LoginValidatorTests
{
    private readonly LoginValidator _sut = new();

    private static LoginDto Valid() => new()
    {
        Email = "user@stylebazar.az",
        Password = "parol1"
    };

    [Fact]
    public void Valid_dto_passes()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    public void Bad_email_fails(string email)
    {
        var dto = Valid();
        dto.Email = email;
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_password_fails()
    {
        var dto = Valid();
        dto.Password = "";
        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Password);
    }
}
