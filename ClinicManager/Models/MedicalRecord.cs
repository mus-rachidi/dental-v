using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

public class MedicalRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Now;

    [MaxLength(200)]
    public string DoctorName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Prescription { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Vitals { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
