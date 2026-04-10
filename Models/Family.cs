namespace MyFinance_App.Models;

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; } = 0;

    public List<User> Members { get; set; } = new();
}