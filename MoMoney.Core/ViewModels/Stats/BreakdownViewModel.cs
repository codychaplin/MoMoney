using System.Collections.ObjectModel;
using Color = Microsoft.Maui.Graphics.Color;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Core.Models;
using MoMoney.Core.Services;
using MoMoney.Core.Helpers;

namespace MoMoney.Core.ViewModels.Stats;

public partial class BreakdownViewModel : ObservableObject
{
    readonly ICategoryService categoryService;
    readonly ITransactionService transactionService;

    [ObservableProperty]
    public decimal incomeSum = 0;
    [ObservableProperty]
    public decimal expenseSum = 0;

    [ObservableProperty]
    public List<Brush> incomePalette = new();
    [ObservableProperty]
    public List<Brush> expensePalette = new();

    [ObservableProperty]
    public ObservableCollection<BreakdownData> incomeData = new();
    [ObservableProperty]
    public ObservableCollection<BreakdownData> expenseData = new();

    [ObservableProperty]
    public DateTime selectedTime = new();

    [ObservableProperty]
    public string type = "Month";

    List<DateTime> Months = new();
    List<DateTime> Years = new();

    [ObservableProperty]
    public int index = 0;
    int timeIndex = 0;

    EventHandler<EventArgs> OnUpdate { get; set; }

    public BreakdownViewModel(ITransactionService _transactionService, ICategoryService _categoryService)
    {
        transactionService = _transactionService;
        categoryService = _categoryService;
    }

    public async void Init(object s, EventArgs e)
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

        // set index, set SelectedMonth, subscribe Update to EventHandler, then invoke EventHandler
        timeIndex = Months.Count - 1;
        SelectedTime = Months[timeIndex];
        OnUpdate += Update;
        OnUpdate?.Invoke(this, new EventArgs());
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
    public void ChangeType()
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

        OnUpdate?.Invoke(this, new EventArgs());
    }

    [RelayCommand]
    public void Decrement()
    {
        var timeType = (Type == "Month") ? Months : Years;
        if (timeIndex > 0)
            SelectedTime = timeType[--timeIndex];

        OnUpdate?.Invoke(this, new EventArgs());
    }

    [RelayCommand]
    public void Increment()
    {
        var timeType = (Type == "Month") ? Months : Years;
        if (timeIndex < timeType.Count - 1)
            SelectedTime = timeType[++timeIndex];

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async void Update(object s, EventArgs e)
    {
        // gets start/end dates then gets transactions between those dates
        DateTime from;
        DateTime to;
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
        var transactions = await transactionService.GetTransactionsFromTo(from, to, false);
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
            await UpdateExpenses(transactions);
        else
            await UpdateIncome(transactions);
    }

    /// <summary>
    /// Updates data for ExpenseData
    /// </summary>
    /// <param name="transactions"></param>
    async Task UpdateExpenses(IEnumerable<Transaction> transactions)
    {
        // group transactions by Category, sum amounts, get Category name from ID, assign colour from palette
        int i = 0;
        ExpenseData = new ObservableCollection<BreakdownData>(
            await Task.Run(() =>
                transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID)
                            .GroupBy(t => t.CategoryID)
                            .Select(group => {
                            var amount = Math.Abs(group.Sum(t => t.Amount));
                                return new BreakdownData
                                {
                                    Amount = amount,
                                    ActualAmount = amount,
                                    Category = categoryService.Categories[group.FirstOrDefault().CategoryID].CategoryName,
                                    Color = ExpensePalette[i++]
                                };
                            })));

        // calculate size of slice as percentage
        decimal total = ExpenseData.Sum(d => d.Amount);
        foreach (var item in ExpenseData)
            item.Percentage = item.Amount / total;
    }

    /// <summary>
    /// Updates data for IncomeData
    /// </summary>
    /// <param name="transactions"></param>
    async Task UpdateIncome(IEnumerable<Transaction> transactions)
    {
        // group transactions by Subcategory, sum amounts, get Subcategory name from ID, assign colour from palette
        int i = 0;
        IncomeData = new ObservableCollection<BreakdownData>(
            await Task.Run(() =>
                transactions.Where(t => t.CategoryID == Constants.INCOME_ID)
                            .GroupBy(t => t.SubcategoryID)
                            .Select(group => {
                                var amount = group.Sum(t => t.Amount);
                                return new BreakdownData
                                {
                                    ActualAmount = amount,
                                    Amount = (amount > 0) ? amount : 0,
                                    Category = categoryService.Categories[group.FirstOrDefault().SubcategoryID].CategoryName,
                                    Color = IncomePalette[i++]
                                };
                            })));

        // calculate size of slice as percentage
        decimal total = IncomeData.Sum(d => d.Amount);
        foreach (var item in IncomeData)
            item.Percentage = item.Amount / total;
    }
}