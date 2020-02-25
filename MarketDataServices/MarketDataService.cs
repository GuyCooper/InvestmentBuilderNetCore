using System.Collections.Generic;
using NLog;

namespace MarketDataServices
{
    /// <summary>
    /// Interface to a MarketData service. Provides a service interface to a market data source.
    /// </summary>
    public interface IMarketDataService
    {
        bool TryGetClosingPrice(string symbol, string exchange, string source, string name, string currency, string reportingCurrency, double? dOverride, out double dClosing);
        IList<string> GetSources();
    }

    /// <summary>
    /// Class provides market data services. provides closing prices and currency conversion for stock symbols.
    /// </summary>
    public class MarketDataService : IMarketDataService
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public MarketDataService(IMarketDataSource marketSource)
        {
            _marketSource = marketSource;
        }

        /// <summary>
        /// Returns a list of all market data sources.
        /// </summary>
        /// <returns></returns>
        public IList<string> GetSources()
        {
            return _marketSource.GetSources();
        }

        /// <summary>
        /// Get current closing price for symbol. convert to reportng currency if required.
        /// </summary>
        public bool TryGetClosingPrice(string symbol, string exchange, string source, string name, string currency, string reportingCurrency, double? dOverride, out double dClosing)
        {
            logger.Log(LogLevel.Info, string.Format("getting closing price for : {0}", name));

            dClosing = 0d;
            string localCurrency = currency;
            if(dOverride.HasValue)
            {
                dClosing = dOverride.Value;
            }
            else
            {
                MarketDataPrice marketData;
                if(_marketSource.TryGetMarketData(symbol, exchange, source, out marketData) == false)
                {
                    return false;
                }
                dClosing = marketData.Price;

                if(dClosing == 0d)
                {
                    return false;
                }
                if (marketData.Currency != null)
                {
                    localCurrency = marketData.Currency;
                }
            }
            if (localCurrency != reportingCurrency)
            {
                //need todo an fx conversion to get correct price
                double dFx;
                if (_marketSource.TryGetFxRate(localCurrency, reportingCurrency, exchange, source, out dFx))
                {
                    dClosing = dClosing * dFx;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private IMarketDataSource _marketSource;

        #endregion
    }
}
