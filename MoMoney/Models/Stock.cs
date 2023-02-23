using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace MoMoney.Models;

public partial class Stock : ObservableObject
{
    [PrimaryKey]
    public string Symbol { get; set; }
    public int Quantity { get; set; }
    public decimal Cost { get; set; }

    [ObservableProperty]
    public decimal marketPrice;
    public decimal BookValue { get; set; }
}
