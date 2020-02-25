using System;
using System.Threading.Tasks;
using System.IO;
using NLog;
using InvestmentBuilderCore;

namespace MarketDataServices
{
    /// <summary>
    /// class gets market data from yahoo
    /// </summary>
    internal class YahooMarketDataSource : TestFileMarketDataSource
    {
        #region Public Properties

        /// <summary>
        /// Name of datasource
        /// </summary>
        public override string Name { get { return "Yahoo"; } }

        /// <summary>
        /// Priority of datasource.
        /// </summary>
        public override int Priority { get { return 1; } }

        #endregion

        #region Public Methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public YahooMarketDataSource() 
        {
        }

        /// <summary>
        /// Overriden method for retreiving a market data price from the yahoo datasource.
        /// </summary>
        public override Task<MarketDataPrice> RequestPrice(string symbol, string exchange, string source)
        {
            return Task.Factory.StartNew(() =>
            {
                MarketDataPrice price = null;
                var outputFile = "";
                try
                {
                    //first check if price is already stored. no point in requesting again
                    if (TryGetMarketData(symbol, exchange, source, out price) == true)
                    {
                        //it is, just return stored price
                        return price;
                    }

                    //run external php script to download price
                    outputFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                              "InvestmentRecordBuilder",
                                              $"marketData_{Guid.NewGuid().ToString()}.txt");

                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "php.exe";
                    process.StartInfo.Arguments = $"MarketDataLoader.php --n:{symbol} --o:{outputFile} --s:{m_serverName} --d:{m_databaseName}"; 
                    process.StartInfo.CreateNoWindow = false;
                    process.StartInfo.ErrorDialog = true;
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.WorkingDirectory = m_scriptFolder;
                    process.Start();
                    process.WaitForExit();

                    //add result to cache
                    ProcessFileName(outputFile);
                    //now retrieve the newly found price and return it
                    TryGetMarketData(symbol, exchange, source, out price);
                }
                catch(Exception ex)
                {
                    logger.Error(ex);
                }
                finally
                {
                    if(File.Exists(outputFile))
                    {
                        File.Delete(outputFile);
                    }
                }
                return price;
            });
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Setup the yahoo datasource
        /// </summary>
        protected override void SetupDataSource(IConfigurationSettings settings)
        {
            if(InvestmentUtils.extractDatabaseDetailsFromDatasource(settings.DatasourceString, out m_serverName, out m_databaseName) == false)
            {
                throw new ApplicationException($"Invalid datasource string in configuration settings: {settings.DatasourceString}");
            }

            m_scriptFolder = settings.ScriptFolder;

        }

        #endregion

        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private string m_scriptFolder;

        private string m_serverName;

        private string m_databaseName;

        #endregion
    }
}