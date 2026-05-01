// backend/Models/Component.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Component
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    [ForeignKey("ComponentType")]
    [Column(TypeName = "uuid")]
    public Guid ComponentTypeId { get; set; }
    public ComponentType ComponentType { get; set; }
    
    public string? ComponentTypeName { get; set; } // Cached field
    public string PartNumber { get; set; }
    
    public string? BatchCode { get; set; }
    public int QuantityOnHand { get; set; } = 0;
    public int QuantityReserved { get; set; } = 0;
    public int QuantityCommitted { get; set; } = 0;
    public int? MinimumStockLevel { get; set; }
    public int? MaximumStockLevel { get; set; }
    public int? ReorderPoint { get; set; }
    public decimal UnitCost { get; set; }

    public Guid? SupplierId { get; set; }
    public string? SupplierCode { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierPartNumber { get; set; }
    public DateTime? LastPriceChange { get; set; }
    public int SupplierLeadTime { get; set; } = 0;
    public DateTime? LastPurchaseDate { get; set; }
    public DateTime? LastReceivedDate { get; set; }
    public DateTime? LastSoldDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}