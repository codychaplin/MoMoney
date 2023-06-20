using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings.Edit;

public partial class AddCategoryViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ILoggerService<AddCategoryViewModel> logger;

    [ObservableProperty]
    public ObservableCollection<Category> parents = new(); // list of categories

    [ObservableProperty]
    public string name; // category name

    [ObservableProperty]
    public string parent; // category parent

    public AddCategoryViewModel(ICategoryService _categoryService, ILoggerService<AddCategoryViewModel> _logger)
    {
        categoryService = _categoryService;
        logger = _logger;
    }

    /// <summary>
    /// adds Category to database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Add()
    {
        try
        {
            await categoryService.AddCategory(Name, Parent);
            await Shell.Current.GoToAsync("..");
        }
        catch (DuplicateCategoryException ex)
        {
            await logger.LogWarning(ex.Message, ex.GetType().Name);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Gets parent categories from database.
    /// </summary>
    public async Task GetParents()
    {
        var categories = await categoryService.GetParentCategories();
        Parents.Clear();
        foreach (var cat in categories)
            Parents.Add(cat);
    }
}