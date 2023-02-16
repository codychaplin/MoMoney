using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Color = Microsoft.Maui.Graphics.Color;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Stats;

public partial class BreakdownViewModel : ObservableObject
{
    [ObservableProperty]
    public decimal incomeSum = 0;
    [ObservableProperty]
    public decimal expenseSum = 0;

    [ObservableProperty]
    public List<Brush> incomePalette = new();
    [ObservableProperty]
    public List<Brush> expensePalette = new();

    [ObservableProperty]
    public ObservableCollection<MonthData> incomeData = new();
    [ObservableProperty]
    public ObservableCollection<MonthData> expenseData = new();

    [ObservableProperty]
    public DateTime selectedMonth = new();
    List<DateTime> Months = new();

    [ObservableProperty]
    public int index = 0;
    int monthIndex = 0;

    

    EventHandler<EventArgs> OnUpdate { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public async void Init(object s, EventArgs e)
    {
        InitPalettes();

        // get date of first transaction, today's date, and add each month to collection
        var first = await TransactionService.GetFirstTransaction();
        DateTime start = new(first.Date.Year, first.Date.Month, 1);
        DateTime end = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        while (start <= end)
        {
            Months.Add(start);
            start = start.AddMonths(1);
        }

        // set index, set SelectedMonth, subscribe Update to EventHandler, then invoke EventHandler
        monthIndex = Months.Count - 1;
        SelectedMonth = Months[monthIndex];
        OnUpdate += Update;
        OnUpdate?.Invoke(this, new EventArgs());
    }

    void InitPalettes()
    {
        ExpensePalette.Add(Color.FromArgb("9d0208"));
        ExpensePalette.Add(Color.FromArgb("e85d04"));
        ExpensePalette.Add(Color.FromArgb("faa307"));
        ExpensePalette.Add(Color.FromArgb("83e377"));
        ExpensePalette.Add(Color.FromArgb("16db93"));
        ExpensePalette.Add(Color.FromArgb("0db39e"));
        ExpensePalette.Add(Color.FromArgb("048ba8"));
        ExpensePalette.Add(Color.FromArgb("2c699a"));
        ExpensePalette.Add(Color.FromArgb("54478c"));

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
    public void DecrementMonth()
    {
        if (monthIndex > 0)
            SelectedMonth = Months[--monthIndex];

        OnUpdate?.Invoke(this, new EventArgs());
    }

    [RelayCommand]
    public void IncrementMonth()
    {
        if (monthIndex < Months.Count - 1)
            SelectedMonth = Months[++monthIndex];

        OnUpdate?.Invoke(this, new EventArgs());
    }

    public async void Update(object s, EventArgs e)
    {
        // gets start/end dates then gets transactions between those dates
        DateTime from = new(SelectedMonth.Year, SelectedMonth.Month, 1);
        DateTime to = new(SelectedMonth.Year, SelectedMonth.Month, SelectedMonth.AddMonths(1).AddDays(-1).Day);
        var transactions = await TransactionService.GetTransactionsFromTo(from, to, false);
        
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
        ExpenseData = new ObservableCollection<MonthData>(
            await Task.Run(() =>
                transactions.Where(t => t.CategoryID >= Constants.EXPENSE_ID)
                            .GroupBy(t => t.CategoryID)
                            .Select(group => {
                            var amount = Math.Abs(group.Sum(t => t.Amount));
                                return new MonthData
                                {
                                    Amount = amount,
                                    ActualAmount = amount,
                                    Category = CategoryService.Categories[group.FirstOrDefault().CategoryID],
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
        IncomeData = new ObservableCollection<MonthData>(
            await Task.Run(() =>
                transactions.Where(t => t.CategoryID == Constants.INCOME_ID)
                            .GroupBy(t => t.SubcategoryID)
                            .Select(group => {
                                var amount = group.Sum(t => t.Amount);
                                return new MonthData
                                {
                                    ActualAmount = amount,
                                    Amount = (amount > 0) ? amount : 0,
                                    Category = CategoryService.Categories[group.FirstOrDefault().SubcategoryID],
                                    Color = IncomePalette[i++]
                                };
                            })));

        // calculate size of slice as percentage
        decimal total = IncomeData.Sum(d => d.Amount);
        foreach (var item in IncomeData)
            item.Percentage = item.Amount / total;
    }
}

public class MonthData
{
    public string Category { get; set; }
    public decimal Amount { get; set; }
    public decimal ActualAmount { get; set; }
    public Brush Color { get; set; }
    public decimal Percentage { get; set; }
}
