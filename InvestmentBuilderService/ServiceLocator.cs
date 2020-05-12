using InvestmentBuilderCore;
using InvestmentBuilderLib;
using MarketDataServices;
using SQLServerDataLayer;

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
            ContainerManager.RegisterType(typeof(IAuthorizationManager), typeof(SQLAuthorizationManager), true);
            ContainerManager.RegisterType(typeof(IMarketDataService), typeof(MarketDataService), true);
            MarketDataRegisterService.RegisterServices();
            ContainerManager.RegisterType(typeof(IInvestmentReportWriter), typeof(DummyInvestmentReportWriter), true);
            ContainerManager.RegisterType(typeof(IDataLayer), typeof(SQLServerDataLayer.SQLServerDataLayer), true);
            ContainerManager.RegisterType(typeof(IInvestmentRecordDataManager), typeof(InvestmentRecordBuilder), true);
        }
    }
}
