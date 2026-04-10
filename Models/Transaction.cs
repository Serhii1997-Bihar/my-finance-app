namespace MyFinance_App.Models;

public class Transaction
{   
    public int Id { get; set; } 
    public decimal Amount { get; set; } 
    
    public int CurrencyId { get; set; }
    public virtual Currency Currency { get; set; } = null!;
    
    public decimal ExchangeRate { get; set; } = 1.0m;
    
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    
    public int? BankId { get; set; }
    public virtual Bank? Bank { get; set; }
    
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
}