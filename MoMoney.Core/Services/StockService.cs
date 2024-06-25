using MoMoney.Core.Data;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.Services;

/// <inheritdoc />
public class StockService : BaseService<StockService, UpdateStocksMessage, string>, IStockService
{
    public Dictionary<int, Stock> Stocks { get; private set; } = new();

    public StockService(MoMoneydb _momoney, ILoggerService<StockService> _logger) : base(_momoney, _logger) { }

    protected override async Task Init()
    {
        await base.Init();
        if (Stocks.Count == 0)
            Stocks = await GetStocksAsDict();
    }

    public async Task AddStock(string symbol, string market, decimal quantity, decimal cost)
    {
        await DbOperation(async () =>
        {
            ValidateStock(symbol, market, quantity, cost, 0);

            var count = await momoney.db.Table<Stock>().CountAsync(s => s.Symbol == symbol && s.Market == market);
            if (count > 0)
                throw new DuplicateStockException($"Stock symbol'{symbol}:{market}' already exists");

            var stock = new Stock(symbol, market, quantity, cost, cost);

            await momoney.db.InsertAsync(stock);
            Stocks.Add(stock.StockID, stock);

            return $"Added Stock '{stock.Symbol}' to db.";
        });
    }

    public async Task AddStocks(List<Stock> stocks)
    {
        await DbOperation(async () =>
        {
            var dbStocks = await momoney.db.Table<Stock>().ToListAsync();

            // checks if names of any new stocks matches any names from db and throw exception if true
            bool containsDuplicates = stocks.Any(s => dbStocks.Select(dbs => dbs.FullName).Contains(s.FullName));
            if (containsDuplicates)
                throw new DuplicateStockException("Imported stocks contained duplicates. Please try again");

            // adds stocks to db and dictionary
            await momoney.db.InsertAllAsync(stocks);
            foreach (var stk in stocks)
                Stocks.Add(stk.StockID, stk);

            return $"Added {stocks.Count} Stocks to db.";
        });
    }

    public async Task UpdateStock(Stock updatedStock)
    {
        await DbOperation(async () =>
        {
            ValidateStock(updatedStock.Symbol, updatedStock.Market, updatedStock.Quantity, updatedStock.Cost, updatedStock.MarketPrice);

            await momoney.db.UpdateAsync(updatedStock);
            Stocks[updatedStock.StockID] = updatedStock;

            return $"Updated Stock '{updatedStock.Symbol}' in db.";
        });
    }

    public async Task RemoveStock(int ID)
    {
        await DbOperation(async () =>
        {
            await momoney.db.DeleteAsync<Stock>(ID);
            Stocks.Remove(ID);

            return $"Removed Stock #{ID} from db.";
        });
    }

    public async Task RemoveStocks()
    {
        await DbOperation(async () =>
        {
            await momoney.db.DeleteAllAsync<Stock>();
            await momoney.db.DropTableAsync<Stock>();
            await momoney.db.CreateTableAsync<Stock>();
            Stocks.Clear();

            return $"Removed all Stocks from db.";
        });
    }

    public async Task<Stock> GetStock(int ID)
    {
        await Init();
        if (Stocks.TryGetValue(ID, out var stock))
            return new Stock(stock);

        var stk = await momoney.db.Table<Stock>().FirstOrDefaultAsync(s => s.StockID == ID);
        if (stk is null)
            throw new StockNotFoundException($"Could not find Stock #{ID}.");
        else
            return stk;
    }

    public async Task<IEnumerable<Stock>> GetStocks()
    {
        await Init();
        if (Stocks.Count != 0)
            return Stocks.Values.ToList();

        return await momoney.db.Table<Stock>().ToListAsync();
    }

    public async Task<int> GetStockCount()
    {
        await momoney.Init();
        return await momoney.db.Table<Stock>().CountAsync();
    }

    /// <summary>
    /// Gets all Stocks from Stocks table as a list.
    /// </summary>
    /// <returns>List of Stock objects</returns>
    async Task<Dictionary<int, Stock>> GetStocksAsDict()
    {
        var stocks = await momoney.db.Table<Stock>().ToListAsync();
        return stocks.ToDictionary(s => s.StockID, s => s);
    }

    /// <summary>
    /// Validates input fields for Stocks.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="quantity"></param>
    /// <param name="cost"></param>
    /// <param name="marketPrice"></param>
    /// <exception cref="InvalidStockException"></exception>
    static void ValidateStock(string symbol, string market, decimal quantity, decimal cost, decimal marketPrice)
    {
        if (string.IsNullOrEmpty(symbol))
            throw new InvalidStockException("Invalid symbol");
        if (string.IsNullOrEmpty(market))
            throw new InvalidStockException("Invalid market");
        if (quantity < 0)
            throw new InvalidStockException("Quantity must be > 0");
        if (cost <= 0)
            throw new InvalidStockException("Cost must be > 0");
        if (marketPrice < 0)
            throw new InvalidStockException("Market Price must be >= 0");
    }
}