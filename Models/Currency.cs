namespace MyFinance_App.Models;

public class Currency
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal RateToUsd { get; set; }
}
