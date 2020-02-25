using System.Collections.Generic;

namespace MarketDataServices
{
    /// <summary>
    /// Interface defines a service for determining all the available datasources.
    /// </summary>
    internal interface IMarketSourceLocator
    {
        /// <summary>
        /// Returns a list of available market data sources.
        /// </summary>
        IEnumerable<IMarketDataSource> Sources { get; }
    }
}
