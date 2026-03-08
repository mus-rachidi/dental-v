using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models;

public enum StaffRole
{
    Dentist,
    Hygienist,
    Nurse,
    Assistant,
    Receptionist,
    Other
}

public enum StaffStatus
{
    Active,
    Inactive
}

public class StaffMember
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public StaffRole Role { get; set; } = StaffRole.Assistant;

    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Specialization { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.Today;

    public StaffStatus Status { get; set; } = StaffStatus.Active;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
