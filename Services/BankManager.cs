using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;

namespace MyFinance_App.Services;

public interface IBankManager
{
    Task<bool> CompleteTransaction(User receiver, decimal amount, int bankId);
}

public class BankManager(AppDbContext context) : IBankManager
{
    public async Task<bool> CompleteTransaction(User receiver, decimal amount, int bankId)
    {
        try
        {
            var bank = await context.Banks
                .FirstOrDefaultAsync(b => b.Id == bankId);

            bank.TotalReserve -= amount;
            var receiverTransaction = new Transaction
            {
                Amount = amount,
                CurrencyId = receiver.CurrencyId,
                UserId = receiver.Id,
                Description = $"Inward transfer to {receiver.Username}",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 7,
                BankId = bankId
            };
            
            context.Transactions.Add(receiverTransaction);

            receiver.Balance += amount;
            return true;
        }
        catch
        {
            return false;
        }
    }
}