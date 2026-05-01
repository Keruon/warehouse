// backend/Models/WarehouseShelf.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class WarehouseShelf
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    [ForeignKey("Area")]
    [Column(TypeName = "uuid")]
    public Guid AreaId { get; set; }
    public WarehouseArea Area { get; set; }

    public string Name { get; set; }
    
    public string Code { get; set; }

    public decimal? WeightLimitKg { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}