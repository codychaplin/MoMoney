using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Views.Settings;

namespace MoMoney.ViewModels.Settings;

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
    public async void Refresh(object s, EventArgs e)
    {
        var categories = await CategoryService.GetCategories();
        Categories.Clear();
        foreach (var cat in categories)
            Categories.Add(cat);
    }
}
