using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseTracker.Functions.Models;

public class Expense
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PaymentMethod { get; set; } = "UPI";

    [Required]
    [MaxLength(200)]
    public string UpiId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string MerchantName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string Currency { get; set; } = "INR";

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = "Others";

    [Required]
    public DateTime TransactionDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Success";

    public string? RawPayload { get; set; }
}
