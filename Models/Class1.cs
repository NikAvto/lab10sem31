using System;

public class StockPrice
{
    public int Id { get; set; }
    public string Ticker { get; set; }
    public double Price { get; set; }
    public DateTime Date { get; set; }
}

public class TodaysCondition
{
    public int Id { get; set; }
    public string Ticker { get; set; }
    public string Condition { get; set; }
}