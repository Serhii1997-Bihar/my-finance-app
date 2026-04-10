using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MyFinance_App.Database;
using MyFinance_App.Models;
using MyFinance_App.Services;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFinanceManager, FinanceManager>();
builder.Services.AddScoped<IAnalyticService, AnalyticService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IUserData, UserData>();
builder.Services.AddScoped<CurrencyProvider>();
builder.Services.AddScoped<IBankManager, BankManager>();

using IHost host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var currencyProvider = scope.ServiceProvider.GetRequiredService<CurrencyProvider>();
    await currencyProvider.UpdateCurrenciesFromApiAsync();
}
Console.WriteLine("Rates updated successfully!");

await RunMainMenu(host.Services);

static async Task RunMainMenu(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var provider = scope.ServiceProvider;
    
    var authService = provider.GetRequiredService<IAuthService>();
    var financeManager = provider.GetRequiredService<IFinanceManager>();
    var analyticService = provider.GetRequiredService<IAnalyticService>();
    var storageService = provider.GetRequiredService<IStorageService>();
    var userData = provider.GetRequiredService<IUserData>();
    var currencyProvider = provider.GetRequiredService<CurrencyProvider>();
    
    while (true)
    {
        Console.WriteLine("\n=== Finance Assistant ===");
        Console.WriteLine("1) Login, 2) Register, 0) Exit");
        var entryChoice = Console.ReadLine();

        if (entryChoice == "0") break;

        if (entryChoice == "2")
        {
            Console.Write("Username: ");
            string name = Console.ReadLine() ?? "";
            Console.Write("Password: ");
            string pass = Console.ReadLine() ?? "";
            Console.Write("Email: ");
            string email = Console.ReadLine() ?? "";
            Console.Write("Birth Year: ");
            int.TryParse(Console.ReadLine(), out int year);
            Console.Write("Gender: ");
            string gender = Console.ReadLine() ?? "";
            Console.Write("Initial Balance: ");
            decimal.TryParse(Console.ReadLine(), out decimal balance);

            var newUser = new User { Username = name, Email = email, BirthYear = year, Balance = balance, Gender = gender};
            await authService.CreateUserAsync(newUser, pass);
            Console.WriteLine("Registration successful! Please login.");
            continue;
        }

        if (entryChoice == "1")
        {
            Console.Write("Enter username: ");
            string loginName = Console.ReadLine() ?? "";
            Console.Write("Enter password: ");
            string loginPass = Console.ReadLine() ?? "";

            var currentUser = await authService.AuthenticateAsync(loginName, loginPass);
            if (currentUser != null)
            {
                await RunUserInterface(currentUser.Id, userData, financeManager, analyticService, storageService, currencyProvider);
            }
            else
            {
                Console.WriteLine("Login failed.");
            }
        }
    }
}

static async Task RunUserInterface(int userId, IUserData userData, IFinanceManager financeManager, 
    IAnalyticService analyticService, IStorageService storageService, CurrencyProvider currencyProvider)
{
    while (true)
    {
        var user = await userData.GetUserByIdAsync(userId);
        if (user == null) break;

        Console.WriteLine($"\n=== Welcome To Finance Assistant, {user.Username} ===");
        Console.WriteLine("1) Add Transaction, 2) Analytics, 3) Send Money, 4) Settings, 0) Logout");
        
        var choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                Console.Write("Summa: "); 
                decimal.TryParse(Console.ReadLine(), out decimal summa);

                Console.WriteLine("Choose Category by number or name:");
                Console.WriteLine("1. Food, 2. Sport, 3. Traveling, 4. Clothes, 5. Medical, 6. Other");
                Console.Write("Category: ");
                int.TryParse(Console.ReadLine(), out int categoryId);

                Console.Write("Description: "); 
                string description = Console.ReadLine() ?? "";

                await financeManager.AddTransactionAsync(userId, summa, categoryId, description);
                break;
            case "2":
                await RunAnalyticMenu(userId, user.Username, analyticService, storageService);
                break;
            case "3":
                Console.Write("Receiver Username: "); 
                string rec = Console.ReadLine() ?? "";
            
                Console.Write("Amount: "); 
                decimal.TryParse(Console.ReadLine(), out decimal sAmt);

                Console.Write("Enter Bank ID: ");
                int.TryParse(Console.ReadLine(), out int bId);

                bool success = await financeManager.SendMoneyAsync(userId, sAmt, rec, bId);
            
                if (success)
                    Console.WriteLine("Success! Money sent.");
                else
                    Console.WriteLine("Error: Transaction failed (check balance, receiver name or bank ID).");
                break;
            case "4":
                await RunSettingsMenu(userId, userData, currencyProvider);
                break;
            case "0":
                return;
        }
    }
}

