using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;
using MyFinance_App.Services;

namespace MyFinance_App.Services;

public interface IFinanceManager
{
    Task<bool> AddTransactionAsync(int userId, decimal amount, int categoryId, string description);
    Task<bool> SendMoneyAsync(int senderId, decimal amount, string receiverUsername, int bankId);
}

public class FinanceManager(AppDbContext context, IBankManager bankManager, ILimitManager limitManager) : IFinanceManager
{
    public async Task<bool> AddTransactionAsync(int userId, decimal amount, int categoryId, string description)
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            var user = await context.Users
                .Include(u => u.Currency)
                .FirstOrDefaultAsync(u => u.Id == userId);
        
            var category = await context.Categories
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (user == null || category == null) return false;
            if (user.Balance < amount)
            {
                return false;
            }

            var transaction = new Transaction 
            { 
                Amount = amount, 
                CurrencyId = user.CurrencyId,
                CategoryId = category.Id,
                Description = description,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            user.Balance -= amount;
            context.Transactions.Add(transaction);
    
            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            bool sentMessage = await limitManager.SendMessageAboutLimitation(user.Id);
            if (!sentMessage)
            {
                Console.WriteLine($"Message not sent to {user.Email}");
            }
        
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> SendMoneyAsync(int senderId, decimal amount, string receiverUsername, int bankId)
    {
        if (amount <= 0) return false;

        await using var dbTransaction = await context.Database.BeginTransactionAsync();
        try
        {
            var sender = await context.Users
                .Include(u => u.Currency)
                .FirstOrDefaultAsync(u => u.Id == senderId);
            
            var receiver = await context.Users
                .Include(u => u.Currency)
                .FirstOrDefaultAsync(u => u.Username == receiverUsername);

            var bank = await context.Banks
                .FirstOrDefaultAsync(b => b.Id == bankId);

            if (receiver == null || sender.Id == receiver.Id || bank == null) return false;

            decimal fee = amount * (bank.Fees / 100.0m);
            decimal totalToDeduct = amount + fee;

            if (sender.Balance < totalToDeduct) return false;

            sender.Balance -= totalToDeduct;

            decimal amountInUsd = sender.CurrencyId == 1 ? amount : amount * sender.Currency.RateToUsd;
            decimal totalInUsd = sender.CurrencyId == 1 ? totalToDeduct : totalToDeduct * sender.Currency.RateToUsd;

            bank.TotalReserve += totalInUsd;

            var senderTransaction = new Transaction
            {
                Amount = amount,
                CurrencyId = sender.CurrencyId,
                UserId = sender.Id,
                Description = $"Transaction to {receiver.Username}",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 7,
                BankId = bankId
            };
            context.Transactions.Add(senderTransaction);

            decimal amountForReceiver = receiver.CurrencyId == 1 ? amountInUsd : amountInUsd / receiver.Currency.RateToUsd;
            
            await bankManager.CompleteTransaction(receiver, amountForReceiver, bankId);
            
            await context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            
            bool sentMessage = await limitManager.SendMessageAboutLimitation(sender.Id);
            if (!sentMessage)
            {
                Console.WriteLine($"Message not sent to {sender.Email}");
            }
            
            return true;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return false;
        }
    }
}