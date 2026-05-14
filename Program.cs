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
builder.Services.AddScoped<IFamilyData, FamilyData>();
builder.Services.AddScoped<ICurrencyProvider, CurrencyProvider>();
builder.Services.AddScoped<IBankManager, BankManager>();
builder.Services.AddScoped<ILimitManager, LimitManager>();
builder.Services.AddScoped<MainMenu>();

using IHost host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var currencyProvider = scope.ServiceProvider.GetRequiredService<ICurrencyProvider>();
    await currencyProvider.UpdateCurrenciesFromApiAsync();
}
Console.WriteLine("Rates updated successfully!");

using (var scope = host.Services.CreateScope())
{
    var mainMenu = scope.ServiceProvider.GetRequiredService<MainMenu>();
    await mainMenu.StartAsync();
}

public class MainMenu(IAuthService authService, IFinanceManager financeManager, IAnalyticService analyticService, 
    IStorageService storageService, IUserData userData, ICurrencyProvider currencyProvider, IFamilyData familyData)
{
    public async Task StartAsync()
    {
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
                    await RunUserInterface(currentUser.Id);
                }
                else
                {
                    Console.WriteLine("Login failed.");
                }
            }
        }
    }

    private async Task RunUserInterface(int userId)
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
                    Console.Write($"Summa {user.Currency.Name}: "); 
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
                    await RunAnalyticMenu(userId);
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
                    await RunSettingsMenu(userId);
                    break;
                case "0":
                    return;
            }
        }
    }

    private async Task RunAnalyticMenu(int userId)
    {
        Console.WriteLine("\n=== Analytics Menu ===");
        Console.WriteLine("1) Total Expenses, 2) Export CSV, 3) Get Balance, 4) Get Transactions, 5) Detailed Expenses,  0) Back");
        var choice = Console.ReadLine();
        
        var user = await userData.GetUserByIdAsync(userId);

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
            DateOnly fromDate;
            Console.WriteLine("Show The Date from (format: YYYY-MM-DD):");
            while (!DateOnly.TryParse(Console.ReadLine(), out fromDate))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid date format! Please try again (e.g., 2026-05-11):");
                Console.ResetColor();
            }

            DateOnly toDate;
            Console.WriteLine("Show The Date to (format: YYYY-MM-DD):");
            while (!DateOnly.TryParse(Console.ReadLine(), out toDate))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid date format! Please try again (e.g., 2026-05-11):");
                Console.ResetColor();
            }

            Console.WriteLine("Show The Category:");
            string category = Console.ReadLine() ?? string.Empty;

            await storageService.ExportTransactionsToCsvAsync(userId, fromDate, toDate, category);
            Console.WriteLine("Exported to Files folder.");
        }
        
        else if (choice == "3")
        {
            var balance = await analyticService.GetUserBalance(userId);
            Console.WriteLine($"=== Your total Balance: {balance} {user.Currency.Code} ===");
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

    private async Task RunSettingsMenu(int userId)
    {
        Console.WriteLine("\n=== Settings Menu ===");
        Console.WriteLine("1) User Data, 2) Currency, 3) My Family, 0) Back");
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
                Console.WriteLine($"Family: {user.Family?.Name}");
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

            case "3":
                var members = await familyData.GetFamilyMembers(userId);
                if (members.Length > 0)
                {
                    Console.WriteLine("======= User Family =======");
                    foreach (var member in members)
                    {
                        var name = member.Username;
                        Console.WriteLine(name);
                    }

                    var budget = await familyData.GetFamilyBudgetAsync(userId);
                    Console.WriteLine($"===== Your family budget ======");

                    foreach (var element in budget)
                    {
                        Console.WriteLine($"{element.Value} {element.Key}");
                    }
                }

                else
                {
                    Console.WriteLine("You have no family, but you can become of member. Put below you family: ");
                    string userFamily = Console.ReadLine();

                    bool isMember = await familyData.AddMemberToFamily(userId, userFamily);
                    if (isMember)
                    {
                        Console.WriteLine("Success");
                    }
                    else
                    {
                        Console.WriteLine("There is not the similar family in the app");
                    }
                }

                break;

            case "0":
                return;
        }

    }
}