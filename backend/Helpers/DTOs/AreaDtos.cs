namespace Storage.Helpers.DTOs;

public class CreateAreaRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ZoneType ZoneType { get; set; }
    public int FloorLevel { get; set; }
    public string? Description { get; set; }
}

public sealed class UpdateAreaRequest : CreateAreaRequest
{
    public bool IsActive { get; set; } = true;
}

public class AreaResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public ZoneType ZoneType { get; set; }
    public int FloorLevel { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ShelfCount { get; set; }
}

public sealed class AreaDetailsResponse : AreaResponse
{
    public IReadOnlyList<ShelfResponse> Shelves { get; set; } = [];
}
