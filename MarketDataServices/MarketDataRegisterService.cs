using InvestmentBuilderCore;

namespace MarketDataServices
{
    /// <summary>
    /// this class registers all the required services for a production ready market data service source
    /// </summary>
    public static class MarketDataRegisterService
    {
        /// <summary>
        /// Register all the marketdata services.
        /// </summary>
        public static void RegisterServices()
        {
            //ContainerManager.RegisterType(typeof(IMarketDataSerialiser), typeof(MarketDataFileSerialiser), true);
            ContainerManager.RegisterType(typeof(IMarketDataSource), typeof(AggregatedMarketDataSource), true);
        }
    }
}
