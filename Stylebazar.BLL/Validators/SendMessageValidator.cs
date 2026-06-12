using FluentValidation;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Validators;

// SendMessageValidator — FluentValidation qaydaları; müvafiq DTO-nun sahələrinin düzgünlüyünü yoxlayır.
public class SendMessageValidator : AbstractValidator<SendMessageDto>
{
    public SendMessageValidator()
    {
        RuleFor(x => x.RecipientUserId)
            .NotEmpty().WithMessage("Mesaj alanı seçilməyib.");

        RuleFor(x => x.Body)
            .MaximumLength(2000).WithMessage("Mesaj 2000 simvoldan uzun ola bilməz.");

        // Per-type guards — exactly the field combos that matter for that shape:
        //   Text         → Body required
        //   Image        → AttachmentUrl required (Body optional caption)
        //   StoryReply   → Body required + StoryId required
        //   ProductShare → ProductId required (Body optional comment)
        RuleFor(x => x.Body)
            .NotEmpty().When(x => x.MessageType == MessageType.Text)
            .WithMessage("Mətn mesajı boş ola bilməz.");

        RuleFor(x => x.AttachmentUrl)
            .NotEmpty().When(x => x.MessageType == MessageType.Image)
            .WithMessage("Şəkil mesajı üçün fayl tələb olunur.");

        RuleFor(x => x.Body)
            .NotEmpty().When(x => x.MessageType == MessageType.StoryReply)
            .WithMessage("Hekayəyə cavab mətni boş ola bilməz.");
        RuleFor(x => x.StoryId)
            .GreaterThan(0).When(x => x.MessageType == MessageType.StoryReply)
            .WithMessage("Hekayə referansı yoxdur.");

        RuleFor(x => x.ProductId)
            .GreaterThan(0).When(x => x.MessageType == MessageType.ProductShare)
            .WithMessage("Məhsul referansı yoxdur.");

        // V2: Voice → audio fayl + 1–300 saniyə uzunluq tələb olunur.
        RuleFor(x => x.AttachmentUrl)
            .NotEmpty().When(x => x.MessageType == MessageType.Voice)
            .WithMessage("Səsli mesaj üçün audio fayl tələb olunur.");
        RuleFor(x => x.DurationSeconds)
            .InclusiveBetween(1, 300).When(x => x.MessageType == MessageType.Voice)
            .WithMessage("Səsli mesaj 1 saniyədən 5 dəqiqəyə qədər ola bilər.");

        // V2: File → fayl URL-i + orijinal ad tələb olunur.
        RuleFor(x => x.AttachmentUrl)
            .NotEmpty().When(x => x.MessageType == MessageType.File)
            .WithMessage("Fayl mesajı üçün fayl tələb olunur.");
        RuleFor(x => x.AttachmentName)
            .NotEmpty().When(x => x.MessageType == MessageType.File)
            .WithMessage("Faylın adı yoxdur.");
    }
}
