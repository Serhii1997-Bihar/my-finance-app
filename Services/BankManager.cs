using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;
using System.Net.Mail;
using System.Net;

namespace MyFinance_App.Services;

public interface IBankManager
{
    Task<bool> CompleteTransaction(User receiver, decimal amount, int bankId);
    Task<bool> AllowCredit(int userId, decimal amount, int bankId);
    Task<bool> RemindDebtor();
}

public class BankManager(AppDbContext context, IFamilyData familyData, IConfiguration configuration) : IBankManager
{
    private async Task<bool> CheckClientFamily(User client, decimal amount)
    {
        var familyBudget = await familyData.GetFamilyBudgetAsync(client.Id);
        var currencyCodes = familyBudget.Keys.Where(k => k != "USD").ToList();
        
        var rates = await context.Currencies
            .Where(c => currencyCodes.Contains(c.Code))
            .ToDictionaryAsync(c => c.Code, c => c.RateToUsd);

        decimal totalInUsd = 0;
        foreach (var (currency, originalSum) in familyBudget)
        {
            if (currency == "USD")
            {
                totalInUsd += originalSum;
            }
            else if (rates.TryGetValue(currency, out decimal rate))
            {
                totalInUsd += originalSum * rate;
            }
        }

        return totalInUsd > amount * 2;
    }

    public async Task<bool> RemindDebtor()
    {
        var debtors = await context.Transactions
            .Include(t => t.User)
            .Where(t => t.Description.Contains("Credit"))
            .GroupBy(t => t.UserId)
            .Select(g => g.First().User)
            .ToListAsync();

        if (!debtors.Any())
        {
            return false;
        }

        bool allNotificationsSent = true;

        using var smtpClient = new SmtpClient(configuration["Smtp:Host"])
        {
            Port = int.Parse(configuration["Smtp:Port"] ?? "587"),
            Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]),
            EnableSsl = true,
        };

        foreach (var user in debtors)
        {
            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(configuration["Smtp:From"]),
                    Subject = "Credit Payment Reminder",
                    Body = $"Hello {user.Username},\n\nThis is a reminder that you have an active credit transaction in your account. Please review your balance and ensure timely repayment.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(user.Email);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                allNotificationsSent = false;
            }
        }

        return allNotificationsSent;
    }
    
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
            await context.SaveChangesAsync();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AllowCredit(int userId, decimal amount, int bankId)
    {
        User user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        Bank bank = await context.Banks
            .FirstOrDefaultAsync(b => b.Id == bankId);
        
        bool allowCredit = await this.CheckClientFamily(user, amount);
        if (allowCredit)
        {
            user.Balance += amount;
            bank.TotalReserve -= amount;
            
            var creditTransaction = new Transaction
            {
                Amount = amount,
                CurrencyId = 1,
                UserId = user.Id,
                Description = $"Credit to {user.Username}",
                CreatedAt = DateTime.UtcNow,
                CategoryId = 7,
                BankId = bankId
            };
            context.Transactions.Add(creditTransaction);
            
            await context.SaveChangesAsync();
            return true;
        }
        
        return false;
    }
}