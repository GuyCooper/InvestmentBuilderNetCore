using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvestmentBuilderCore;
using NLog;

namespace MarketDataServices
{
    /// <summary>
    /// aggregates all market data sources and iterates through each one to get 
    /// source data until succeeds. Uses the MEF service locator to load all known
    /// marketdatasources in this assembley. Each datasource has a priority (1  = highest)
    /// this class acts as a broker forwarding market data requests to each of the registered
    /// market data sources in priority order until the request is satisifed.
    /// </summary>
    internal class AggregatedMarketDataSource : IMarketDataSource
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public AggregatedMarketDataSource(IConfigurationSettings settings)
        {
            _sources.Add(new YahooMarketDataSource());
            foreach(var source in  _sources)
            {
                source.Initialise(settings);
            }
        }

        #endregion

        #region IMarketDataSource

        /// <summary>
        /// Name of datasource.
        /// </summary>
        public string Name { get { return "Aggregated"; } }

        /// <summary>
        /// Priority of datasource
        /// </summary>
        public int Priority { get { return 0; } }

        /// <summary>
        /// Returns the list of sources for the aggregated data source.
        /// </summary>
        public IList<string> GetSources()
        {
            return _sources.SelectMany(x => x.GetSources()).ToList();
        }

        /// <summary>
        /// Try  to get market price for symbol.
        /// </summary>
        public bool TryGetMarketData(string symbol, string exchange, string source, out MarketDataPrice marketData)
        {
            foreach(var element in _GetOrderedDataSources(source))
            {
                if(element.TryGetMarketData(symbol, exchange, source, out marketData))
                {
                    return true;
                }
            }

            marketData = null;
            return false;
        }

        /// <summary>
        /// Try to fx rate for ccy pair, return true for success, false for fail. 
        /// </summary>
        public bool TryGetFxRate(string baseCurrency, string contraCurrency, string exchange, string source, out double dFxRate)
        {
            foreach (var element in _GetOrderedDataSources(source))
            {
                if (element.TryGetFxRate(baseCurrency, contraCurrency, exchange, source, out dFxRate))
                {
                    return true;
                }
            }

            dFxRate = 0d;
            return false;
        }

        /// <summary>
        /// Try to retrieve historical data for instrument, return data if success, null for fail.
        /// </summary>
        public IEnumerable<HistoricalData> GetHistoricalData(string instrument, string exchange, string source,  DateTime dtFrom)
        {
            foreach (var element in _GetOrderedDataSources(source))
            {
                var result = element.GetHistoricalData(instrument, exchange, source, dtFrom);
                if(result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Initialise method with configuration settings.
        /// </summary>
        public void Initialise(IConfigurationSettings settings) { }

        /// <summary>
        /// Asynchronously request a price from the data source.
        /// </summary>
        public async Task<MarketDataPrice> RequestPrice(string symbol, string exchange, string source)
        {
            foreach (var dataSource in _GetOrderedDataSources(source))
            {
                var marketData = await dataSource.RequestPrice(symbol, exchange, source);
                if (marketData != null)
                {
                    return marketData;
                }
            }
            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the list of datasources in priority order (Each datasource is assigned a priority
        /// 1 = highest.
        /// </summary>
        private IList<IMarketDataSource> _GetOrderedDataSources(string source)
        {
            if(string.IsNullOrEmpty(source) == false)
            {
                var element = _sources.FirstOrDefault(x => source.Equals(x.Name));
                if (element != default(IMarketDataSource))
                    return new List<IMarketDataSource> { element };

                logger.Log(LogLevel.Warn, "unable to locate market source {0}!", source);
            }

            return _sources.OrderBy(x => x.Priority).ToList();
            
        }

        #endregion

        private static Logger logger = LogManager.GetCurrentClassLogger();

        //private IMarketSourceLocator _sourceLocator;
        private readonly List<IMarketDataSource> _sources = new List<IMarketDataSource>();

    }
}
