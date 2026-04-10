using MyFinance_App.Models;
using MyFinance_App.Database;
using Microsoft.EntityFrameworkCore;

namespace MyFinance_App.Services;

public interface IAuthService
{
    Task CreateUserAsync(User user, string password);
    Task<User?> AuthenticateAsync(string username, string password);
}

public class AuthService(AppDbContext context) : IAuthService
{
    public async Task CreateUserAsync(User user, string password)
    {
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        
        context.Users.Add(user);
        await context.SaveChangesAsync();
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;
        
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }
}