static async Task RunAnalyticMenu(int userId, string username, IAnalyticService analyticService, IStorageService storageService)
{
    Console.WriteLine("\n=== Analytics Menu ===");
    Console.WriteLine("1) Total Expenses, 2) Export CSV, 3) Get Balance, 4) Get Transactions, 5) Detailed Expenses,  0) Back");
    var choice = Console.ReadLine();

    if (choice == "1")
    {   
        var totals = await analyticService.GetTotalExpensesAsync(userId);

        Console.WriteLine("\n===== Your Total Expenses =====");
        if (totals.Count == 0)
        {
            Console.WriteLine("No transactions found.");
        }
        else
        {
            foreach (var entry in totals)
            {
                Console.WriteLine($"{entry.Value:0.00} {entry.Key}");
            }
        }
    }
    
    else if (choice == "2")
    {
        await storageService.ExportTransactionsToCsvAsync(userId, username);
        Console.WriteLine("Exported to Files folder.");
    }
    
    else if (choice == "3")
    {
        var balance = await analyticService.GetUserBalance(userId);
        Console.WriteLine($"=== Your total Balance: {balance} ===");
    }
    
    else if (choice == "4")
    {
        var transactions = await analyticService.GetTransactions(userId);

        Console.WriteLine("\n" + new string('=', 95));
        Console.WriteLine($"{"Amount",-10} | {"Currency",-10} | {"Category",-15} | {"Description",-30} | {"Date",-20}");
        Console.WriteLine(new string('=', 95));

        foreach (var t in transactions)
        {
            string amount = $"{t.Amount:0.00}";
            string currency = $"{t.Currency.Code}";
            string category = t.Category?.Name ?? "Other";
            
            string description = t.Description.Length > 27 
                ? t.Description[..27] + "..." 
                : t.Description;
        
            string date = t.CreatedAt.ToString("dd.MM.yyyy HH:mm");

            Console.WriteLine($"{amount,-10} | {currency,-10} | {category,-15} | {description,-30} | {date,-20}");
        }

        Console.WriteLine(new string('=', 95));
        
    }
    
    else if (choice == "5")
    {
        Console.Write("Enter category name: ");
        string catName = Console.ReadLine() ?? "";

        var transactions = await analyticService.GetTransactionsByCategory(userId, catName);

        if (transactions.Count == 0)
        {
            Console.WriteLine($"\nNo transactions found for category: {catName}");
        }
        else
        {
            Console.WriteLine("\n" + new string('=', 95));
            Console.WriteLine($"{"Amount",-10} | {"Currency",-10} | {"Category",-15} | {"Description",-30} | {"Date",-20}");
            Console.WriteLine(new string('=', 95));

            foreach (var t in transactions)
            {
                string amount = $"{t.Amount:0.00}";
                string currency = t.Currency?.Code ?? "???";
                string category = t.Category?.Name ?? "Other";
            
                string description = t.Description.Length > 27 
                    ? t.Description[..27] + "..." 
                    : t.Description;

                string date = t.CreatedAt.ToString("dd.MM.yyyy HH:mm");

                Console.WriteLine($"{amount,-10} | {currency,-10} | {category,-15} | {description,-30} | {date,-20}");
            }
            Console.WriteLine(new string('=', 95));
        }
    }
    
    else if (choice == "0")
    {
        return;
    }
}

static async Task RunSettingsMenu(int userId, IUserData userData, CurrencyProvider currencyProvider)
{
    Console.WriteLine("\n=== Settings Menu ===");
    Console.WriteLine("1) User Data, 2) Currency, 0) Back");
    var user = await userData.GetUserByIdAsync(userId);
    
    var choice = Console.ReadLine();
    switch (choice)
    {
        case "1":
            Console.WriteLine("\n============== User Data ===============");
            Console.WriteLine($"ID: {user.Id}");
            Console.WriteLine($"Username: {user.Username}");
            Console.WriteLine($"Gender: {user.Gender}");
            Console.WriteLine($"Email: {user.Email}");
            Console.WriteLine($"Birth Of Year: {user.BirthYear}");
            Console.WriteLine($"Balance: {user.Balance}");
            Console.WriteLine($"Currency: {user.Currency.Code}");
            Console.WriteLine("==========================================");
            break;
        case "2":
            var currencies = await currencyProvider.GetCurrencies();
            foreach (var currency in currencies)
            {
                Console.WriteLine($"{currency.Code} - {currency.Id}");
            }
            
            Console.Write("\nPut ID of your new currency: ");
            if (int.TryParse(Console.ReadLine(), out int selectedId))
            {
                var isChanged = await userData.ChangeUserCurrency(userId, selectedId);
                Console.WriteLine(isChanged 
                    ? "Currency has been changed successfully." 
                    : "Please, select the correct currency.");
            }
            break;
            
        case "0":
            return;
    }
    

}