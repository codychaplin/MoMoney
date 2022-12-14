using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Services;
using MoMoney.Models;
using MoMoney.Views.Settings;
using System.Collections.ObjectModel;

namespace MoMoney.ViewModels.Settings
{
    public partial class CategoriesViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Category> categories = new();

        /// <summary>
        /// Goes to AddCategoryPage.xaml.
        /// </summary>
        [RelayCommand]
        async Task GoToAddCategory()
        {
            await Shell.Current.GoToAsync(nameof(AddCategoryPage));
        }

        /// <summary>
        /// Goes to EditCategoryPage.xaml with a Category ID as a parameter.
        /// </summary>
        [RelayCommand]
        async Task GoToEditCategory(int ID)
        {
            await Shell.Current.GoToAsync($"{nameof(EditCategoryPage)}?ID={ID}");
        }

        /// <summary>
        /// Gets updated categories from database and refreshes Categories collection.
        /// </summary>
        public async Task Refresh()
        {
            Categories.Clear();
            var categories = await CategoryService.GetCategories();
            foreach (var cat in categories)
                Categories.Add(cat);
        }
    }
}
