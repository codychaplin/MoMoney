using System.Collections.ObjectModel;
using HtmlAgilityPack;
using CommunityToolkit.Mvvm.ComponentModel;
using MoMoney.Models;
using MoMoney.Services;
using MoMoney.Exceptions;

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

    public CancellationTokenSource cts = new();

    /// <summary>
    /// Initializes data for StocksPage
    /// </summary>
    /// <param name="s"></param>
    /// <param name="e"></param>
    public async void Init(object s, EventArgs e)
    {
        // populate collection with cached values first
        var stocks = StockService.Stocks.Values.OrderByDescending(s => s.MarketPrice);
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
        
        await Task.Delay(500); // allows smooth transition to page
        await GetUpdatedStockPrices(cts.Token); // get updated prices via webscraping
    }

    /// <summary>
    /// Uses an API to fetch a list of stocks' prices.
    /// </summary>
    public async Task GetUpdatedStockPrices(CancellationToken token)
    {
        try
        {
            HttpClient client = new();
            foreach (var stock in Stocks)
            {
                // get url from stock symbol, get response, and then contents
                string url = $"https://www.google.com/finance/quote/{stock.Symbol}";
                HttpResponseMessage response = await client.GetAsync(url, token);
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync(token);

                // parse content to html, find element using xpath
                HtmlDocument document = new();
                document.LoadHtml(htmlContent);
                HtmlNode priceElement = document.DocumentNode.SelectSingleNode("//div[@class='YMlKec fxKbKc']");
                string price = priceElement.InnerHtml[1..];

                // validate price
                if (string.IsNullOrEmpty(price))
                    throw new InvalidStockException("Updated price not found.");
                if (decimal.TryParse(price, out decimal marketPrice))
                {
                    stock.MarketPrice = marketPrice;

                    // if market price has changed, update in db
                    var oldStock = StockService.Stocks[stock.Symbol];
                    if (stock.MarketPrice != oldStock.MarketPrice)
                    {
                        // need to use oldStock due to casting issues
                        oldStock.MarketPrice = stock.MarketPrice;
                        await StockService.UpdateStock(oldStock);
                    }
                }
                else
                    throw new InvalidStockException($"{price} is not a valid number");
            }
        }
        catch (HttpRequestException ex)
        {
            await Shell.Current.DisplayAlert("HTTP Error", ex.Message, "OK");
        }
        catch (InvalidStockException ex)
        {
            await Shell.Current.DisplayAlert("Parse Error", ex.Message, "OK");
        }
        catch (InvalidOperationException)
        {
            // triggers if page is closed while HttpResponseMessage is processing
        }
        catch (TaskCanceledException)
        {
            // triggers when page is closed
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            cts?.Cancel(); // cancel task regardless
        }
    }
}

public class DetailedStock : Stock
{
    public decimal MarketValue => Symbol.EndsWith(":TSE") ? MarketPrice * Quantity : MarketPrice * Quantity * 1.3m;
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
