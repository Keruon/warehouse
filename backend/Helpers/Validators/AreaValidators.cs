using FluentValidation;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.Validators;

public class CreateAreaRequestValidator : AbstractValidator<CreateAreaRequest>
{
    public CreateAreaRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FloorLevel).InclusiveBetween(-5, 200);
        RuleFor(x => x.ZoneType).IsInEnum();
    }
}

public class UpdateAreaRequestValidator : AbstractValidator<UpdateAreaRequest>
{
    public UpdateAreaRequestValidator()
    {
        Include(new CreateAreaRequestValidator());
    }
}
