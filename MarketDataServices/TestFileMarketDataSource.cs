using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using InvestmentBuilderCore;
using NLog;

namespace MarketDataServices
{
    /// <summary>
    /// market data source based on a test file.Just looks forthe symbol ina test file
    /// and returns value specified. returns true if symbol found otherwise returns
    /// false. does the same for FxRates
    ///  file has format M/F,Symbol,Value
    ///  where M = stock dataand F= FX data
    /// This class is useful for full application testing when there are no datasources available
    /// 
    /// </summary>
    public class TestFileMarketDataSource : IMarketDataSource, IDisposable
    {
        #region Constructor

        /// <summary>
        /// Parameterless Constructor.
        /// </summary>
        public TestFileMarketDataSource()
        {
        }

        /// <summary>
        /// Construct from file
        /// </summary>
        public TestFileMarketDataSource(string filename)
        {
            m_currentSource = filename;
            InitialiseFromFile(m_currentSource);
        }

        #endregion

        #region IMarketDataSource

        /// <summary>
        /// Name of data source.
        /// </summary>
        public virtual string Name { get { return "TestFileMarketDataSource"; } }

        /// <summary>
        /// Priority of data source.
        /// </summary>
        public virtual int Priority { get { return 5; } }

        /// <summary>
        /// Initialise. Initialise from configuration settings. Called when class
        /// instantiated from depenency injection framework.
        /// </summary>
        public virtual void Initialise(IConfigurationSettings settings)
        {
            m_currentSource = settings.MarketDatasource;
            InitialiseFromFile(m_currentSource);
            SetupDataSource(settings);
        }

        /// <summary>
        /// Return the name of this source.
        /// </summary>
        public IList<string> GetSources()
        {
            return new List<string> { Name };
        }

