// backend/Models/ComponentCategory.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ComponentCategory
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    public string Name { get; set; }
    
    [ForeignKey("Parent")]
    [Column(TypeName = "uuid")]
    public Guid? ParentId { get; set; }
    public ComponentCategory? Parent { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}