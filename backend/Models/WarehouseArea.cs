// backend/Models/WarehouseArea.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class WarehouseArea
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Or Identity if using a library, but SQL uses gen_random_uuid()
    [Column(TypeName = "uuid")] // Explicitly name the column as Id in SQL
    public Guid Id { get; set; }

    public string Name { get; set; }
    
    public string Code { get; set; }

    [Column(TypeName = "text")]
    public ZoneType ZoneType { get; set; }

    public int FloorLevel { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}