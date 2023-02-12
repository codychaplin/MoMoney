using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Views.Stats;

namespace MoMoney.ViewModels
{
    public partial class StatsPageViewModel : ObservableObject
    {


        /// <summary>
        /// Goes to AccountSummaryPage.xaml.
        /// </summary>
        [RelayCommand]
        async Task GoToAccountSummary()
        {
            await Shell.Current.GoToAsync(nameof(AccountSummaryPage));
        }

        /// <summary>
        /// Goes to MonthBreakdownPage.xaml.
        /// </summary>
        [RelayCommand]
        async Task GoToMonthBreakdown()
        {
            await Shell.Current.GoToAsync(nameof(MonthBreakdownPage));
        }

        /// <summary>
        /// Goes to MonthBreakdownPage.xaml.
        /// </summary>
        [RelayCommand]
        async Task GoToStocks()
        {
            await Shell.Current.GoToAsync(nameof(StocksPage));
        }
    }
}
