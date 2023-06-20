using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;
using MoMoney.Views.Settings.Edit;

namespace MoMoney.ViewModels.Settings.Edit;

public partial class CategoriesViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ILoggerService<CategoriesViewModel> logger;

    [ObservableProperty]
    public ObservableCollection<CategoryGroup> categories = new();

    public CategoriesViewModel(ICategoryService _categoryService, ILoggerService<CategoriesViewModel> _logger)
    {
        categoryService = _categoryService;
        logger = _logger;
    }

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
        try
        {
            var cat = await categoryService.GetParentCategory(name);
            await Shell.Current.GoToAsync($"{nameof(EditCategoryPage)}?ID={cat.CategoryID}");
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogError(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets updated categories from database and refreshes Categories collection.
    /// </summary>
    public async void Refresh(object s, EventArgs e)
    {
        var categories = await categoryService.GetCategories();
        Categories.Clear();
        // groups categories by parent except where ParentName == ""
        foreach (var cat in categories.GroupBy(c => c.ParentName))
            if (!string.IsNullOrEmpty(cat.Key))
                Categories.Add(new CategoryGroup(cat));
    }
}