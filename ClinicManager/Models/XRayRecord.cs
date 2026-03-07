using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

public class XRayRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [Required, MaxLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    /// <summary>Comma-separated tooth numbers (e.g. "14,15") or empty for full mouth</summary>
    [MaxLength(100)]
    public string ToothNumbers { get; set; } = string.Empty;
}
