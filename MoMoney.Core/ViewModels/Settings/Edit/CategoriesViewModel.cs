using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services.Interfaces;
using MoMoney.Core.Helpers;

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
    public async Task LoadCategories()
    {
        try
        {
            var categories = await categoryService.GetCategories();

            // groups categories by parent except where ParentCategoryID == null
            // new parent categories will not show up in the list until a subcategory is added
            var groupedCategories = categories
                .Where(c => c.ParentCategoryID != null)
                .GroupBy(c => c.ParentCategoryID)
                .Select(group =>
                {
                    var parent = categories.FirstOrDefault(c => c.CategoryID == group.Key);
                    return new CategoryGroup(parent!, group);
                });
            Categories.Clear();
            foreach (var category in groupedCategories)
                Categories.Add(category);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(LoadCategories), ex);
            await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Goes to the add version of EditCategoryPage.xaml.
    /// </summary>
    [RelayCommand]
    async Task GoToAddCategory()
    {
        await Shell.Current.GoToAsync("EditCategoryPage", new ShellNavigationQueryParameters() { { "Category", null! } });
    }

    /// <summary>
    /// Goes to EditCategoryPage.xaml with a Category ID as a parameter.
    /// </summary>
    [RelayCommand]
    async Task GoToEditCategory(Category category)
    {
        await Shell.Current.GoToAsync($"EditCategoryPage", new ShellNavigationQueryParameters() { { "Category", category } });
    }
}