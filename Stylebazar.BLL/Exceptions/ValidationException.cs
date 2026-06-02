using FluentValidation.Results;

namespace Stylebazar.BLL.Exceptions;

// YOXLAMA XƏTASI — daxil edilən məlumat qaydalara uyğun olmayanda atılır.
// Hansı sahədə hansı səhvlərin olduğunu Errors lüğətində saxlayır. API-də 400 (Bad Request) olur.
public class ValidationException : Exception
{
    // Açar = sahə adı, dəyər = həmin sahənin səhv mesajları.
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException()
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    // FluentValidation-ın səhvlərini sahəyə görə qruplaşdırıb Errors-a yığır.
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }
}
