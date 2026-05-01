namespace Storage.Helpers.DTOs;

public class CreateShelfRequest
{
    public Guid AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal? WeightLimitKg { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateShelfRequest : CreateShelfRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed class ShelfResponse
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal? WeightLimitKg { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
