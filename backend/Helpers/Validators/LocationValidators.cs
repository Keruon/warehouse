using FluentValidation;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.Validators;

public class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.ShelfId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BinX).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BinY).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Depth).GreaterThanOrEqualTo(0).When(x => x.Depth.HasValue);
        RuleFor(x => x.Width).GreaterThanOrEqualTo(0).When(x => x.Width.HasValue);
        RuleFor(x => x.Height).GreaterThanOrEqualTo(0).When(x => x.Height.HasValue);
        RuleFor(x => x.Volume).GreaterThanOrEqualTo(0).When(x => x.Volume.HasValue);
    }
}

public class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        Include(new CreateLocationRequestValidator());
    }
}
