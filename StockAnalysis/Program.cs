using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace StockAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {

            const string API_KEY = "EQDBTWY2QMI9J3SR";

            // NYSE - New York Stock Exchange
            const string CITIBANK_SYMBOL = "NYSE:C";
            const string ALFABANK_SYMBOL = "NYSE:ALBKY";
            const string COMMERZBANK_SYMBOL = "NYSE:CBK";

            string jsonStr;

            using (WebClient wc = new WebClient()) jsonStr = wc.DownloadString($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={CITIBANK_SYMBOL}&outputsize=full&apikey={API_KEY}");
            List<StockDataForDay> citibankStockSeries = ConvertJsonToStockTimeSeriesDaily(jsonStr);

            using (WebClient wc = new WebClient()) jsonStr = wc.DownloadString($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={ALFABANK_SYMBOL}&outputsize=full&apikey={API_KEY}");
            List<StockDataForDay> alfabankStockSeries = ConvertJsonToStockTimeSeriesDaily(jsonStr);

            using (WebClient wc = new WebClient()) jsonStr = wc.DownloadString($"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={COMMERZBANK_SYMBOL}&outputsize=full&apikey={API_KEY}");
            List<StockDataForDay> commerzbankStockSeries = ConvertJsonToStockTimeSeriesDaily(jsonStr);

            List<(string startDate, string endDate, string status)> periodsUpAndDown = GetPeriodsUpAndDownStockPrice(citibankStockSeries, alfabankStockSeries, commerzbankStockSeries);

            foreach (var item in periodsUpAndDown)
            {
                Console.WriteLine(item);
            }

            Console.ReadKey();
        }

        static List<StockDataForDay> ConvertJsonToStockTimeSeriesDaily(string json)
        {

            List<StockDataForDay> stockSeries = new List<StockDataForDay>();

            Regex regex = new Regex(@"20(20|21)-\d{2}-\d{2}"":[^}]*}");
            MatchCollection matches = regex.Matches(json);

            foreach (Match item in matches)
            {
                string date = new Regex(@"20(20|21)-\d{2}-\d{2}").Match(item.Value).Value;
                decimal stockOpenPrice = decimal.Parse(new Regex(@"\d+\.?\d*").Match(new Regex(@"open"": ""\d+\.?\d*").Match(item.Value).Value).Value.Replace('.', ','));
                decimal stockClosePrice = decimal.Parse(new Regex(@"\d+\.?\d*").Match(new Regex(@"close"": ""\d+\.?\d*""").Match(item.Value).Value).Value.Replace('.', ','));
                decimal profit = (stockClosePrice - stockOpenPrice) / stockOpenPrice;
                stockSeries.Add(new StockDataForDay { Date = date, StockOpenPrice = stockOpenPrice, StockClosePrice = stockClosePrice, Profitability = profit });
            }

            return stockSeries;

        }

        static List<(string startDate, string endDate, string status)> GetPeriodsUpAndDownStockPrice(List<StockDataForDay> stockSeries1, List<StockDataForDay> stockSeries2, List<StockDataForDay> stockSeries3)
        {

            List<(string startDate, string endDate, string status)> periods = new List<(string startDate, string endDate, string status)>();

            if (stockSeries1.Count != stockSeries2.Count || stockSeries2.Count != stockSeries3.Count) return periods;

            for (int i = stockSeries1.Count - 1; i >= 0; i--)
            {
                if (stockSeries1[i].Date == stockSeries2[i].Date && stockSeries2[i].Date == stockSeries3[i].Date)
                {
                    if (stockSeries1[i].Profitability > 0 && stockSeries2[i].Profitability > 0 && stockSeries3[i].Profitability > 0)
                    {
                        if (periods.Count > 0)
                        {
                            if (periods[periods.Count - 1].status == "Up") 
                                periods[periods.Count - 1] = (periods[periods.Count - 1].startDate, stockSeries1[i].Date, "Up");
                            else
                            {
                                periods.Add((stockSeries1[i].Date, stockSeries1[i].Date, "Up"));
                            }
                        }
                        else
                        {
                            periods.Add((stockSeries1[i].Date, stockSeries1[i].Date, "Up"));
                        }
                    }
                    else if (stockSeries1[i].Profitability < 0 && stockSeries2[i].Profitability < 0 && stockSeries3[i].Profitability < 0)
                    {
                        if (periods.Count > 0)
                        {
                            if (periods[periods.Count - 1].status == "Down")
                                periods[periods.Count - 1] = (periods[periods.Count - 1].startDate, stockSeries1[i].Date, "Down");
                            else
                            {
                                periods.Add((stockSeries1[i].Date, stockSeries1[i].Date, "Down"));
                            }
                        }
                        else
                        {
                            periods.Add((stockSeries1[i].Date, stockSeries1[i].Date, "Down"));
                        }
                    }
                }
            }

            return periods;
        }
    }

    class StockDataForDay
    {
        public string Date { get; set; }
        public decimal StockOpenPrice { get; set; }
        public decimal StockClosePrice { get; set; }
        public decimal Profitability { get; set; }
    }
}
