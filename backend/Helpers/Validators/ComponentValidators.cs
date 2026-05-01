using FluentValidation;
using Storage.Helpers.DTOs;

namespace Storage.Helpers.Validators;

public class CreateComponentRequestValidator : AbstractValidator<CreateComponentRequest>
{
    public CreateComponentRequestValidator()
    {
        RuleFor(x => x.ComponentTypeId).NotEmpty();
        RuleFor(x => x.PartNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockLevel).GreaterThanOrEqualTo(0).When(x => x.MinimumStockLevel.HasValue);
        RuleFor(x => x.MaximumStockLevel).GreaterThanOrEqualTo(0).When(x => x.MaximumStockLevel.HasValue);
        RuleFor(x => x.ReorderPoint).GreaterThanOrEqualTo(0).When(x => x.ReorderPoint.HasValue);
    }
}

public class UpdateComponentRequestValidator : AbstractValidator<UpdateComponentRequest>
{
    public UpdateComponentRequestValidator()
    {
        Include(new CreateComponentRequestValidator());
    }
}

public class CreateComponentTypeRequestValidator : AbstractValidator<CreateComponentTypeRequest>
{
    public CreateComponentTypeRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Kind).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Value).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Footprint).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.Footprint));
        RuleFor(x => x.Type).IsInEnum();
    }
}

public class UpdateComponentTypeRequestValidator : AbstractValidator<UpdateComponentTypeRequest>
{
    public UpdateComponentTypeRequestValidator()
    {
        Include(new CreateComponentTypeRequestValidator());
    }
}

public class ComponentSearchRequestValidator : AbstractValidator<ComponentSearchRequest>
{
    public ComponentSearchRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);
        RuleFor(x => x.SortDirection)
            .Must(direction => direction == null || direction.Equals("asc", StringComparison.OrdinalIgnoreCase) || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SortDirection must be 'asc' or 'desc'.");
    }
}

public class CreateComponentCategoryRequestValidator : AbstractValidator<CreateComponentCategoryRequest>
{
    public CreateComponentCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateComponentCategoryRequestValidator : AbstractValidator<UpdateComponentCategoryRequest>
{
    public UpdateComponentCategoryRequestValidator()
    {
        Include(new CreateComponentCategoryRequestValidator());
    }
}

public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        Include(new CreateSupplierRequestValidator());
    }
}
