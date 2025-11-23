using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExpenseTracker.Functions.Data;
using ExpenseTracker.Functions.DTOs;
using System.Net;

namespace ExpenseTracker.Functions.Functions;

/// <summary>
/// HTTP Trigger function to retrieve expenses
/// </summary>
public class GetExpensesTrigger
{
    private readonly ILogger<GetExpensesTrigger> _logger;
    private readonly ExpenseDbContext _dbContext;

    public GetExpensesTrigger(
        ILogger<GetExpensesTrigger> logger,
        ExpenseDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("GetExpenses")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "expenses")] HttpRequestData req)
    {
        _logger.LogInformation("Get expenses function processing a request.");

        try
        {
            // Parse query parameters
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var category = query["category"];
            var startDateStr = query["startDate"];
            var endDateStr = query["endDate"];
            var limitStr = query["limit"] ?? "100";

            // Build query
            var expensesQuery = _dbContext.Expenses.AsQueryable();

            // Filter by category
            if (!string.IsNullOrEmpty(category))
            {
                expensesQuery = expensesQuery.Where(e => e.Category == category);
            }

            // Filter by date range
            if (DateTime.TryParse(startDateStr, out var startDate))
            {
                expensesQuery = expensesQuery.Where(e => e.TransactionDate >= startDate);
            }

            if (DateTime.TryParse(endDateStr, out var endDate))
            {
                expensesQuery = expensesQuery.Where(e => e.TransactionDate <= endDate);
            }

            // Apply limit
            if (int.TryParse(limitStr, out var limit) && limit > 0)
            {
                expensesQuery = expensesQuery.Take(limit);
            }

            // Execute query
            var expenses = await expensesQuery
                .OrderByDescending(e => e.TransactionDate)
                .ToListAsync();

            // Map to response DTOs
            var expenseResponses = expenses.Select(e => new ExpenseResponse
            {
                Id = e.Id,
                TransactionId = e.TransactionId,
                PaymentMethod = e.PaymentMethod,
                UpiId = e.UpiId,
                MerchantName = e.MerchantName,
                Amount = e.Amount,
                Currency = e.Currency,
                Description = e.Description,
                Category = e.Category,
                TransactionDate = e.TransactionDate,
                CreatedAt = e.CreatedAt,
                Status = e.Status
            }).ToList();

            _logger.LogInformation($"Retrieved {expenseResponses.Count} expenses");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                count = expenseResponses.Count,
                expenses = expenseResponses
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }

    [Function("GetExpenseById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "expenses/{id}")] HttpRequestData req,
        string id)
    {
        _logger.LogInformation($"Get expense by ID: {id}");

        try
        {
            if (!Guid.TryParse(id, out var expenseId))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid expense ID format" });
                return badResponse;
            }

            var expense = await _dbContext.Expenses.FindAsync(expenseId);

            if (expense == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new { error = "Expense not found" });
                return notFoundResponse;
            }

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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(expenseResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving expense with ID: {id}");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Internal server error" });
            return errorResponse;
        }
    }
}
