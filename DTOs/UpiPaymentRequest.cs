using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Functions.DTOs;

public class UpiPaymentRequest
{
    [Required]
    public string TransactionId { get; set; } = string.Empty;

    [Required]
    public string UpiId { get; set; } = string.Empty;

    [Required]
    public string MerchantName { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "INR";

    public string? Description { get; set; }

    public string Category { get; set; } = "Others";

    [Required]
    public DateTime TransactionDate { get; set; }

    public string Status { get; set; } = "Success";
}
