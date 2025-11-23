using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ExpenseTracker.Functions.Data;
using ExpenseTracker.Functions.DTOs;
using ExpenseTracker.Functions.Models;
using System.Net;
using System.Text.Json;

namespace ExpenseTracker.Functions.Functions;

/// <summary>
/// HTTP Trigger function to handle UPI payment notifications
/// </summary>
public class UpiPaymentTrigger
{
    private readonly ILogger<UpiPaymentTrigger> _logger;
    private readonly ExpenseDbContext _dbContext;

    public UpiPaymentTrigger(
        ILogger<UpiPaymentTrigger> logger,
        ExpenseDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("UpiPaymentTrigger")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upi/payment")] HttpRequestData req)
    {
        _logger.LogInformation("UPI Payment trigger function processing a request.");

        try
        {
            // Read and deserialize the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Request body is empty" });
                return badResponse;
            }

            var paymentRequest = JsonSerializer.Deserialize<UpiPaymentRequest>(requestBody, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (paymentRequest == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid payment data" });
                return badResponse;
            }

            // Check if transaction already exists
            var existingExpense = _dbContext.Expenses
                .FirstOrDefault(e => e.TransactionId == paymentRequest.TransactionId);

            if (existingExpense != null)
            {
                _logger.LogWarning($"Duplicate transaction detected: {paymentRequest.TransactionId}");
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteAsJsonAsync(new { 
                    error = "Transaction already exists",
                    transactionId = paymentRequest.TransactionId 
                });
                return conflictResponse;
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
                RawPayload = requestBody
            };

            // Save to database
            _dbContext.Expenses.Add(expense);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Expense created successfully. Transaction ID: {expense.TransactionId}, Amount: {expense.Amount} {expense.Currency}");

            // Create response
            var expenseResponse = new ExpenseResponse
            {
                Id = expense.Id,
                TransactionId = expense.TransactionId,
                PaymentMethod = expense.PaymentMethod,
                UpiId = expense.UpiId,
                MerchantName = expense.MerchantName,
                Amount = expense.Amount,
                Currency = expense.Currency,
                Description = expense.Description,
                Category = expense.Category,
                TransactionDate = expense.TransactionDate,
                CreatedAt = expense.CreatedAt,
                Status = expense.Status
            };

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(expenseResponse);
            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing payment request");
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(new { error = "Invalid JSON format" });
            return errorResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UPI payment");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }
}
