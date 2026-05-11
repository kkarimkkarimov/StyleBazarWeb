using FluentValidation;
using Stylebazar.BLL.DTOs.Contact;

namespace Stylebazar.BLL.Validators;

public class CreateContactMessageValidator : AbstractValidator<CreateContactMessageDto>
{
    public CreateContactMessageValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(5).MaximumLength(4000);
    }
}
