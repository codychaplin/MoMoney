using MoMoney.Core.Models;
using MoMoney.Core.Exceptions;

namespace MoMoney.Core.Services.Interfaces;

public interface IStockService
{
    /// <summary>
    /// Cached dictionary of Stocks
    /// </summary>
    Dictionary<int, Stock> Stocks { get; }

    /// <summary>
    /// Creates new Stock object and inserts into Stocks table.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="market"></param>
    /// <param name="quantity"></param>
    /// <param name="cost"></param>
    /// <exception cref="DuplicateStockException"></exception>
    Task AddStock(string symbol, string market, decimal quantity, decimal cost);

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
    /// Removes Stock from Stocks table.
    /// </summary>
    /// <param name="id"></param>
    Task RemoveStock(int id);

    /// <summary>
    /// Removes ALL Stocks from Stocks table.
    /// </summary>
    Task RemoveStocks();

    /// <summary>
    /// Gets a stock from the Stocks table using a stock ID.
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Stock object</returns>
    Task<Stock> GetStock(int id);

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
