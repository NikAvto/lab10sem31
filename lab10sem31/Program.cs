using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

public class StockContext : DbContext
{
    public DbSet<StockPrice> StockPrices { get; set; }
    public DbSet<TodaysCondition> TodaysConditions { get; set; }

    public StockContext() => Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=stockdata.db");
}

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        string[] tickers = File.ReadAllLines("ticker.txt");
        string fromDate = "2024-01-06";
        string toDate = DateTime.Now.ToString("yyyy-MM-dd");

        using (var context = new StockContext())
        {
            foreach (var ticker in tickers)
            {
                var averagePrice = await GetDataAsync(ticker.Trim(), fromDate, toDate);
                if (averagePrice.HasValue)
                {
                    Console.WriteLine($"Средняя цена для {ticker}: {averagePrice.Value}");
                    SavePriceToDatabase(context, ticker.Trim(), averagePrice.Value);
                    Console.WriteLine("Добавлено в базу.");
                }
            }

            AnalyzeStockConditions(context);
        }
    }

    private static async Task<double?> GetDataAsync(string ticker, string fromDate, string toDate)
    {
        string url = $"https://api.marketdata.app/v1/stocks/candles/D/{ticker}/?from={fromDate}&to={toDate}&token=T1JFM3lweVluSEhzTWhMQk5EM2NOal9PdDUzbUFzX090cTlTSXRuT1lPdz0";
        try
        {
            var response = await client.GetStringAsync(url);
            var json = JObject.Parse(response);
            var highs = json["h"].ToObject<List<double>>();
            var lows = json["l"].ToObject<List<double>>();
            double averageHigh = CalculateAverage(highs);
            double averageLow = CalculateAverage(lows);
            return (averageHigh + averageLow) / 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении данных для {ticker}: {ex.Message}");
            return null;
        }
    }

    private static double CalculateAverage(List<double> values)
    {
        if (values.Count == 0) return 0;
        double sum = 0;
        foreach (var value in values)
        {
            sum += value;
        }
        return sum / values.Count;
    }

    private static void SavePriceToDatabase(StockContext context, string ticker, double price)
    {
        var stockPrice = new StockPrice { Ticker = ticker, Price = price, Date = DateTime.Now };
        context.StockPrices.Add(stockPrice);
        context.SaveChanges();
    }

    private static void AnalyzeStockConditions(StockContext context)
    {
        Console.Write("Введите тикер для анализа: ");
        string userTicker = Console.ReadLine().Trim();

        var todayPrices = context.StockPrices.Where(sp => sp.Date.Date == DateTime.Now.Date && sp.Ticker == userTicker).ToList();

        if (todayPrices.Count == 0)
        {
            Console.WriteLine($"Нет данных для тикера {userTicker} за сегодня.");
            return;
        }

        foreach (var stock in todayPrices)
        {
            var yesterdayPrice = context.StockPrices.Where(sp => sp.Ticker == stock.Ticker).OrderByDescending(sp => sp.Date).ToList()[1];
            if (yesterdayPrice != null)
            {
                string condition = stock.Price > yesterdayPrice.Price ? "выросла" : "упала";
                SaveConditionToDatabase(context, stock.Ticker, condition);
                Console.WriteLine($"Акция {stock.Ticker} {condition}.");
            }
            else
            {
                Console.WriteLine($"Нет данных за вчерашний день для тикера {stock.Ticker}.");
            }
        }
    }

    private static void SaveConditionToDatabase(StockContext context, string ticker, string condition)
    {
        var todaysCondition = new TodaysCondition { Ticker = ticker, Condition = condition };
        context.TodaysConditions.Add(todaysCondition);
        context.SaveChanges();
    }
}