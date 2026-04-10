namespace MyFinance_App.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}