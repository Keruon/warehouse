namespace Storage.Helpers.DTOs;

public class CreateComponentTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public ComponentPackageType Type { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateComponentTypeRequest : CreateComponentTypeRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed class ComponentTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComponentPackageType Type { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
