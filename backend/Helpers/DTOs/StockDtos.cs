namespace Storage.Helpers.DTOs;

public sealed class ReceiveStockRequest
{
    public Guid ComponentId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public string? BatchCode { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public sealed class GatherStockRequest
{
    public Guid ComponentId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
}

public sealed class TransferStockRequest
{
    public Guid ComponentId { get; set; }
    public Guid FromLocationId { get; set; }
    public Guid ToLocationId { get; set; }
    public int Quantity { get; set; }
}

public sealed class StockLevelResponse
{
    public Guid ComponentId { get; set; }
    public Guid LocationId { get; set; }
    public int Quantity { get; set; }
    public string? BatchCode { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
