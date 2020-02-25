using System;
using System.IO;
using InvestmentBuilderCore;

namespace MarketDataServices
{
    /// <summary>
    /// Market data file serialiser class. saves market data to a file.
    /// </summary>
    internal class MarketDataFileSerialiser : IMarketDataSerialiser
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public MarketDataFileSerialiser(IConfigurationSettings settings)
        {
            _fileName = settings.OutputCachedMarketData;
        }

        #region IMarketDataSerialiser

        /// <summary>
        /// Starts a data serialiser. 
        /// </summary>
        public void StartSerialiser()
        {
            if (_fileName != null && _writer == null)
            {
                _writer = new StreamWriter(_fileName);
            }
        }

        /// <summary>
        /// Close a serialiser.
        /// </summary>
        public void EndSerialiser()
        {
            if(_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }

        /// <summary>
        /// Serialise some data.
        /// </summary>
        public void SerialiseData(string data, params object[] prm)
        {
            if(_writer != null)
            {
                _writer.WriteLine(data, prm);
            }
        }

        /// <summary>
        /// Load all the data from a serialiser. For each block of data call the processRecord
        /// delegate.
        /// </summary>
        public void LoadData(Action<string> processRecord)
        {
            if(_fileName == null)
            {
                return;
            }

            using (var reader = new StreamReader(_fileName))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if(processRecord != null)
                    {
                        processRecord(line);
                    }
                }
            }
        }

        #endregion

        #region Private Data

        private string _fileName;
        private StreamWriter _writer;

        #endregion
    }
}
