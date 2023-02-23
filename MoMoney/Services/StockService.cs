using MoMoney.Models;
using MoMoney.Exceptions;

namespace MoMoney.Services;

public static class StockService
{
    public static Dictionary<string, Stock> Stocks { get; private set; } = new();

    /// <summary>
    /// Calls db Init.
    /// </summary>
    public static async Task Init()
    {
        if (!Stocks.Any())
            Stocks = await GetStocksAsDict();
    }

    /// <summary>
    /// Creates new Stock object and inserts into Stocks table.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="quantity"></param>
    /// <param name="cost"></param>
    /// <param name="marketprice"></param>
    /// <param name="bookvalue"></param>
    /// <exception cref="DuplicateStockException"></exception>
    public static async Task AddStock(string symbol, int quantity, decimal cost, decimal marketprice, decimal bookvalue)
    {
        ValidateStock(symbol, quantity, cost, marketprice, bookvalue);

        var res = await MoMoneydb.db.Table<Stock>().CountAsync(s => s.Symbol == symbol);
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

        // adds Account to db and dictionary
        await MoMoneydb.db.InsertAsync(stock);
        Stocks.Add(stock.Symbol, stock);
    }

    /// <summary>
    /// Inserts multiple Stock objects into Stocks table.
    /// </summary>
    /// <param name="stocks"></param>
    /// <exception cref="DuplicateStockException"></exception>
    public static async Task AddStocks(List<Stock> stocks)
    {
        // gets accounts from db
        var dbStocks = await MoMoneydb.db.Table<Stock>().ToListAsync();

        // checks if names of any new accounts matches any names from dbAccounts and throw exception if true
        bool containsDuplicates = stocks.Any(s => dbStocks.Select(dbs => dbs.Symbol).Contains(s.Symbol));
        if (containsDuplicates)
            throw new DuplicateStockException("Imported stocks contained duplicates. Please try again");

        // adds accounts to db and dictionary
        await MoMoneydb.db.InsertAllAsync(stocks);
        foreach (var stk in stocks)
            Stocks.Add(stk.Symbol, stk);
    }

    /// <summary>
    /// Given an Stock object, updates the corresponding stock in the Stocks table.
    /// </summary>
    /// <param name="updatedStock"></param>
    public static async Task UpdateStock(Stock updatedStock)
    {
        ValidateStock(updatedStock.Symbol, updatedStock.Quantity, updatedStock.Cost, updatedStock.MarketPrice, updatedStock.BookValue);

        // update Stock in db and dictionary
        await MoMoneydb.db.UpdateAsync(updatedStock);
        Stocks[updatedStock.Symbol] = updatedStock;
    }

    /// <summary>
    /// Removes Stock from Stocks table.
    /// </summary>
    /// <param name="symbol"></param>
    public static async Task RemoveStock(string symbol)
    {
        // remove Stock from db and dictionary
        await MoMoneydb.db.DeleteAsync<Stock>(symbol);
        Stocks.Remove(symbol);
    }

    /// <summary>
    /// Gets a stock from the Stocks table using a symbol.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns>Stock object</returns>
    public static async Task<Stock> GetStock(string symbol)
    {
        //eturn await MoMoneydb.db.Table<Stock>().FirstOrDefaultAsync(s => s.Symbol == symbol);
        if (Stocks.TryGetValue(symbol, out var stock))
            return stock;
        else
        {
            var stk = await MoMoneydb.db.Table<Stock>().FirstOrDefaultAsync(s => s.Symbol == symbol);
            if (stk is null)
                throw new StockNotFoundException($"Could not find Stock with symbol '{symbol}'.");
            else
                return stk;
        }
    }

    /// <summary>
    /// Gets all Stocks from Stocks table as a list.
    /// </summary>
    /// <returns>List of Stock objects</returns>
    public static async Task<List<Stock>> GetStocks()
    {
        return await MoMoneydb.db.Table<Stock>().ToListAsync();
    }

    /// <summary>
    /// Gets all Stocks from Stocks table as a list.
    /// </summary>
    /// <returns>List of Stock objects</returns>
    static async Task<Dictionary<string, Stock>> GetStocksAsDict()
    {
        var stocks = await MoMoneydb.db.Table<Stock>().ToListAsync();
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
