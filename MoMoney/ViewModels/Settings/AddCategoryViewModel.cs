using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

namespace MoMoney.ViewModels.Settings;

public partial class AddCategoryViewModel : ObservableObject
{
    readonly ICategoryService categoryService;

    [ObservableProperty]
    public ObservableCollection<Category> parents = new(); // list of categories

    [ObservableProperty]
    public string name; // category name

    [ObservableProperty]
    public string parent; // category parent

    public AddCategoryViewModel(ICategoryService _categoryService)
    {
        categoryService = _categoryService;
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