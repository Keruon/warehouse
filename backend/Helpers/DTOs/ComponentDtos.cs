namespace Storage.Helpers.DTOs;

public class CreateComponentRequest
{
    public Guid ComponentTypeId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string? BatchCode { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierPartNumber { get; set; }
    public decimal UnitCost { get; set; }
    public int? MinimumStockLevel { get; set; }
    public int? MaximumStockLevel { get; set; }
    public int? ReorderPoint { get; set; }
}

public sealed class UpdateComponentRequest : CreateComponentRequest
{
    public bool IsActive { get; set; } = true;
}

public sealed class ComponentSearchRequest : PagedQuery
{
    public Guid? TypeId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Manufacturer { get; set; }
    public string? PartNumber { get; set; }
}

public sealed class ComponentResponse
{
    public Guid Id { get; set; }
    public Guid ComponentTypeId { get; set; }
    public string? ComponentTypeName { get; set; }
    public Guid CategoryId { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string? BatchCode { get; set; }
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int QuantityCommitted { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsActive { get; set; }
}
