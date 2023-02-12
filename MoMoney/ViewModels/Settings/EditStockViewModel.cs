using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings
{
    [QueryProperty(nameof(Symbol), nameof(Symbol))]
    public partial class EditStockViewModel : ObservableObject
    {
        [ObservableProperty]
        public Stock stock = new();

        public string Symbol { get; set; } // Stock Symbol

        /// <summary>
        /// Gets Stock using Symbol.
        /// </summary>
        public async Task GetStock()
        {
            Stock = await StockService.GetStock(Symbol);
        }

        /// <summary>
        /// Edits Stock in database using input fields from view.
        /// </summary>
        [RelayCommand]
        async Task Edit()
        {
            try
            {
                await StockService.UpdateStock(Stock);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        /// <summary>
        /// Removes the Stock from the database.
        /// </summary>
        [RelayCommand]
        async Task Remove()
        {
            bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Stock.Symbol}\"?", "Yes", "No");

            if (flag)
            {
                await StockService.RemoveStock(Stock.Symbol);
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
