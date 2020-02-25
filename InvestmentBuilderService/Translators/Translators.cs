using System.Collections.Generic;
using InvestmentBuilderService.Dtos;
using InvestmentBuilderCore;

namespace InvestmentBuilderService.Translators
{
    /// <summary>
    /// Static Helper class for translating Dto objects into internal business objects 
    /// and vice versa
    /// </summary>
    internal static class Translators
    {
        /// <summary>
        /// Method converts an internal asset report into an investsummarymodel dto object
        /// </summary>
        public static InvestmentSummaryModel ToInvestmentSummaryModel(this AssetReport report)
        {
            //return _CloneObject<InvestmentSummaryModel>(report.GetType(), report, () => new InvestmentSummaryModel());
            return new InvestmentSummaryModel
            {
                AccountName = report.AccountName,
                BankBalance = report.BankBalance.ToString("#0.00"),
                MonthlyPnL = report.MonthlyPnL.ToString("#0.00"),
                NetAssets = report.NetAssets.ToString("#0.00"),
                ReportingCurrency = report.ReportingCurrency,
                TotalAssets = report.TotalAssets.ToString("#0.00"),
                TotalAssetValue = report.TotalAssetValue.ToString("#0.00"),
                ValuationDate = report.ValuationDate,
                ValuePerUnit = report.ValuePerUnit.ToString("#0.00")
            };
        }

        /// <summary>
        /// Translation method converts a Stock object into an internal Trades item 
        /// A Trades item is just a container of Stock objects that contains 3 lists
        /// of stock objects - Adds, Sells and Changes.
        /// </summary>
        /// <returns></returns>
        public static Trades ToInternalTrade(this Stock tradeItem, TransactionType action)
        {
            var trades = new Trades();
            var arrStock = new List<Stock> { tradeItem }.ToArray();
            switch (action)
            {
                case TransactionType.Buy:
                    trades.Buys = arrStock;
                    break;
                case TransactionType.Sell:
                    trades.Sells = arrStock;
                    break;
                case TransactionType.Change:
                    trades.Changed = arrStock;
                    break;
            }
            return trades;
        }

        /// <summary>
        /// Method converts an external TradeItemDto object into an internal Trades item
        /// </summary>
        /// <param name="tradeItem"></param>
        /// <returns></returns>
        public static Trades ToInternalTrade(this TradeItemDto tradeItem)
        {
            if(tradeItem.Action == TransactionType.None)
            {
                throw new System.Exception($"Invalid action type on trade item");
            }
            var stock = new Stock
            {
                Currency = tradeItem.Currency,
                Name = tradeItem.ItemName,
                Quantity = tradeItem.Quantity,
                Symbol = tradeItem.Symbol,
                TotalCost = tradeItem.TotalCost,
                TransactionDate = tradeItem.TransactionDate
            };
            return stock.ToInternalTrade(tradeItem.Action);
        }
    }
}
