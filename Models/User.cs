namespace MyFinance_App.Models;

public class User
{   
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    
    public int CurrencyId { get; set; } = 1; 
    public virtual Currency Currency { get; set; } = null!;
    
    public decimal Balance { get; set; } 
    public int BirthYear { get; set; }
    public string Gender { get; set; } = string.Empty;
    
    public int? FamilyId { get; set; } 
    public virtual Family? Family { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}