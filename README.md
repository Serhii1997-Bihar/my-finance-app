# 💳 MyFinance_App

**MyFinance_App** is a multi-currency personal and family finance management system built with **.NET (C#)** and **PostgreSQL**. The application features multi-currency accounts, bank management, expense analytics, spending limits, and shared family budgeting.

---

## 🌟 Key Features

* 💱 **Multi-Currency & Auto-Conversion** — Full multi-currency support (`Currency.cs`, `CurrencyProvider.cs`) with dynamic rate conversion for transactions and balance tracking.
* 🏦 **Bank & Account Management** — Complete oversight of bank accounts, cards, and financial operations (`Bank.cs`, `BankManager.cs`).
* 📊 **Deep Financial Analytics** — In-depth expense analysis filtered by timeframes, types, and categories (`AnalyticService.cs`, `FinanceManager.cs`).
* 📁 **Export & Data Storage** — Exports and saves financial reports and transaction logs directly to files (`Files/`, `StorageUtils.cs`).
* ⚠️ **Limit Control System** — Real-time monitoring of daily and category-specific spending limits (`LimitManager.cs`).
* 👨‍👩‍👧 **Shared Family Budgeting** — Collaborative money management with real-time transaction visibility for family members (`Family.cs`, `FamilyData.cs`).
* 🔐 **Auth & User Security** — User management and authentication subsystem (`User.cs`, `UserData.cs`, `AuthService.cs`).

---

## 🛠️ Tech Stack

* **Language & Platform:** C# / .NET 8+
* **Database:** PostgreSQL (via Entity Framework Core)
* **ORM & Migrations:** EF Core (`AppDbContext.cs`, `Migrations/`)
* **Architecture:** Layered Architecture (Models, Services, Database Layer)

---

## 📁 Repository Structure

```text
MyFinance_App/
├── Database/              # Entity Framework database context (AppDbContext.cs)
├── Files/                 # Generated reports, transaction logs, and local data files
├── Migrations/            # EF Core PostgreSQL database migrations (Initial, AddFees, AddBank)
├── Models/                # Domain entities (Bank, Category, Currency, Family, Transaction, User)
├── Services/              # Business logic services (Analytic, Auth, Currency, Limits, Family, Storage)
├── .gitignore             # Git ignore configuration
├── appsettings.json       # Database connection strings and configuration settings
├── Program.cs             # Application entry point
└── README.md              # Project documentation