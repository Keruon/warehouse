// backend/Models/WarehouseLocation.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class WarehouseLocation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    [ForeignKey("Shelf")]
    [Column(TypeName = "uuid")]
    public Guid ShelfId { get; set; }
    public WarehouseShelf Shelf { get; set; }

    public string Name { get; set; }
    
    public string Code { get; set; }
    public string? Description { get; set; }

    public int BinX { get; set; }
    public int BinY { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Volume { get; set; }
    public bool IsReserved { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}