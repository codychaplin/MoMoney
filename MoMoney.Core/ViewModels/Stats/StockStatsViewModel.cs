﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using HtmlAgilityPack;
using MoMoney.Core.Models;
using MoMoney.Core.Helpers;
using MoMoney.Core.Exceptions;
using MoMoney.Core.Services.Interfaces;

namespace MoMoney.Core.ViewModels.Stats;

public partial class StockStatsViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    readonly IStockService stockService;
    readonly ILoggerService<StockStatsViewModel> logger;

    [ObservableProperty] ObservableCollection<DetailedStock> stocks = [];
    [ObservableProperty] ObservableRangeCollection<StockData> stockData = [];

    [ObservableProperty] decimal total = 0;
    [ObservableProperty] decimal totalPercent = 0;
    [ObservableProperty] decimal marketValue = 0;

    [ObservableProperty] string showValue = "$0";

    decimal totalBook = 0;
    decimal totalMarket = 0;

    public CancellationTokenSource cts = new();

    public StockStatsViewModel(IStockService _stockService, ILoggerService<StockStatsViewModel> _logger)
    {
        stockService = _stockService;
        logger = _logger;
        logger.LogFirebaseEvent(FirebaseParameters.EVENT_VIEW_STOCKS, FirebaseParameters.GetFirebaseParameters());
        ShowValue = Utilities.ShowValue ? "$0" : "$?";
    }

    /// <summary>
    /// Initializes data for StocksPage
    /// </summary>
    public async Task Init()
    {
        // populate collection with cached values first
        var stocks = await stockService.GetStocks();
        if (!stocks.Any())
            return;

        foreach (var stock in stocks)
        {
            DetailedStock dStock = new(stock);
            Stocks.Add(dStock);
            totalBook += dStock.BookValue;
            totalMarket += dStock.MarketValue;
        }
        MarketValue = totalMarket;
        Total = totalMarket - totalBook;
        TotalPercent = (totalMarket / totalBook) - 1;

        UpdateChart();
        await GetUpdatedStockPrices(cts.Token); // get updated prices via webscraping
    }

    void UpdateChart()
    {
        var stockData = Stocks.Select(ds =>
        {
            return new StockData
            {
                Symbol = ds.Symbol,
                Price = ds.MarketValue
            };
        })
        .OrderByDescending(sd => sd.Price);
        StockData.ReplaceRange(stockData);
    }

    /// <summary>
    /// Uses an API to fetch a list of stocks' prices.
    /// </summary>
    async Task GetUpdatedStockPrices(CancellationToken token)
    {
        try
        {
            HttpClient client = new();
            for (int i = 0; i < Stocks.Count; i++)
            {
                // get url from stock symbol, get response, and then contents
                string url = $"https://www.google.com/finance/quote/{Stocks[i].Symbol}";
                HttpResponseMessage response = await client.GetAsync(url, token);
                response.EnsureSuccessStatusCode();
                string htmlContent = await response.Content.ReadAsStringAsync(token);

                // parse content to html, find element using xpath
                HtmlDocument document = new();
                document.LoadHtml(htmlContent);
                HtmlNode priceElement = document.DocumentNode.SelectSingleNode("//div[@class='YMlKec fxKbKc']") 
                   ?? throw new StockNotFoundException($"Could not find '{Stocks[i].Symbol}'. Please ensure the name and market are spelled correctly");

                // validate price
                string price = priceElement.InnerHtml[1..];
                if (string.IsNullOrEmpty(price))
                    throw new InvalidStockException("Updated price not found.");
                if (decimal.TryParse(price, out decimal newPrice))
                {
                    decimal difference = newPrice - Stocks[i].MarketPrice;
                    // if price has changed, update values
                    if (difference == 0)
                        continue;

                    var stock = new DetailedStock(Stocks[i]) { MarketPrice = newPrice };
                    var marketValue = Stocks[i].MarketValue;
                    Stocks[i] = stock;

                    // update in db (need to use oldStock due to casting issues)
                    var oldStock = stockService.Stocks[Stocks[i].Symbol];
                    oldStock.MarketPrice = Stocks[i].MarketPrice;
                    await stockService.UpdateStock(oldStock);

                    // update totals
                    totalMarket += stock.MarketValue - marketValue;
                    MarketValue = totalMarket;
                    Total = totalMarket - totalBook;
                    TotalPercent = (totalMarket / totalBook) - 1;

                }
                else
                    throw new InvalidStockException($"{price} is not a valid number");
            }
        }
        catch (HttpRequestException ex)
        {
            await logger.LogError(nameof(GetUpdatedStockPrices), ex);
            await Shell.Current.DisplayAlert("HTTP Error", ex.Message, "OK");
        }
        catch (InvalidStockException ex)
        {
            await logger.LogError(nameof(GetUpdatedStockPrices), ex);
            await Shell.Current.DisplayAlert("Parse Error", ex.Message, "OK");
        }

        catch (StockNotFoundException ex)
        {
            await logger.LogError(nameof(GetUpdatedStockPrices), ex);
            await Shell.Current.DisplayAlert("Stock not found", ex.Message, "OK");
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
            await logger.LogError(nameof(GetUpdatedStockPrices), ex);
            await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            cts?.Cancel(); // cancel task regardless
        }
    }
}