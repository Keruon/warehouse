// backend/Models/ComponentType.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ComponentType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    [ForeignKey("Category")]
    [Column(TypeName = "uuid")]
    public Guid CategoryId { get; set; }
    public ComponentCategory? Category { get; set; }

    public string Kind { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Footprint { get; set; }
    
    [Column(TypeName = "component_type_enum")]
    public ComponentPackageType Type { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}