using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace MarketDataServices
{
    public enum SourceDataFormat
    {
        CSV,
        JSON,
        XML
    }

    /// <summary>
    /// Abstract interface for reading raw data from a data source.
    /// </summary>
    [ContractClass(typeof(MarketDataReaderContracts))]
    public interface IMarketDataReader
    {
        IEnumerable<string> GetData(string url, SourceDataFormat format);
    }

    [ContractClassFor(typeof(IMarketDataReader))]
    internal abstract class MarketDataReaderContracts : IMarketDataReader
    {
        public IEnumerable<string> GetData(string url, SourceDataFormat format)
        {
            Contract.Requires(string.IsNullOrEmpty(url) == false);
            Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
            return null;
        }
    }
}
