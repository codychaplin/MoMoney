using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings
{
    public partial class AddAccountViewModel : ObservableObject
    {
        [ObservableProperty]
        public string name; // account name

        [ObservableProperty]
        public string type; // account type

        [ObservableProperty]
        public decimal startingBalance; // starting balance

        /// <summary>
        /// adds Account to database using input fields from view.
        /// </summary>
        [RelayCommand]
        async Task Add()
        {
            await AccountService.AddAccount(Name, Type, StartingBalance, true);
            await Shell.Current.GoToAsync("..");
        }
    }
}
