namespace Storage.Data.Models
{
    public class Area
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<Shelf> Shelves { get; set; }
    }

    public class Shelf
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int AreaId { get; set; }
        public string ShelfCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Area? Area { get; set; }
        public virtual ICollection<Location> Locations { get; set; }
    }

    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ShelfId { get; set; }
        public int ItemCount { get; set; }
        public double TotalValue { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Shelf? Shelf { get; set; }
        public virtual ICollection<Item> Items { get; set; }
    }

    public class Item
    {
        public int Id { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int LocationId { get; set; }
        public int Quantity { get; set; }
        public DateTime ReceivedAt { get; set; }
        public DateTime? GatheringDueDate { get; set; }
        public bool IsGathering { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Location? Location { get; set; }
        public virtual ComponentParameter? ComponentParameter { get; set; }
    }

    public class UserAccount
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User"; // Admin or User
        public string[] Permissions { get; set; } = Array.Empty<string>();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<AuditLog> AuditLogs { get; set; }
    }

    public class AuditLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty; // receiving, gathering, transfer, recalculate
        public string EntityType { get; set; } = string.Empty; // Item, Location, Area, etc.
        public string EntityId { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Ipv4Address { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;

        public virtual UserAccount? UserAccount { get; set; }
    }

    public class ComponentParameter
    {
        public int Id { get; set; }
        public string ComponentCode { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Resistor, Capacitor, etc.
        public string Size { get; set; } = string.Empty; // R0805, R0603, etc.
        public string Value { get; set; } = string.Empty; // 10kΩ, 100nF, etc.
        public string Footprint { get; set; } = string.Empty; // 0805, 0603, etc.
        public double MaxVoltage { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string StockCode { get; set; } = string.Empty;
        public int? ItemId { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Item? Item { get; set; }
    }
}