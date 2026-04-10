using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;

namespace MyFinance_App.Services;

public interface IUserData
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> UpdateBalanceAsync(int userId, decimal newBalance);
    Task<bool> ChangeUserCurrency(int userId, int currencyId);
}

public class UserData(AppDbContext context) : IUserData
{
    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await context.Users
            .AsNoTracking()
            .Include(u => u.Currency)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> UpdateBalanceAsync(int userId, decimal newBalance)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Balance = newBalance;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeUserCurrency(int userId, int newCurrencyId)
    {
        try
        {
            var user = await context.Users
                .Include(u => u.Currency)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var newCurrency = await context.Currencies
                .FirstOrDefaultAsync(c => c.Id == newCurrencyId);
            
            if (newCurrency.RateToUsd > 0)
            {
                user.Balance = (user.Balance * user.Currency.RateToUsd) / newCurrency.RateToUsd;
            }

            user.CurrencyId = newCurrencyId;

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}