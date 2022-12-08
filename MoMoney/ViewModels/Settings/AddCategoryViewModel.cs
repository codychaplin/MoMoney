using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Settings
{
    public partial class AddCategoryViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Category> parents = new(); // list of categories

        [ObservableProperty]
        public string name; // category name

        [ObservableProperty]
        public string parent; // category parent

        /// <summary>
        /// adds Category to database using input fields from view.
        /// </summary>
        [RelayCommand]
        async Task Add()
        {
            await CategoryService.AddCategory(Name, Parent);
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Gets parent categories from database.
        /// </summary>
        public async Task GetParents()
        {
            Parents.Clear();
            var categories = await CategoryService.GetParentCategories();
            foreach (var cat in categories)
                Parents.Add(cat);
        }
    }
}
