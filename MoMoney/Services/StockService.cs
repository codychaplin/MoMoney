using MoMoney.Data;
using MoMoney.Models;
using MoMoney.Exceptions;
using Android.Webkit;

namespace MoMoney.Services;

/// <inheritdoc />
public class StockService : IStockService
{
    readonly MoMoneydb momoney;
    readonly ILoggerService<StockService> logger;

    public Dictionary<string, Stock> Stocks { get; private set; } = new();

    public StockService(MoMoneydb _momoney, ILoggerService<StockService> _logger)
    {
        momoney = _momoney;
        logger = _logger;
    }

    public async Task Init()
    {
        await momoney.Init();
        if (!Stocks.Any())
            Stocks = await GetStocksAsDict();
    }

    public async Task AddStock(string symbol, int quantity, decimal cost, decimal marketprice, decimal bookvalue)
    {
        await Init();
        ValidateStock(symbol, quantity, cost, marketprice, bookvalue);

        var res = await momoney.db.Table<Stock>().CountAsync(s => s.Symbol == symbol);
        if (res > 0)
            throw new DuplicateStockException("Stock symbol'" + symbol + "' already exists");

        var stock = new Stock
        {
            Symbol = symbol,
            Quantity = quantity,
            Cost = cost,
            MarketPrice = marketprice,
            BookValue = bookvalue
        };

        await momoney.db.InsertAsync(stock);
        Stocks.Add(stock.Symbol, stock);
        await logger.LogInfo($"Added Stock '{stock.Symbol}' to db.");
    }

    public async Task AddStocks(List<Stock> stocks)
    {
        await Init();
        var dbStocks = await momoney.db.Table<Stock>().ToListAsync();

        // checks if names of any new stocks matches any names from db and throw exception if true
        bool containsDuplicates = stocks.Any(s => dbStocks.Select(dbs => dbs.Symbol).Contains(s.Symbol));
        if (containsDuplicates)
            throw new DuplicateStockException("Imported stocks contained duplicates. Please try again");

        // adds stocks to db and dictionary
        await momoney.db.InsertAllAsync(stocks);
        foreach (var stk in stocks)
            Stocks.Add(stk.Symbol, stk);

        await logger.LogInfo($"Added {stocks.Count} Stocks to db.");
    }

    public async Task UpdateStock(Stock updatedStock)
    {
        await Init();
        ValidateStock(updatedStock.Symbol, updatedStock.Quantity, updatedStock.Cost, updatedStock.MarketPrice, updatedStock.BookValue);
        await momoney.db.UpdateAsync(updatedStock);
        Stocks[updatedStock.Symbol] = updatedStock;
        await logger.LogInfo($"Updated Stock '{updatedStock.Symbol}' in db.");
    }

    public async Task UpdateStock(Stock updatedStock, Stock oldStock)
    {
        await Init();
        ValidateStock(updatedStock.Symbol, updatedStock.Quantity, updatedStock.Cost, updatedStock.MarketPrice, updatedStock.BookValue);
        await momoney.db.DeleteAsync(oldStock);
        await momoney.db.InsertAsync(updatedStock);
        Stocks.Remove(oldStock.Symbol);
        Stocks[updatedStock.Symbol] = updatedStock;
        await logger.LogInfo($"Updated Stock '{updatedStock.Symbol}' in db.");
    }

    public async Task RemoveStock(string symbol)
    {
        await Init();
        await momoney.db.DeleteAsync<Stock>(symbol);
        Stocks.Remove(symbol);
        await logger.LogInfo($"Removed Stock '{symbol}' from db.");
    }

    public async Task RemoveStocks()
    {
        await Init();
        await momoney.db.DeleteAllAsync<Stock>();
        await momoney.db.DropTableAsync<Stock>();
        await momoney.db.CreateTableAsync<Stock>();
        Stocks.Clear();
        await logger.LogInfo($"Removed all Stocks from db.");
    }

    public async Task<Stock> GetStock(string symbol)
    {
        await Init();
        if (Stocks.TryGetValue(symbol, out var stock))
            return new Stock(stock);

        var stk = await momoney.db.Table<Stock>().FirstOrDefaultAsync(s => s.Symbol == symbol);
        if (stk is null)
            throw new StockNotFoundException($"Could not find Stock with symbol '{symbol}'.");
        else
            return stk;
    }

    public async Task<List<Stock>> GetStocks()
    {
        await Init();
        if (Stocks.Any())
            return Stocks.Values.ToList();

        return await momoney.db.Table<Stock>().ToListAsync();
    }

    /// <summary>
    /// Gets all Stocks from Stocks table as a list.
    /// </summary>
    /// <returns>List of Stock objects</returns>
    async Task<Dictionary<string, Stock>> GetStocksAsDict()
    {
        await momoney.Init();
        var stocks = await momoney.db.Table<Stock>().ToListAsync();
        return stocks.ToDictionary(s => s.Symbol, s => s);
    }

    /// <summary>
    /// Validates input fields for Stocks.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="quantity"></param>
    /// <param name="cost"></param>
    /// <param name="marketPrice"></param>
    /// <param name="bookValue"></param>
    /// <exception cref="InvalidStockException"></exception>
    static void ValidateStock(string symbol, int quantity, decimal cost, decimal marketPrice, decimal bookValue)
    {
        if (symbol == "")
            throw new InvalidStockException("Invalid symbol");
        if (quantity < 0)
            throw new InvalidStockException("Quantity must be > 0");
        if (cost <= 0)
            throw new InvalidStockException("Cost must be > 0");
        if (marketPrice <= 0)
            throw new InvalidStockException("Market Price must be > 0");
        if (bookValue <= 0)
            throw new InvalidStockException("Book Value must be > 0");
    }
}