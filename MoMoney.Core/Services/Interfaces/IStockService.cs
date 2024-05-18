using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Services.Interfaces;

public interface IStockService
{
    /// <summary>
    /// Cached dictionary of Stocks
    /// </summary>
    Dictionary<string, Stock> Stocks { get; }

    /// <summary>
    /// Creates new Stock object and inserts into Stocks table.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="quantity"></param>
    /// <param name="cost"></param>
    /// <param name="marketprice"></param>
    /// <param name="bookvalue"></param>
    /// <exception cref="DuplicateStockException"></exception>
    Task AddStock(string symbol, int quantity, decimal cost, decimal marketprice, decimal bookvalue);

    /// <summary>
    /// Inserts multiple Stock objects into Stocks table.
    /// </summary>
    /// <param name="stocks"></param>
    /// <exception cref="DuplicateStockException"></exception>
    Task AddStocks(List<Stock> stocks);

    /// <summary>
    /// Given an Stock object, updates the corresponding stock in the Stocks table.
    /// </summary>
    /// <param name="updatedStock"></param>
    Task UpdateStock(Stock updatedStock);

    /// <summary>
    /// Given a new and old Stock object, removes old and inserts new into the Stocks table.
    /// </summary>
    /// <param name="updatedStock"></param>
    Task UpdateStock(Stock updatedStock, Stock oldStock);

    /// <summary>
    /// Removes Stock from Stocks table.
    /// </summary>
    /// <param name="symbol"></param>
    Task RemoveStock(string symbol);

    /// <summary>
    /// Removes ALL Stocks from Stocks table.
    /// </summary>
    Task RemoveStocks();

    /// <summary>
    /// Gets a stock from the Stocks table using a symbol.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns>Stock object</returns>
    Task<Stock> GetStock(string symbol);

    /// <summary>
    /// Gets all Stocks from Stocks table as a list.
    /// </summary>
    /// <returns>List of Stock objects</returns>
    Task<IEnumerable<Stock>> GetStocks();

    /// <summary>
    /// Gets total number of Stocks in db.
    /// </summary>
    /// <returns>Integer representing number of Stocks</returns>
    Task<int> GetStockCount();
}
