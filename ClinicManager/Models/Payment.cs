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

public enum CNSSClaimStatus
{
    NotSubmitted,
    Submitted,
    Approved,
    Rejected
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

    // Morocco & CNSS fields
    [Column(TypeName = "decimal(18,2)")]
    public decimal TreatmentCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CNSSCoveredAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PatientAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal VATRate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal VATAmount { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "MAD";

    public CNSSClaimStatus CNSSClaimStatus { get; set; } = CNSSClaimStatus.NotSubmitted;

    [MaxLength(50)]
    public string CNSSReceiptNumber { get; set; } = string.Empty;
}
