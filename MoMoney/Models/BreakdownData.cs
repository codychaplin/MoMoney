
namespace MoMoney.Models;

public class BreakdownData
{
    public string Category { get; set; }
    public decimal Amount { get; set; }
    public decimal ActualAmount { get; set; }
    public Brush Color { get; set; }
    public decimal Percentage { get; set; }
}