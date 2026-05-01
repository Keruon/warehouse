namespace Storage.Helpers.DTOs;

public class CreateComponentTypeRequest
{
    public Guid CategoryId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Footprint { get; set; }
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
    public Guid CategoryId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Footprint { get; set; }
    public ComponentPackageType Type { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
