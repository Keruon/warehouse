using FluentValidation;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.Validators;

public class CreateShelfRequestValidator : AbstractValidator<CreateShelfRequest>
{
    public CreateShelfRequestValidator()
    {
        RuleFor(x => x.AreaId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.WeightLimitKg).GreaterThanOrEqualTo(0).When(x => x.WeightLimitKg.HasValue);
    }
}

public class UpdateShelfRequestValidator : AbstractValidator<UpdateShelfRequest>
{
    public UpdateShelfRequestValidator()
    {
        Include(new CreateShelfRequestValidator());
    }
}
