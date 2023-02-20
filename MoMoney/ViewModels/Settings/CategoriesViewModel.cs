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
    public ObservableCollection<CategoryGroup> categories = new();

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
    /// Goes to EditCategoryPage.xaml with a Category ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditCategoryString(string name)
    {
        var cat = await CategoryService.GetParentCategory(name);
        await Shell.Current.GoToAsync($"{nameof(EditCategoryPage)}?ID={cat.CategoryID}");
    }

    /// <summary>
    /// Gets updated categories from database and refreshes Categories collection.
    /// </summary>
    public async void Refresh(object s, EventArgs e)
    {
        var categories = await CategoryService.GetCategories();
        Categories.Clear();
        // groups categories by parent except where ParentName == ""
        foreach (var cat in categories.GroupBy(c => c.ParentName))
            if (!string.IsNullOrEmpty(cat.Key))
                Categories.Add(new CategoryGroup(cat));
    }
}

public class CategoryGroup : List<Category>
{
    public string CategoryName { get; private set; }

    public CategoryGroup(IGrouping<string, Category> cat) : base(cat.ToList())
    {
        CategoryName = cat.Key;
    }
}