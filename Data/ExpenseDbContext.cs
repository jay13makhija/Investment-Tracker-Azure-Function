using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Functions.Models;

namespace ExpenseTracker.Functions.Data;

public class ExpenseDbContext : DbContext
{
    public ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : base(options)
    {
    }

    public DbSet<Expense> Expenses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");

            entity.HasIndex(e => e.TransactionId)
                .IsUnique()
                .HasDatabaseName("IX_Expenses_TransactionId");

            entity.HasIndex(e => e.TransactionDate)
                .HasDatabaseName("IX_Expenses_TransactionDate");

            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_Expenses_Category");

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
