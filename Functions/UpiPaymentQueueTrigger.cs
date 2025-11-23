using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ExpenseTracker.Functions.Data;
using ExpenseTracker.Functions.DTOs;
using ExpenseTracker.Functions.Models;
using System.Text.Json;

namespace ExpenseTracker.Functions.Functions;

/// <summary>
/// Service Bus Queue Trigger function to handle UPI payment notifications from a queue
/// </summary>
public class UpiPaymentQueueTrigger
{
    private readonly ILogger<UpiPaymentQueueTrigger> _logger;
    private readonly ExpenseDbContext _dbContext;

    public UpiPaymentQueueTrigger(
        ILogger<UpiPaymentQueueTrigger> logger,
        ExpenseDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("UpiPaymentQueueTrigger")]
    public async Task Run(
        [ServiceBusTrigger("upi-payments", Connection = "ServiceBusConnection")] 
        string messageBody,
        FunctionContext context)
    {
        _logger.LogInformation("Processing UPI payment from Service Bus queue.");
        _logger.LogInformation($"Message: {messageBody}");

        try
        {
            var paymentRequest = JsonSerializer.Deserialize<UpiPaymentRequest>(messageBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paymentRequest == null)
            {
                _logger.LogError("Failed to deserialize payment request");
                throw new InvalidOperationException("Invalid payment data");
            }

            // Check if transaction already exists
            var existingExpense = _dbContext.Expenses
                .FirstOrDefault(e => e.TransactionId == paymentRequest.TransactionId);

            if (existingExpense != null)
            {
                _logger.LogWarning($"Duplicate transaction detected: {paymentRequest.TransactionId}");
                return; // Skip duplicate transactions
            }

            // Create expense entity
            var expense = new Expense
            {
                Id = Guid.NewGuid(),
                TransactionId = paymentRequest.TransactionId,
                PaymentMethod = "UPI",
                UpiId = paymentRequest.UpiId,
                MerchantName = paymentRequest.MerchantName,
                Amount = paymentRequest.Amount,
                Currency = paymentRequest.Currency,
                Description = paymentRequest.Description,
                Category = paymentRequest.Category,
                TransactionDate = paymentRequest.TransactionDate,
                Status = paymentRequest.Status,
                CreatedAt = DateTime.UtcNow,
                RawPayload = messageBody
            };

            // Save to database
            _dbContext.Expenses.Add(expense);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Expense created successfully from queue. Transaction ID: {expense.TransactionId}, Amount: {expense.Amount} {expense.Currency}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UPI payment from queue");
            throw; // Re-throw to trigger retry mechanism
        }
    }
}
