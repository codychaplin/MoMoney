using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class CategoriesViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ILoggerService<CategoriesViewModel> logger;

    [ObservableProperty] ObservableCollection<CategoryGroup> categories = [];

    public CategoriesViewModel(ICategoryService _categoryService, ILoggerService<CategoriesViewModel> _logger)
    {
        categoryService = _categoryService;
        logger = _logger;
    }

    /// <summary>
    /// Gets updated categories from database and refreshes Categories collection.
    /// </summary>
    public async void RefreshCategories(object s, EventArgs e)
    {
        try
        {
            await Task.Delay(1);
            var categories = await categoryService.GetCategories();
            Categories.Clear();

            // groups categories by parent except where ParentName == ""
            // new parent categories will not show up in the list until a subcategory is added
            foreach (var cat in categories.GroupBy(c => c.ParentName))
                if (!string.IsNullOrEmpty(cat.Key))
                    Categories.Add(new CategoryGroup(cat));
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RefreshCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to AddCategoryPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddCategory()
    {
        await Shell.Current.GoToAsync("AddCategoryPage");
    }

    /// <summary>
    /// Goes to EditCategoryPage.xaml with a Category ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditCategory(int ID)
    {
        await Shell.Current.GoToAsync($"EditCategoryPage?ID={ID}");
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
            await Shell.Current.GoToAsync($"EditCategoryPage?ID={cat.CategoryID}");
        }
        catch (CategoryNotFoundException ex)
        {
            await logger.LogError(nameof(GoToEditCategoryString), ex);
            await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GoToEditCategoryString), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}