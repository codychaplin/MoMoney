using System.Collections.ObjectModel;
using Color = Microsoft.Maui.Graphics.Color;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Stats;

public partial class BreakdownViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;
    readonly ILoggerService<BreakdownViewModel> logger;

    [ObservableProperty] decimal incomeSum = 0;
    [ObservableProperty] decimal expenseSum = 0;

    [ObservableProperty] List<Brush> incomePalette = [];
    [ObservableProperty] List<Brush> expensePalette = [];

    [ObservableProperty] ObservableCollection<BreakdownData> incomeData = [];
    [ObservableProperty] ObservableCollection<BreakdownData> expenseData = [];

    [ObservableProperty] DateTime selectedTime = new();

    [ObservableProperty] string type = "Month";

    [ObservableProperty] string showValue = "$0";

    [ObservableProperty] int index = 0;
    partial void OnIndexChanged(int value) => _ = UpdateBreakdown();
    
    int timeIndex = 0;

    List<DateTime> Months = [];
    List<DateTime> Years = [];

    DateTime cachedFrom;
    DateTime cachedTo;
    IEnumerable<Transaction> cachedTransactions = [];

    public BreakdownViewModel(ITransactionService _transactionService, ICategoryService _categoryService, ILoggerService<BreakdownViewModel> _loggerService)
    {
        transactionService = _transactionService;
        categoryService = _categoryService;
        logger = _loggerService;
        logger.LogFirebaseEvent(FirebaseParameters.EVENT_VIEW_BREAKDOWN, FirebaseParameters.GetFirebaseParameters());
        ShowValue = Utilities.ShowValue ? "$0" : "$?";
    }

    public async void Init(object s, EventArgs e)
    {
        try
        {
            InitPalettes();

            var first = await transactionService.GetFirstTransaction();
            if (first is null)
            {
                Months.Add(DateTime.Today);
                Years.Add(DateTime.Today);
                SelectedTime = Months[0];
                return;
            }

            // get date of first transaction, today's date, and add each month to collection
            DateTime start = new(first.Date.Year, first.Date.Month, 1);
            DateTime end = new(DateTime.Today.Year, DateTime.Today.Month, 1);
            while (start <= end)
            {
                Months.Add(start);
                start = start.AddMonths(1);
            }

            // create list of years
            for (int i = first.Date.Year; i <= DateTime.Today.Year; i++)
                Years.Add(new DateTime(i, 1, 1));

            // set index, set SelectedMonth, and call UpdateBreakdown
            timeIndex = Months.Count - 1;
            SelectedTime = Months[timeIndex];

            cachedFrom = new(DateTime.Today.Year, 1, 1);
            cachedTo = new(DateTime.Today.Year, 12, 31);
            cachedTransactions = await transactionService.GetTransactionsFromTo(cachedFrom, cachedTo, false);
            await UpdateBreakdown();
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(Init), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    void InitPalettes()
    {
        // expense colour palette
        ExpensePalette.Add(Color.FromArgb("9d0208"));
        ExpensePalette.Add(Color.FromArgb("e85d04"));
        ExpensePalette.Add(Color.FromArgb("faa307"));
        ExpensePalette.Add(Color.FromArgb("83e377"));
        ExpensePalette.Add(Color.FromArgb("16db93"));
        ExpensePalette.Add(Color.FromArgb("0db39e"));
        ExpensePalette.Add(Color.FromArgb("048ba8"));
        ExpensePalette.Add(Color.FromArgb("2c699a"));
        ExpensePalette.Add(Color.FromArgb("54478c"));

        // income colour palette
        IncomePalette.Add(Color.FromArgb("155d27"));
        IncomePalette.Add(Color.FromArgb("1a7431"));
        IncomePalette.Add(Color.FromArgb("208b3a"));
        IncomePalette.Add(Color.FromArgb("25a244"));
        IncomePalette.Add(Color.FromArgb("2dc653"));
        IncomePalette.Add(Color.FromArgb("4ad66d"));
        IncomePalette.Add(Color.FromArgb("6ede8a"));
        IncomePalette.Add(Color.FromArgb("92e6a7"));
    }

    [RelayCommand]
    async Task ChangeType()
    {
        if (Type == "Month")
        {
            Type = "Year";
            timeIndex = Years.Count - 1;
            SelectedTime = Years[timeIndex];
        }
        else
        {
            Type = "Month";
            timeIndex = Months.Count - 1;
            SelectedTime = Months[timeIndex];
        }

        await UpdateBreakdown();
    }

    [RelayCommand]
    async Task Decrement()
    {
        var timeType = (Type == "Month") ? Months : Years;
        if (timeIndex > 0)
            SelectedTime = timeType[--timeIndex];

        await UpdateBreakdown();
    }

    [RelayCommand]
    async Task Increment()
    {
        var timeType = (Type == "Month") ? Months : Years;
        if (timeIndex < timeType.Count - 1)
            SelectedTime = timeType[++timeIndex];

        await UpdateBreakdown();
    }

    [RelayCommand]
    async Task UpdateBreakdown()
    {
        try
        {
            // gets start/end dates then gets transactions between those dates
            DateTime from;
            DateTime to;
            IEnumerable<Transaction> transactions = [];
            if (Type == "Month")
            {
                from = new(SelectedTime.Year, SelectedTime.Month, 1);
                to = new(SelectedTime.Year, SelectedTime.Month, SelectedTime.AddMonths(1).AddDays(-1).Day);
            }
            else
            {
                from = new(SelectedTime.Year, 1, 1);
                to = new(SelectedTime.Year, 12, 31);
            }

            // if the selected time is outside of the cached range, update cached data
            if (from < cachedFrom || to > cachedTo)
            {
                cachedFrom = new(SelectedTime.Year, 1, 1);
                cachedTo = new(SelectedTime.Year, 12, 31);
                cachedTransactions = await transactionService.GetTransactionsFromTo(cachedFrom, cachedTo, false);
            }

            transactions = cachedTransactions.Where(t => t.Date >= from && t.Date <= to);
            if (!transactions.Any())
            {
                ExpenseSum = 0;
                IncomeSum = 0;
                ExpenseData.Clear();
                IncomeData.Clear();
                return;
            }

            // calculates sums for tab headers
            // absolute value for expenses just to make it look cleaner
            ExpenseSum = Math.Abs(transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID)
                                              .Select(t => t.Amount).Sum());
            IncomeSum = transactions.Where(t => t.CategoryID == Constants.INCOME_ID)
                                    .Select(t => t.Amount).Sum();

            // first tab = expenses, second tab = income
            if (Index == 0)
                UpdateExpenses(transactions);
            else
                UpdateIncome(transactions);
        }
        catch (Exception ex)
        {
            await logger.LogError(nameof(UpdateBreakdown), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    /// <summary>
    /// Updates data for ExpenseData
    /// </summary>
    /// <param name="transactions"></param>
    void UpdateExpenses(IEnumerable<Transaction> transactions)
    {
        // group transactions by Category, sum amounts, get Category name from ID, assign colour from palette
        int i = 0;
        var expenseData = transactions
            .Where(t => t.CategoryID >= Constants.EXPENSE_ID)
            .GroupBy(t => t.CategoryID)
            .OrderByDescending(g => Math.Abs(g.Sum(t => t.Amount)))
            .Select(group =>
            {
                var amount = Math.Abs(group.Sum(t => t.Amount));
                return new BreakdownData
                {
                    Amount = amount,
                    ActualAmount = amount,
                    Category = categoryService.Categories[group.Key].CategoryName,
                    Color = ExpensePalette[i++]
                };
            });

        ExpenseData.Clear();
        foreach (var item in expenseData)
            ExpenseData.Add(item);

        // calculate size of slice as percentage
        decimal total = ExpenseData.Sum(d => d.Amount);
        foreach (var item in ExpenseData)
            item.Percentage = item.Amount / total;
    }

    /// <summary>
    /// Updates data for IncomeData
    /// </summary>
    /// <param name="transactions"></param>
    void UpdateIncome(IEnumerable<Transaction> transactions)
    {
        // group transactions by Subcategory, sum amounts, get Subcategory name from ID, assign colour from palette
        int i = 0;
        var incomeData = transactions
            .Where(t => t.CategoryID == Constants.INCOME_ID)
            .GroupBy(t => t.SubcategoryID)
            .OrderByDescending(g => g.Sum(t => t.Amount))
            .Select(group =>
            {
                var amount = group.Sum(t => t.Amount);
                return new BreakdownData
                {
                    ActualAmount = amount,
                    Amount = (amount > 0) ? amount : 0,
                    Category = categoryService.Categories[group.Key].CategoryName,
                    Color = IncomePalette[i++]
                };
            });

        IncomeData.Clear();
        foreach (var item in incomeData)
            IncomeData.Add(item);

        // calculate size of slice as percentage
        decimal total = IncomeData.Sum(d => d.Amount);
        foreach (var item in IncomeData)
            item.Percentage = item.Amount / total;
    }
}