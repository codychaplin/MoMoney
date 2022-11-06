using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using System.Collections.ObjectModel;

namespace MoMoney.ViewModels
{
    public partial class AddTransactionViewModel : ObservableObject
    {
        [ObservableProperty]
        public ObservableCollection<Account> accounts = new();

        [ObservableProperty]
        public ObservableCollection<Category> categories = new();

        [ObservableProperty]
        public ObservableCollection<Category> subcategories = new();

        /// <summary>
        /// Gets updated accounts from database and refreshes Accounts collection
        /// </summary>
        public async Task Refresh()
        {
            // updates accounts
            Accounts.Clear();
            var accounts = await AccountService.GetActiveAccounts();
            foreach (var acc in accounts)
                Accounts.Add(acc);

            // updates categories
            Categories.Clear();
            var categories = await CategoryService.GetActiveParentCategories();
            foreach (var cat in categories)
                Categories.Add(cat);

            //clears subcategories
            Subcategories.Clear();
        }

        /// <summary>
        /// Updates Subcategories based on selected parent Category
        /// </summary>
        /// <param name="parentCategory"></param>
        public async Task GetSubcategories(Category parentCategory)
        {
            Subcategories.Clear();
            if (parentCategory is not null)
            {
                var subcategories = await CategoryService.GetSubcategories(parentCategory);
                foreach (var cat in subcategories)
                    Subcategories.Add(cat);
            }
        }
    }
}
