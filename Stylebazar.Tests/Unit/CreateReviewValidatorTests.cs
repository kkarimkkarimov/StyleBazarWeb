using FluentValidation.TestHelper;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.Validators;
using Xunit;

namespace Stylebazar.Tests.Unit;

public class CreateReviewValidatorTests
{
    private readonly CreateReviewValidator _sut = new();

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Rating_in_range_passes(int rating)
    {
        _sut.TestValidate(new CreateReviewDto { Rating = rating })
            .ShouldNotHaveValidationErrorFor(x => x.Rating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rating_out_of_range_fails(int rating)
    {
        _sut.TestValidate(new CreateReviewDto { Rating = rating })
            .ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Comment_over_2000_chars_fails()
    {
        _sut.TestValidate(new CreateReviewDto { Rating = 5, Comment = new string('x', 2001) })
            .ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
