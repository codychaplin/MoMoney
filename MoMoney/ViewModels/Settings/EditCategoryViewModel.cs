using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MoMoney.Services;
using MoMoney.Models;

namespace MoMoney.ViewModels.Settings
{
    [QueryProperty(nameof(ID), nameof(ID))]
    public partial class EditCategoryViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Category> parents = new(); // list of categories

        [ObservableProperty]
        public Category category = new(); // selected category

        [ObservableProperty]
        public Category parent; // category parent

        [ObservableProperty]
        public string name; // category name

        public string ID { get; set; } // category ID
        int id = 0;

        /// <summary>
        /// Gets category using ID
        /// </summary>
        public async Task GetCategory()
        {
            if (int.TryParse(ID, out id))
            {
                Category = await CategoryService.GetCategory(id);
                Parent = await CategoryService.GetParentCategory(Category.ParentName);
            }
            else
                await Shell.Current.DisplayAlert("Error", "Could not find category", "OK");
        }

        /// <summary>
        /// Gets parent categories from database
        /// </summary>
        public async Task GetParents()
        {
            Parents.Clear();
            var categories = await CategoryService.GetParentCategories();
            foreach (var cat in categories)
                Parents.Add(cat);
        }

        /// <summary>
        /// Edits Category in database using input fields from view
        /// </summary>
        [RelayCommand]
        async Task Edit()
        {
            if (Category is null || string.IsNullOrEmpty(Category.CategoryName))
            {
                // if invalid, display error
                await Shell.Current.DisplayAlert("Error", "Information not valid", "OK");
            }
            else
            {
                Category = new Category // if valid, update Category
                {
                    CategoryID = id,
                    CategoryName = Category.CategoryName,
                    ParentName = Parent.CategoryName,
                    Enabled = Category.Enabled
                };

                await CategoryService.UpdateCategory(Category);
                await Shell.Current.GoToAsync("..");
            }
        }

        /// <summary>
        /// Removes the Category from the database
        /// </summary>
        [RelayCommand]
        async Task Remove()
        {
            bool flag = await Shell.Current.DisplayAlert("", $"Are you sure you want to delete \"{Category.CategoryName}\"", "Yes", "No");

            if (flag)
            {
                await CategoryService.RemoveCategory(Category.CategoryID);
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}
