using System.Text.Json;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;

namespace MoMoney.ViewModels.Stats;

public partial class StocksViewModel : ObservableObject
{
    [ObservableProperty]
    public ObservableCollection<DetailedStock> stocks = new();

    [ObservableProperty]
    public decimal total = 0;

    [ObservableProperty]
    public decimal totalChange = 0;

    [ObservableProperty]
    public decimal marketValue = 0;

    /// <summary>
    /// Initializes data for StocksPage
    /// </summary>
    /// <param name="s"></param>
    /// <param name="e"></param>
    public async void Init(object s, EventArgs e)
    {
        // populate collection with cached values first
        var stocks = StockService.Stocks.Values;
        if (!stocks.Any())
            return;

        decimal totalBook = 0;
        decimal totalMarket = 0;
        foreach (var stock in stocks)
        {
            DetailedStock dStock = new(stock);
            Stocks.Add(dStock);
            totalBook += dStock.BookValue;
            totalMarket += dStock.MarketValue;
        }
        MarketValue = totalMarket;
        Total = totalMarket - totalBook;
        TotalChange = (totalMarket / totalBook) - 1;

        // get updated prices from API
        await GetUpdatedStockPrices();
    }

    /// <summary>
    /// Uses an API to fetch a list of stocks' prices.
    /// </summary>
    public async Task GetUpdatedStockPrices()
    {
        int count = 0;
        foreach (var stock in Stocks)
        {
            using (var client = new HttpClient())
            {
                string APIKey = (++count <= 5) ? Secret.AlphavantageAPIKey1 : Secret.AlphavantageAPIKey2;
                string url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={stock.Symbol}&apikey={APIKey}";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // parse response and get 'price' property
                    string json = await response.Content.ReadAsStringAsync();
                    JsonDocument doc = JsonDocument.Parse(json);
                    JsonElement root = doc.RootElement;
                    string stringPrice = "-1";
                    decimal price = -1;
                    try
                    {
                        JsonElement quote = root.GetProperty("Global Quote");
                        stringPrice = quote.GetProperty("05. price").GetString();
                    }
                    catch (KeyNotFoundException)
                    {
                        // triggers if reached API call limit
                    }
                    finally
                    {
                        // if price is valid, update stock info
                        if (decimal.TryParse(stringPrice, out price))
                        {
                            // use old market price if unchanged
                            if (price == -1)
                                price = stock.MarketPrice;

                            // update price in collection
                            Stocks.Where(s => s.Symbol == stock.Symbol).First().MarketPrice = price;

                            // if market price has changed, update in db
                            var oldStock = StockService.Stocks[stock.Symbol];
                            if (stock.MarketPrice != oldStock.MarketPrice)
                            {
                                // need to use oldStock due to casting issues
                                oldStock.MarketPrice = stock.MarketPrice;
                                await StockService.UpdateStock(oldStock);
                            }
                        }
                    }
                }
            }
        }
    }
}

public class DetailedStock : Stock
{
    public decimal MarketValue => Symbol.EndsWith(".TO") ? MarketPrice * Quantity : MarketPrice * Quantity * 1.3m;
    public decimal Change => MarketValue - BookValue;
    public decimal ChangePercent => (MarketValue / BookValue) - 1;

    public DetailedStock(Stock stock)
    {
        Symbol = stock.Symbol;
        Quantity = stock.Quantity;
        Cost = stock.Cost;
        MarketPrice = stock.MarketPrice;
        BookValue= stock.BookValue;
    }
}
