namespace Storage.Helpers.DTOs;

public class CreateComponentCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateComponentCategoryRequest : CreateComponentCategoryRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed class ComponentCategoryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}