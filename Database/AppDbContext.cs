using Microsoft.EntityFrameworkCore;
using MyFinance_App.Models;

namespace MyFinance_App.Database;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<Bank> Banks { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Currency> Currencies { get; set; } = null!;
    public DbSet<Family> Families { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Models.Transaction>(entity =>
        {
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.ExchangeRate)
                .HasPrecision(18, 4);
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne<Models.Currency>(t => t.Currency)
                .WithMany()
                .HasForeignKey(t => t.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Models.Category>(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId);
    
            entity.HasOne<Models.User>(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId);
    
            entity.HasOne<Models.Bank>(t => t.Bank)
                .WithMany(b => b.Transactions)
                .HasForeignKey(t => t.BankId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Balance)
                .HasPrecision(18, 2);

            entity.HasOne(u => u.Currency)
                  .WithMany()
                  .HasForeignKey(u => u.CurrencyId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.Family)
                  .WithMany(f => f.Members)
                  .HasForeignKey(u => u.FamilyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
        
        modelBuilder.Entity<Bank>(entity => 
        {
            entity.Property(e => e.TotalReserve)
                .HasPrecision(18, 2);
        });

        modelBuilder.Entity<Family>(entity => 
        {
            entity.Property(e => e.Balance)
                .HasPrecision(18, 2);
        });
        
        modelBuilder.Entity<Currency>(entity => 
        { 
            entity.Property(e => e.RateToUsd)
                .HasPrecision(18, 6); 
            
            entity.HasIndex(c => c.Code)
                .IsUnique(); 
        });
    }
}