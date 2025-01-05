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

    [ObservableProperty] decimal incomeSum;
    [ObservableProperty] decimal expenseSum;

    [ObservableProperty] List<Brush> incomePalette = [];
    [ObservableProperty] List<Brush> expensePalette = [];

    [ObservableProperty] ObservableCollection<BreakdownData> incomeData = [];
    [ObservableProperty] ObservableCollection<BreakdownData> expenseData = [];

    [ObservableProperty] DateTime selectedTime = new();

    [ObservableProperty] string type = "Month";

    [ObservableProperty] string showValue = "$0";

    [ObservableProperty] int index;
    partial void OnIndexChanged(int value) => _ = UpdateBreakdown();
    
    int timeIndex;

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
        ShowValue = Utilities.ShowValue ? "C0" : "$?";
    }

    public async Task LoadBreakdown()
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
            await logger.LogError(nameof(LoadBreakdown), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    void InitPalettes()
    {
        // expense colour palette
        ExpensePalette.Add(Color.FromArgb("9D0208")); // red
        ExpensePalette.Add(Color.FromArgb("EB7F08")); // orange
        ExpensePalette.Add(Color.FromArgb("D6BF0F")); // yellow
        ExpensePalette.Add(Color.FromArgb("79BD2B")); // lime
        ExpensePalette.Add(Color.FromArgb("24B37F")); // teal-green
        ExpensePalette.Add(Color.FromArgb("2E5B99")); // blue
        ExpensePalette.Add(Color.FromArgb("55408F")); // purple
        ExpensePalette.Add(Color.FromArgb("A15FD6")); // purple-pink
        ExpensePalette.Add(Color.FromArgb("C261B5")); // pink
        ExpensePalette.Add(Color.FromArgb("AD4B49")); // faded red

        // income colour palette
        IncomePalette.Add(Color.FromArgb("008000")); // start green
        IncomePalette.Add(Color.FromArgb("1F991F"));
        IncomePalette.Add(Color.FromArgb("47B347"));
        IncomePalette.Add(Color.FromArgb("43CC79"));
        IncomePalette.Add(Color.FromArgb("49D59F")); // start teal
        IncomePalette.Add(Color.FromArgb("50BDA9"));
        IncomePalette.Add(Color.FromArgb("21948A"));
        IncomePalette.Add(Color.FromArgb("026969"));
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
                var color = ExpensePalette[i++ % ExpensePalette.Count];
                return new BreakdownData
                {
                    Amount = amount,
                    ActualAmount = amount,
                    Category = categoryService.Categories[group.Key].CategoryName,
                    Color = color
                };
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        // calculate size of slice as percentage
        ExpenseData.Clear();
        decimal total = expenseData.Sum(d => d.Amount);
        foreach (var item in expenseData)
        {
            if (total > 0)
                item.Percentage = item.Amount / total;
            ExpenseData.Add(item);
        }
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
                var color = IncomePalette[i++ % IncomePalette.Count];
                return new BreakdownData
                {
                    ActualAmount = amount,
                    Amount = (amount > 0) ? amount : 0,
                    Category = categoryService.Categories[group.Key].CategoryName,
                    Color = color
                };
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        IncomeData.Clear();
        decimal total = incomeData.Sum(d => d.Amount);
        foreach (var item in incomeData)
        {
            if (total > 0)
                item.Percentage = item.Amount / total;
            IncomeData.Add(item);
        }
    }
}