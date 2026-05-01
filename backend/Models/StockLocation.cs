// backend/Models/StockLocation.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class StockLocation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    [ForeignKey("Component")]
    [Column(TypeName = "uuid")]
    public Guid ComponentId { get; set; }
    public Component Component { get; set; }
    
    [ForeignKey("WarehouseLocation")]
    [Column(TypeName = "uuid")]
    public Guid LocationId { get; set; }
    public WarehouseLocation WarehouseLocation { get; set; }

    public int BinX { get; set; }
    public int BinY { get; set; }
    public int Quantity { get; set; } = 0;
    public string? BatchCode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}