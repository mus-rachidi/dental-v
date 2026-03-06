using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models;

public enum PaymentMethod
{
    Cash,
    CreditCard,
    DebitCard,
    Check,
    Insurance,
    BankTransfer,
    Other
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Refunded,
    Cancelled
}

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    [ForeignKey(nameof(PatientId))]
    public virtual Patient? Patient { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Now;

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
