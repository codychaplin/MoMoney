using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Settings.Edit;

public partial class EditCategoryViewModel : BaseEditViewModel<ICategoryService, EditCategoryViewModel>
{
    [ObservableProperty] ObservableCollection<Category> parents = []; // list of categories
    [ObservableProperty] Category category = new(); // selected category
    [ObservableProperty] Category parent; // category parent

    public EditCategoryViewModel(ICategoryService _categoryService, ILoggerService<EditCategoryViewModel> _logger) : base(_categoryService, _logger) { }

    /// <summary>
    /// Controls whether the view is in edit mode or not.
    /// </summary>
    /// <param name="query"></param>
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query["Category"] is not Category category)
        {
            Task.Run(GetParents);
            return;
        }

        IsEditMode = true;
        Category = new(category);

        Task.Run(async () =>
        {
            try
            {
                await GetParents();

                if (!string.IsNullOrEmpty(Category.ParentName))
                    Parent = await service.GetParentCategory(Category.ParentName);
            }
            catch (CategoryNotFoundException ex)
            {
                await logger.LogError(nameof(ApplyQueryAttributes), ex);
                await Shell.Current.DisplayAlert("Category Not Found Error", ex.Message, "OK");
            }
            catch (Exception ex)
            {
                await logger.LogError(nameof(ApplyQueryAttributes), ex);
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        });
    }

    /// <summary>
    /// Gets parent categories from database.
    /// </summary>
    async Task GetParents()
    {
        try
        {
            var categories = await service.GetParentCategories();
            Parents.Clear();
            foreach (var category in categories)
                Parents.Add(category);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(GetParents), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// adds Category to database using input fields from view.
    /// </summary>
    [RelayCommand]
    protected override async Task Add()
    {
        try
        {
            await service.AddCategory(Category.CategoryName, Parent.CategoryName);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_ADD_CATEGORY, FirebaseParameters.GetFirebaseParameters());

            // if parent category, notify the user that it won't show up until a subcategory is added
            if (Parent == null)
                _ = Shell.Current.DisplayAlert("Note", "Parent categories will only appear in this list once a subcategory has been added.", "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (DuplicateCategoryException ex)
        {
            await logger.LogWarning(nameof(Add), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Add), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Edits Category in database using input fields from view.
    /// </summary>
    [RelayCommand]
    protected override async Task Edit()
    {
        if (Category is null || string.IsNullOrEmpty(Category.CategoryName))
        {
            // if invalid, display error
            await Shell.Current.DisplayAlert("Error", "Information not valid", "OK");
            return;
        }

        try
        {
            Category.ParentName = Parent.CategoryName;
            await service.UpdateCategory(Category);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_EDIT_CATEGORY, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Edit), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Removes the Category from the database.
    /// </summary>
    [RelayCommand]
    protected override async Task Remove()
    {
        bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Category.CategoryName}\"?", "Yes", "No");
        if (!flag)
            return;

        try
        {
            await service.RemoveCategory(Category.CategoryID);
            logger.LogFirebaseEvent(FirebaseParameters.EVENT_DELETE_CATEGORY, FirebaseParameters.GetFirebaseParameters());
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Remove), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }
}