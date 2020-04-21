using InvestmentBuilderCore;
using InvestmentBuilderLib;
using MarketDataServices;

namespace InvestmentBuilderService
{
    /// <summary>
    /// Service Locator for the InvestmentBuilderService module. Allows
    /// any services to be registered before the handlers are loaded
    /// </summary>
    public static class ServiceLocator
    {
        public static void RegisterServices()
        {
            ContainerManager.RegisterType(typeof(IAuthorizationManager), typeof(SQLAuthorizationManager), false);
            ContainerManager.RegisterType(typeof(IMarketDataService), typeof(MarketDataService), false);
            MarketDataRegisterService.RegisterServices();
            ContainerManager.RegisterType(typeof(IInvestmentReportWriter), typeof(DummyInvestmentReportWriter), false);
            ContainerManager.RegisterType(typeof(IDataLayer), typeof(SQLServerDataLayer.SQLServerDataLayer), false);
            ContainerManager.RegisterType(typeof(IInvestmentRecordDataManager), typeof(InvestmentRecordBuilder), false);
        }
    }
}
