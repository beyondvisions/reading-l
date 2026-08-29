using FluentValidation;
using ReadingList.Contracts;

namespace ReadingList.Validators;

public class BookRequestValidator : AbstractValidator<BookRequest>
{
    public BookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Author)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.PageCount)
            .GreaterThan(0);

        RuleFor(x => x.Status)
            .IsInEnum();
    }
}