        /// <summary>
        /// Retrieve market data from cache.
        /// </summary>
        public bool TryGetMarketData(string symbol, string exchange, string source, out MarketDataPrice marketData)
        {
            if (_marketDataLookup.TryGetValue(symbol, out marketData))
            {
                marketData.DecimalisePrice();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieve an FX rate from the cache.
        /// </summary>
        public bool TryGetFxRate(string baseCurrency, string contraCurrency, string exchange, string source, out double dFxRate)
        {
            var ccypair = _mapCurrency(baseCurrency) + _mapCurrency(contraCurrency);
            return _fxDataLookup.TryGetValue(ccypair, out dFxRate);
        }

        /// <summary>
        /// Retrieve historical data for an instrument.
        /// </summary>
        public IEnumerable<HistoricalData> GetHistoricalData(string instrument, string exchange, string source, DateTime dtFrom)
        {
            //first check if instrument is in historical data cache
            IList<HistoricalData> cache;
            if (_historicalDataLookup.TryGetValue(instrument, out cache) == true)
            {
                var result = cache.Where(x => x.Date >= dtFrom).ToList();
                if (result.Count > 0)
                {
                    return result;
                }
            }

            //if not in cache then just generate some historical price data

            if (instrument.Contains("FTSE"))
            {
                return _GenerateHistoricalData(dtFrom, 0.008); //ftse
            }
            else if (instrument.Contains("GSPC"))
            {
                return _GenerateHistoricalData(dtFrom, 0.009); //s&p
            }

            return _GenerateHistoricalData(dtFrom, 0.006);
        }

        /// <summary>
        /// Dispose class.
        /// </summary>
        public void Dispose()
        {
            Console.WriteLine("disposing TestDataSource...");
        }

        public virtual Task<MarketDataPrice> RequestPrice(string symbol, string exchange, string source)
        {
            return Task.Factory.StartNew(() =>
            {
                MarketDataPrice price;
                if (TryGetMarketData(symbol, exchange, source, out price) == true)
                {
                    return price;
                }
                return null;
            });
        }

        #endregion

        #region IScheduledTask

        /// <summary>
        /// Refresh the datasource.
        /// </summary>
        public void RunJob()
        {
            logger.Info($"Refreshing Datasource {Name}");
            ClearCaches();
            InitialiseFromFile(m_currentSource);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Datasource specific setup. Can be overriden by implementations.
        /// </summary>
        protected virtual void SetupDataSource(IConfigurationSettings settings)
        {
        }

        /// <summary>
        /// Load data from a file.
        /// </summary>
        protected void ProcessFileName(string filename)
        {
            if (File.Exists(filename) == false)
            {
                logger.Log(LogLevel.Error, "file does not exist: {0}", filename);
                return;
            }

            logger.Info($"Loading market data from file {filename}");

            using (var reader = new StreamReader(filename))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var elems = line.Split(';');
                    if (elems.Length > 2)
                    {
                        if (elems[0].Equals("M", StringComparison.CurrentCultureIgnoreCase) == true)
                        {
                            if (elems.Length > 3)
                            {
                                _addMarketDataToLookup(elems[1], elems[2], elems[3], _marketDataLookup);
                            }
                        }
                        else if (elems[0].Equals("F", StringComparison.CurrentCultureIgnoreCase) == true)
                        {
                            _addDataToLookup(elems[1], elems[2], _fxDataLookup);
                        }
                        else if (elems[0].Equals("H", StringComparison.CurrentCultureIgnoreCase) == true)
                        {
                            _AddDataToHistoricalLookup(elems[1], elems[2]);
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Add market data to a cache.
        /// </summary>
        private void _addMarketDataToLookup(string name, string strPrice, string strCurrency, Dictionary<string, MarketDataPrice> lookup)
        {
            if (lookup.ContainsKey(name) == true)
            {
                return;
            }

            double dPrice;
            if (double.TryParse(strPrice, out dPrice))
            {
                lookup.Add(name, new MarketDataPrice
                (
                    name,
                    name,
                    dPrice,
                    strCurrency
                ));
            }
        }

        /// <summary>
        /// Add FX market data to a cache.
        /// </summary>
        private void _addDataToLookup(string name, string strPrice, Dictionary<string, double> lookup)
        {
            double dRate;
            if (double.TryParse(strPrice, out dRate))
            {
                lookup.Add(name, dRate);
            }
        }

        /// <summary>
        /// historical data has format dd/mm/yyyy=value:dd/mm/yyyy=value:etc... 
        /// </summary>
        private void _AddDataToHistoricalLookup(string name, string data)
        {
            _historicalDataLookup.Add(name,
                data.Split(':').Select(x =>
                {
                    int split = x.IndexOf('=');
                    return new HistoricalData
                    (
                        date: DateTime.Parse(x.Substring(0, split)),
                        price: Double.Parse(x.Substring(split + 1))
                    );
                }).ToList());
        }

        /// <summary>
        /// Clear all the current caches.
        /// </summary>
        private void ClearCaches()
        {
            _marketDataLookup.Clear();
            _fxDataLookup.Clear();
            _historicalDataLookup.Clear();
        }

        /// <summary>
        /// Initialise cache from a file.
        /// </summary>
        private void InitialiseFromFile(string filename)
        {
            if (_marketDataLookup.Count == 0)
            {
                ProcessFileName(filename);
            }
        }

        /// <summary>
        /// Add a generated list of historic data.
        /// </summary>
        private IEnumerable<HistoricalData> _GenerateHistoricalData(DateTime dtFrom, double dIncrement)
        {
            //always take basedate from first day of month
            DateTime dtDate = new DateTime(dtFrom.Year,dtFrom.Month, 1);
           
            double dPrice = 1.0;
            while(dtDate <= DateTime.Today)
            {
                yield return new HistoricalData
                (
                    date: dtDate,
                    price: dPrice
                );

                dtDate = dtDate.AddMonths(1);
                dPrice += dIncrement;
            }
        }

        /// <summary>
        /// Map a currency to its other known name if defined.
        /// </summary>
        private string _mapCurrency(string ccy)
        {
            if (_currencyMapper.ContainsKey(ccy) == true)
            {
                return _currencyMapper[ccy];
            }
            return ccy;
        }

        #endregion

        #region Private Data

        //  Caches...
        private Dictionary<string, MarketDataPrice> _marketDataLookup = new Dictionary<string, MarketDataPrice>();
        private Dictionary<string, double> _fxDataLookup = new Dictionary<string, double>();
        private Dictionary<string, IList<HistoricalData>> _historicalDataLookup = new Dictionary<string, IList<HistoricalData>>();

        private string m_currentSource;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        //private const string _testDataPath = @"C:\Projects\TestData\InvestmentBuilder";
        //private const string _testDataFile = "testMarketData.txt";
        private static Dictionary<string, string> _currencyMapper = new Dictionary<string, string>()
            {
                {"NYQ", "USD"}
            };

        #endregion
    }
}
