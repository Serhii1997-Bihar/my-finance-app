namespace MyFinance_App.Models;

public class Bank
{
    public int Id { get; set; }
    public string BankName { get; set; } = string.Empty;
    public decimal TotalReserve { get; set; } = 100000000;
    public decimal Fees { get; set; }

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}