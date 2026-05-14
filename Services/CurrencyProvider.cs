using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyFinance_App.Database;
using MyFinance_App.Models;

namespace MyFinance_App.Services;

public interface ICurrencyProvider
{
    Task<Dictionary<string, decimal>> GetLatestRatesAsync();
    Task UpdateCurrenciesFromApiAsync();
    Task<List<Currency>> GetCurrencies();
}

public class CurrencyProvider(AppDbContext context) : ICurrencyProvider
{
    private readonly HttpClient _httpClient = new();
    private const string ApiUrl = "https://open.er-api.com/v6/latest/USD";

    public async Task<Dictionary<string, decimal>> GetLatestRatesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(ApiUrl);
            using var doc = JsonDocument.Parse(response);
            var ratesElement = doc.RootElement.GetProperty("rates");

            var result = new Dictionary<string, decimal>();

            foreach (var property in ratesElement.EnumerateObject())
            {
                string code = property.Name;
                decimal rateToUsdInverse = property.Value.GetDecimal();

                if (rateToUsdInverse != 0)
                {
                    result.Add(code, 1 / rateToUsdInverse);
                }
            }

            return result;
        }
        catch
        {
            return new Dictionary<string, decimal>();
        }
    }

    public async Task UpdateCurrenciesFromApiAsync()
    {
        var newRates = await GetLatestRatesAsync();
        if (newRates.Count == 0) return;

        var dbCurrencies = await context.Currencies.ToListAsync();

        foreach (var currency in dbCurrencies)
        {
            if (newRates.TryGetValue(currency.Code, out decimal freshRate))
            {
                currency.RateToUsd = freshRate;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<Currency>> GetCurrencies()
    {
        return await context.Currencies
            .AsNoTracking()
            .ToListAsync();
    }
}