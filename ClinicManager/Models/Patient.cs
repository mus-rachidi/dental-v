using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models;

public class Patient
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(10)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PhotoPath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Morocco & Pro fields
    [MaxLength(20)]
    public string CIN { get; set; } = string.Empty;

    [MaxLength(200)]
    public string EmergencyContact { get; set; } = string.Empty;

    public DateTime RegistrationDate { get; set; } = DateTime.Now;

    [MaxLength(1000)]
    public string Allergies { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Medications { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string ChronicDiseases { get; set; } = string.Empty;

    [MaxLength(50)]
    public string PregnancyStatus { get; set; } = string.Empty;

    // CNSS (Caisse Nationale de Sécurité Sociale - Morocco)
    [MaxLength(50)]
    public string CNSSNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string CNSSCoverageType { get; set; } = string.Empty;

    public DateTime? CNSSRegistrationDate { get; set; }

    public DateTime? CNSSValidityDate { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<ToothRecord> ToothRecords { get; set; } = new List<ToothRecord>();
    public virtual ICollection<XRayRecord> XRayRecords { get; set; } = new List<XRayRecord>();
}
