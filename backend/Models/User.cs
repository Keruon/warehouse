using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column(TypeName = "uuid")]
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    [Column(TypeName = "uuid")]
    public Guid? ActiveProjectLocationId { get; set; }

    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public Guid ModifiedBy { get; set; }
}
