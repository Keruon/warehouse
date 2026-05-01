namespace Storage.Helpers.DTOs;

public class CreateLocationRequest
{
    public Guid ShelfId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BinX { get; set; }
    public int BinY { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Volume { get; set; }
    public bool IsReserved { get; set; }
}

public sealed class UpdateLocationRequest : CreateLocationRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed class LocationResponse
{
    public Guid Id { get; set; }
    public Guid ShelfId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BinX { get; set; }
    public int BinY { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Volume { get; set; }
    public bool IsReserved { get; set; }
    public bool IsActive { get; set; }
}
