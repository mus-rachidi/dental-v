using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

public class InventoryItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "pcs";

    public int MinStockLevel { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(200)]
    public string Supplier { get; set; } = string.Empty;

    public DateTime? LastRestockedDate { get; set; }

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
