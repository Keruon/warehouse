using FluentValidation;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.Validators;

public class ReceiveStockRequestValidator : AbstractValidator<ReceiveStockRequest>
{
    public ReceiveStockRequestValidator()
    {
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class GatherStockRequestValidator : AbstractValidator<GatherStockRequest>
{
    public GatherStockRequestValidator()
    {
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.LocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class TransferStockRequestValidator : AbstractValidator<TransferStockRequest>
{
    public TransferStockRequestValidator()
    {
        RuleFor(x => x.ComponentId).NotEmpty();
        RuleFor(x => x.FromLocationId).NotEmpty();
        RuleFor(x => x.ToLocationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.FromLocationId != x.ToLocationId)
            .WithMessage("FromLocationId and ToLocationId must be different.");
    }
}
