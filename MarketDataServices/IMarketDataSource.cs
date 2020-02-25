using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvestmentBuilderCore;
using System.Diagnostics.Contracts;

namespace MarketDataServices
{
    /// <summary>
    /// Class defines a price item.
    /// </summary>
    public class MarketDataPrice
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public MarketDataPrice(string name, string symbol, double price,
                               string currency = null, string exchange = null)
        {
            Name = name;
            Symbol = symbol;
            Price = price;
            Currency = currency;
            Exchange = exchange;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Name of price item.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Symbol for source of price.
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Actual price.
        /// </summary>
        public double Price { get; private set; }

        /// <summary>
        /// Currency of price
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// Exhange of price source (optional).
        /// </summary>
        public string Exchange { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// To String
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0},{1},{2},{3}", Symbol, Price, Currency, Exchange);
        }

        /// <summary>
        /// format price depending on currency name.
        /// </summary>
        public void DecimalisePrice()
        {
            //deciamlise price if required
            if (Currency[Currency.Length - 1] == 'p')
            {
                Price = Price / 100d;
                Currency = Currency.ToUpper();
            }
        }

        #endregion

        #region Protected Methods

        [ContractInvariantMethod]
        protected void ObjectInvarianceCheck()
        {
            Contract.Invariant(string.IsNullOrEmpty(Name) == false);
            Contract.Invariant(Price > 0);
            Contract.Invariant(string.IsNullOrEmpty(Symbol) == false);
        }

        #endregion
    }

    /// <summary>
    /// Interface for a market data source.
    /// </summary>
    public interface IMarketDataSource
    {
        /// <summary>
        /// returns list of source names for this market data source
        /// </summary>
        IList<string> GetSources();

        /// <summary>
        /// Try to get market price for symbol.
        /// </summary>
        bool TryGetMarketData(string symbol, string exchange, string source, out MarketDataPrice marketData);

        /// <summary>
        /// Try to fx rate for ccy pair, return true for success, false for fail. 
        /// </summary>
        bool TryGetFxRate(string baseCurrency, string contraCurrency, string exchange, string source, out double dFxRate);

        /// <summary>
        /// Try to retrieve historical data for instrument, return data if success, null for fail.
        /// </summary>
        IEnumerable<HistoricalData> GetHistoricalData(string instrument, string exchange, string source, DateTime dtFrom);

        /// <summary>
        /// name ofsource
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// for multiple data sources this value orders them in priority order
        /// 1 = highest
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Initialise datasource with configuration settings.
        /// </summary>
        void Initialise(IConfigurationSettings settings);
        
        /// <summary>
        /// Asynchronously request a price from the data source.
        /// </summary>
        Task<MarketDataPrice> RequestPrice(string symbol, string exchange, string source);

    }

}
