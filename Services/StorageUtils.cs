using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyFinance_App.Database;

namespace MyFinance_App.Services;

public interface IStorageService
{
    Task ExportTransactionsToCsvAsync(int userId, string username);
}

public class StorageService(AppDbContext context, IConfiguration configuration) : IStorageService
{
    private readonly string _exportPath = configuration["StorageSettings:ExportPath"] switch
    {
        var path when string.IsNullOrEmpty(path) => Path.Combine(AppContext.BaseDirectory, "Files"),
        var path when !Path.IsPathRooted(path) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path)),
        var path => path
    };

    public async Task ExportTransactionsToCsvAsync(int userId, string username)
    {
        if (!Directory.Exists(_exportPath))
        {
            Directory.CreateDirectory(_exportPath);
        }

        var safeUsername = username.Replace(" ", "_");
        var fileName = $"{safeUsername}_Transactions_{DateTime.Now:yyyyMMdd}.csv";
        var filePath = Path.Combine(_exportPath, fileName);
        
        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        await writer.WriteLineAsync("Id,Category,Amount,Currency,Date,Description");

        var transactions = context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Currency)
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .AsAsyncEnumerable();

        await foreach (var t in transactions)
        {
            var line = string.Format(CultureInfo.InvariantCulture, 
                "{0},{1},{2},{3},{4},{5}",
                t.Id, 
                EscapeCsv(t.Category.Name), 
                t.Amount, 
                EscapeCsv(t.Currency.Code), 
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), 
                EscapeCsv(t.Description));

            await writer.WriteLineAsync(line);
        }
    
        await writer.FlushAsync();
    }

    private string EscapeCsv(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return string.Empty;
        
        if (data.Contains(',') || data.Contains('"') || data.Contains('\n') || data.Contains('\r'))
        {
            return $"\"{data.Replace("\"", "\"\"")}\"";
        }
        return data;
    }
}