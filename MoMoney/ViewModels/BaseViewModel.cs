using CommunityToolkit.Mvvm.ComponentModel;

namespace MoMoney.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    public static DateTime from = new();

    [ObservableProperty]
    public static DateTime to = new();
}