using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

public enum ToothCondition
{
    Healthy,
    Cavity,
    Filled,
    Crown,
    RootCanal,
    Missing,
    Implant,
    Bridge,
    Extraction,
    Fractured
}

public enum ToothType
{
    Incisor,
    Canine,
    Premolar,
    Molar,
    WisdomTooth
}

public class ToothRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [Required]
    public int ToothNumber { get; set; }

    public ToothType Type { get; set; } = ToothType.Molar;

    public ToothCondition Condition { get; set; } = ToothCondition.Healthy;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
