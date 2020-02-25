using System;
using System.Collections.Generic;

namespace InvestmentBuilderCore
{
    /// <summary>
    /// Class defines  a single instrument (i.e. company) including the details of its source
    /// information
    /// </summary>
    public class InvestmentInformation
    {
        public InvestmentInformation(string symbol, 
                                     string exchange,
                                     string currency,
                                     double scalingFactor)
        {
            Symbol = symbol;
            Exchange = exchange;
            Currency = currency;
            ScalingFactor = scalingFactor;
        }

        public string Symbol { get; private set; }
        public string Exchange { get; private set; }
        public string Currency { get; private set; }
        public double ScalingFactor { get; private set; }

    }

    /// <summary>
    /// CashAccountData class. Defines the cash flow for an account.
    /// </summary>
    public class CashAccountData
    {
        public CashAccountData()
        {
            Dividends = new Dictionary<string, double>();
        }
        public Dictionary<string, double> Dividends { get; private set; }
        public double BankBalance { get; set; }
    }

    /// <summary>
    /// Class defines the valuation of a single instrument (company) on a specific date
    /// </summary>
    public class CompanyData
    {
        public string Name { get; set; }
        public DateTime ValuationDate { get; set; }
        public DateTime LastBrought {get;set;}
        public double Quantity {get;set;}
        public double AveragePricePaid { get; set; }
        public double TotalCost { get; set; }
        public double SharePrice { get; set; }
        public double NetSellingValue { get; set; }
        public double ProfitLoss { get; set; }
        public double MonthChange { get; set; }
        public double MonthChangeRatio { get; set; }
        public double Dividend { get; set; }
        public string ManualPrice { get; set; }
        public double TotalReturn { get; set; }
    }

    //this class represents a data point in a performance graph. the
    //point may represent an historical data point in which case the
    //date property will be populated or it may just contain a key 
    //point (i.e. the average yield for a company) in which case the
    //key property will be populated and the date property will be null
    public class HistoricalData
    {
        public HistoricalData(string key, double price)
        {
            Key = key;
            Price = price;
        }

        public HistoricalData(DateTime? date, double price)
        {
            Date = date;
            Price = price;
        }

        /// <summary>
        /// Rebase the price from the base price.
        /// </summary>
        public void RebasePrice(double basePrice)
        {
            Price = 1 + ((Price - basePrice) / basePrice);
        }

        public DateTime? Date { get; private set; }
        public string Key { get; private set; }
        public double Price { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}={1}", Date.Value.ToString("dd/MM/yyyy"), Price);
        }

    }

    /// <summary>
    /// Manual Prices class. lookup of investment name to manual price. curency is 
    /// always considered to be the same as the reporting currency for the account
    /// </summary>
    public class ManualPrices : Dictionary<string, double>
    {
        public ManualPrices() : base(StringComparer.InvariantCultureIgnoreCase) { }
    }

    /// <summary>
    /// TradeType Enum. 
    /// </summary>
    public enum TradeType
    {
        BUY,
        SELL,
        MODIFY
    } 

    /// <summary>
    /// Redemption Status.
    /// </summary>
    public enum RedemptionStatus
    {
        Pending,
        Complete,
        Failed
    }

    /// <summary>
    /// Redemption Class. Contains details about a requested redemetion. Redemptions have to be
    /// recoreded as requested because they cannot be issued until the account has been valued.
    /// </summary>
    public class Redemption
    {
        public Redemption(string user, double amount, DateTime date, RedemptionStatus status)
        {
            User = user;
            Amount = amount;
            TransactionDate = date;
            Status = status;
        }

        public string User { get; private set; }
        public double Amount { get; private set; }
        public DateTime TransactionDate { get; private set; }
        public double RedeemedUnits { get; set; }
        public RedemptionStatus Status { get; set; }

        public void UpdateAmount(double amount)
        {
            Amount = amount;
        }

    }

    /// <summary>
    /// Transaction class. Defines a single investment transaction. 
    /// </summary>
    public class Transaction
    {
        public string InvestmentName { get; set; }
        public TradeType TransactionType { get; set; }
        public double Quantity { get; set; }
        public double Amount { get; set; }
    }
}
