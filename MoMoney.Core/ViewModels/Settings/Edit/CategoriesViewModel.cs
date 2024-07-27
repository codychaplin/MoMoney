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

            // groups categories by parent except where ParentName == ""
            // new parent categories will not show up in the list until a subcategory is added
            var groupedCategories = categories.GroupBy(c => c.ParentName)
                .Where(cat => !string.IsNullOrEmpty(cat.Key))
                .Select(cat => new CategoryGroup(cat));
            Categories.Clear();
            foreach (var category in groupedCategories)
                Categories.Add(category);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(RefreshCategories), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to the add version of EditCategoryPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddCategory()
    {
        await Shell.Current.GoToAsync("EditCategoryPage", new ShellNavigationQueryParameters() { { "Category", null } });
    }

    /// <summary>
    /// Goes to EditCategoryPage.xaml with a Category ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditCategory(Category category)
    {
        await Shell.Current.GoToAsync($"EditCategoryPage", new ShellNavigationQueryParameters() { { "Category", category } });
    }

    /// <summary>
    /// Goes to EditCategoryPage.xaml with a Category ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditCategoryString(string name)
    {
        try
        {
            var category = await categoryService.GetParentCategory(name);
            await Shell.Current.GoToAsync($"EditCategoryPage", new ShellNavigationQueryParameters() { { "Category", category } });
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