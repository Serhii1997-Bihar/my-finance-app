using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;

namespace MyFinance_App.Services;

public interface ILimitManager
{
    Task<bool> SendMessageAboutLimitation(int userId);
}

public class LimitManager(AppDbContext context, IConfiguration configuration) : ILimitManager
{
    private decimal GetUserConvertedBalance(User user)
    {
        decimal balanceInUsd = user.CurrencyId == 1 ? user.Balance : user.Balance * user.Currency.RateToUsd;
        return balanceInUsd;
    }

    public async Task<bool> SendMessageAboutLimitation(int userId)
    {
        User user = await context.Users
            .AsNoTracking()
            .Include(u => u.Currency)
            .FirstOrDefaultAsync(u => u.Id == userId);

        decimal balanceInUsd = this.GetUserConvertedBalance(user);
        string email = user.Email;

        if (balanceInUsd < 100)
        {
            try
            {
                using var smtpClient = new SmtpClient(configuration["Smtp:Host"])
                {
                    Port = int.Parse(configuration["Smtp:Port"] ?? "587"),
                    Credentials = new NetworkCredential(configuration["Smtp:Username"], configuration["Smtp:Password"]),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(configuration["Smtp:From"]),
                    Subject = "Financial Limit Warning",
                    Body = $"Hello {user.Username},\n\nYour balance is below $100 (Current: {balanceInUsd:F2} USD). Please Top up.",
                    IsBodyHtml = false,
                };
                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
                return true; 
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        return false;
    }
}