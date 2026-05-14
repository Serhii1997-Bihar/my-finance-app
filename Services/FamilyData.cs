namespace MyFinance_App.Services;
using MyFinance_App.Database;
using MyFinance_App.Models;
using Microsoft.EntityFrameworkCore;

public interface IFamilyData
{
    Task<User[]> GetFamilyMembers(int userId);
    Task<Dictionary<string, decimal>> GetFamilyBudgetAsync(int UserId);
    Task<bool> AddMemberToFamily(int userId, string userFamily);
}

public class FamilyData(AppDbContext context) : IFamilyData
{   
    public async Task<User[]> GetFamilyMembers(int userId)
    {
        var family = await context.Families
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Members.Any(m => m.Id == userId));

        if (family == null)
        {
            return Array.Empty<User>();
        }

        return family.Members.ToArray();
    }

    public async Task<Dictionary<string, decimal>> GetFamilyBudgetAsync(int userId)
    {
        return await context.Families
            .Where(f => f.Members.Any(m => m.Id == userId))
            .SelectMany(f => f.Members)
            .GroupBy(m => m.Currency.Code)
            .Select(g => new 
            { 
                CurrencyCode = g.Key, 
                TotalBalance = g.Sum(m => m.Balance) 
            })
            .ToDictionaryAsync(x => x.CurrencyCode, x => x.TotalBalance);
    }

    public async Task<bool> AddMemberToFamily(int userId, string userFamily)
    {
        var family = await context.Families
            .FirstOrDefaultAsync(family => family.Name == userFamily);
        if (family == null)
        {
            return false;
        }

        if (family.Name == userFamily)
        {
            User user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            family.Members.Add(user);
            await context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}