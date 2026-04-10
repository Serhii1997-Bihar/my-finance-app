using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;

namespace MyFinance_App.Services;

public interface IAnalyticService
{
    Task<Dictionary<string, decimal>> GetTotalExpensesAsync(int userId);
    Task<List<Transaction>> GetTransactionsByCategory(int userId, string category);
    Task<decimal> GetUserBalance(int userId);
    Task<List<Transaction>> GetTransactions(int userId);
}

public class AnalyticService(AppDbContext context) : IAnalyticService
{
    public async Task<Dictionary<string, decimal>> GetTotalExpensesAsync(int userId)
    {
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Include(t => t.Currency)
            .GroupBy(t => t.Currency.Code)
            .Select(g => new 
            { 
                CurrencyCode = g.Key, 
                Total = g.Sum(t => t.Amount) 
            })
            .ToDictionaryAsync(x => x.CurrencyCode, x => x.Total);
    }

    public async Task<List<Transaction>> GetTransactions(int userId)
    {
        return await context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Include(t => t.Category)
            .Include(t => t.Currency) 
            .ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsByCategory(int userId, string categoryName)
    {
        return await context.Transactions
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.Currency)
            .Where(t => t.UserId == userId && 
                        t.Category.Name.ToLower() == categoryName.ToLower())
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetUserBalance(int userId)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Balance)
            .FirstOrDefaultAsync();
    }
}