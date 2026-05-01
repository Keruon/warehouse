using FluentValidation;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.Validators;

public class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);
        RuleFor(x => x.SortDirection)
            .Must(direction => direction == null || direction.Equals("asc", StringComparison.OrdinalIgnoreCase) || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
    }
}
