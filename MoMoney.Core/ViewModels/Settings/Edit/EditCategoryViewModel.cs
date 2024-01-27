using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

[QueryProperty(nameof(ID), nameof(ID))]
public partial class EditCategoryViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ILoggerService<EditCategoryViewModel> logger;

    [ObservableProperty]
    public ObservableCollection<Category> parents = new(); // list of categories

    [ObservableProperty]
    public Category category = new(); // selected category

    [ObservableProperty]
    public Category parent; // category parent

    [ObservableProperty]
    public string name; // category name

    public string ID { get; set; } // category ID

    public EditCategoryViewModel(ICategoryService _categoryService, ILoggerService<EditCategoryViewModel> _logger)
    {
        categoryService = _categoryService;
        logger = _logger;
    }

    /// <summary>
    /// Gets category using ID.
    /// </summary>
    public async Task GetCategory()
    {
        if (int.TryParse(ID, out int id))
        {
            try
            {
                Category = await categoryService.GetCategory(id);
                if (!string.IsNullOrEmpty(Category.ParentName))
                    Parent = await categoryService.GetParentCategory(Category.ParentName);
            }
            catch (CategoryNotFoundException ex)
            {
                await logger.LogError(ex.Message, ex.GetType().Name);
                await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
            }

            return;
        }

        string message = $"{ID} is not a valid ID";
        await logger.LogError(message);
        await Shell.Current.DisplayAlert("Category Not Found Error", message, "OK");
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

    /// <summary>
    /// Edits Category in database using input fields from view.
    /// </summary>
    [RelayCommand]
    async Task Edit()
    {
        if (Category is null || string.IsNullOrEmpty(Category.CategoryName))
        {
            // if invalid, display error
            await Shell.Current.DisplayAlert("Error", "Information not valid", "OK");
            return;
        }

        Category.ParentName = Parent.CategoryName;
        await categoryService.UpdateCategory(Category);
        await Shell.Current.GoToAsync("..");
    }

    /// <summary>
    /// Removes the Category from the database.
    /// </summary>
    [RelayCommand]
    async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Category.CategoryName}\"?", "Yes", "No");

        if (!flag)
            return;

        await categoryService.RemoveCategory(Category.CategoryID);
        await Shell.Current.GoToAsync("..");
    }
}
