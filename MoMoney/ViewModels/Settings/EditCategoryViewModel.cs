using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings;

[QueryProperty(nameof(ID), nameof(ID))]
public partial class EditCategoryViewModel : ObservableObject
{
    readonly ICategoryService categoryService;

    [ObservableProperty]
    public ObservableCollection<Category> parents = new(); // list of categories

    [ObservableProperty]
    public Category category = new(); // selected category

    [ObservableProperty]
    public Category parent; // category parent

    [ObservableProperty]
    public string name; // category name

    public string ID { get; set; } // category ID

    public EditCategoryViewModel(ICategoryService _categoryService)
    {
        categoryService = _categoryService;
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
                await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
            }

            return;
        }

        await Shell.Current.DisplayAlert("Category Not Found Error", $"{ID} is not a valid ID", "OK");
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
